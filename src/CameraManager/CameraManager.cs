

using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Devices.Common.Cameras;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras
{

    public class CameraManager : EssentialsDevice
    {
        private readonly CameraManagerPropertiesConfig config;

        private EssentialsRoomCombiner roomCombiner;

        private INetworkSwitchPoeVlanManager networkSwitch;

        private Dictionary<string, CiscoCamera> managedCameras = new Dictionary<string, CiscoCamera>();

        private Dictionary<string, ICiscoCodecCameraFactoryReset> managedCodecs = new Dictionary<string, ICiscoCodecCameraFactoryReset>();

        private readonly Dictionary<string, CameraMigrationState> activeMigrations = new Dictionary<string, CameraMigrationState>();
        private readonly object activeMigrationsLock = new object();

        // In-flight dynamic camera ID reservations. Between the moment we issue an
        // AssignedSerialNumber to a codec and the moment the codec reports it back in its Cameras
        // collection, the confirmed state does not yet reflect the assignment. Without tracking
        // these pending allocations, two dynamic cameras assigned to the same codec in quick
        // succession could both be handed the same free id — a duplicate write. On a Cisco codec
        // AssignedSerialNumber is a single-value-per-slot config, so a duplicate write overwrites
        // the first serial, orphaning that camera and letting the codec auto-detect it onto an
        // arbitrary slot (possibly outside the [7,8,9] pool). These reservations close that window.
        // Keyed by codec key -> (camera key -> reservation). Guarded by activeMigrationsLock.
        private readonly Dictionary<string, Dictionary<string, CameraIdReservation>> pendingCameraIdReservations = new Dictionary<string, Dictionary<string, CameraIdReservation>>();
        private static readonly TimeSpan CameraIdReservationTtl = TimeSpan.FromSeconds(30);

        // Cumulative attach-timeout count per camera, preserved across the reseed→fresh-cascade
        // retry loop (the migration state is recreated each cycle, so the count must live here).
        // Used for log visibility only; it does not change recovery behavior. Cleared on a
        // confirmed attach.
        private readonly Dictionary<string, int> attachFailureCounts = new Dictionary<string, int>();

        // Safety-net reconciliation: per-camera earliest UTC at which the periodic floating-camera
        // sweep may next act. Seeded after a reseed so the normal rediscovery cascade gets a grace
        // period before the watchdog intervenes; cleared on a confirmed attach. Guarded by
        // activeMigrationsLock.
        private readonly Dictionary<string, DateTime> reconcileNextActionUtc = new Dictionary<string, DateTime>();

        // Counts floating watchdog recovery attempts per camera. Floating recovery uses PoE
        // bounce on every attempt and stops once the attempt limit is reached.
        private readonly Dictionary<string, int> floatingRecoveryCounts = new Dictionary<string, int>();

        private readonly Timer attachVerificationTimer;
        private int attachVerificationTimerHandlerActive;
        // Only touched from inside AttachVerificationTimer_Elapsed (single-threaded via the
        // re-entrancy guard), so it needs no lock.
        private DateTime nextReconcileSweepUtc = DateTime.MinValue;

        // Set to 1 when activation runs before the room combiner has resolved its scenario, so the
        // initial reconciliation must be deferred. The attach-verification timer (which runs every
        // second from activation) retries it once the combiner reports a scenario; cleared when the
        // deferred run executes or when a scenario-changed event runs reconciliation instead.
        private int startupReconciliationPending;

        private const int AttachWaitTimeoutMs = 120000;
        private const int MaxPoeOffDurationMs = 60000;
        private const int MigrationPoeOffDelayMs = 500;
        private const int DefaultFactoryResetSettleMs = 2000;

        // Delay between issuing the source-codec factory reset and tearing down PoE/VLAN to move
        // the camera. The reset must take effect first, otherwise the camera arrives at the target
        // codec still paired to the source and the target reports "pinhole factory reset required".
        // NOTE: the source codec's Connected feedback is NOT a reliable signal here — the camera
        // begins factory-defaulting as soon as the command is sent, but the codec keeps reporting
        // Connected=true until the camera actually reboots/drops, so we cannot gate on it. A fixed
        // settle delay is used instead. Configurable via factoryResetSettleMs; defaults to
        // DefaultFactoryResetSettleMs.
        private readonly int factoryResetSettleMs;

        // When true, the migration cascade and recovery/reconcile paths skip all PoE off/on calls
        // and move cameras by VLAN change + serial reassignment only. The cascade is advanced
        // manually at the points the PoE off/on feedback events would normally drive it.
        // Configurable via disablePoeCycling; defaults to false (normal PoE cycling).
        private readonly bool disablePoeCycling;

        // When true, the post-factory-reset settle is gated on the source codec reporting the
        // camera disconnected (the reset actually taking effect) rather than the fixed
        // factoryResetSettleMs timer. A bounded fallback (FactoryResetDisconnectTimeoutMs) starts
        // the cascade anyway if no disconnect arrives. Configurable via
        // useCameraFactoryResetDisconnectFeedback; defaults to false (fixed timer).
        private readonly bool useCameraFactoryResetDisconnectFeedback;

        // Upper bound on how long to wait for the source-codec disconnect feedback before starting
        // the PoE/VLAN cascade anyway, when useCameraFactoryResetDisconnectFeedback is enabled.
        private const int FactoryResetDisconnectTimeoutMs = 25000;

        // Safety-net reconciliation cadence and per-camera backoff. The periodic sweep catches
        // cameras that ended up floating after a recovery dead-end (e.g. the source codec never
        // re-reported the camera online after a reseed, so no fresh migration cascade ever
        // restarted). The backoff keeps the watchdog from disrupting an in-flight recovery.
        private const int ReconcileSweepIntervalMs = 30000;
        private const int ReconcileBackoffMs = AttachWaitTimeoutMs;
        private const int FloatingRecoveryAttemptLimit = 12;

        // Maximum time to wait for source-side assigned-serial clear feedback before forcing the
        // VLAN switch. This prevents a reseeded camera from staying parked on the source codec.
        private const int AssignmentClearTimeoutMs = 5000;

        public CameraManager(string key, string name, CameraManagerPropertiesConfig config)
            : base(key, name)
        {
            this.config = config;
            factoryResetSettleMs = config != null && config.FactoryResetSettleMs > 0
                ? config.FactoryResetSettleMs
                : DefaultFactoryResetSettleMs;
            disablePoeCycling = config != null && config.DisablePoeCycling;
            useCameraFactoryResetDisconnectFeedback = config != null && config.UseCameraFactoryResetDisconnectFeedback;
            attachVerificationTimer = new Timer(1000) { AutoReset = true };
            attachVerificationTimer.Elapsed += AttachVerificationTimer_Elapsed;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

        }

        ~CameraManager()
        {
            StopAndDisposeAttachVerificationTimer();
        }

        private void StartAttachVerificationTimer()
        {
            if (!attachVerificationTimer.Enabled)
            {
                attachVerificationTimer.Start();
            }
        }

        private void StopAndDisposeAttachVerificationTimer()
        {
            try
            {
                attachVerificationTimer.Stop();
                attachVerificationTimer.Elapsed -= AttachVerificationTimer_Elapsed;
                attachVerificationTimer.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            StopAndDisposeAttachVerificationTimer();
        }

        /// <summary>
        /// Custom activation to link the Camera Manager to the room combiner, network switch, codecs, and cameras based on the keys provided in the configuration.
        /// </summary>
        /// <returns></returns>
        public override bool CustomActivate()
        {
            var activated = CustomActivateInternal();
            if (activated)
            {
                StartAttachVerificationTimer();
            }

            return activated;
        }

        private bool CustomActivateInternal()
        {
            if (config == null)
            {
                this.LogError($"Camera Manager {Key} failed to activate: missing required config");
                return false;
            }

            if (config.RoomCombinerConfig == null)
            {
                this.LogError($"Camera Manager {Key} failed to activate: missing required roomCombinerConfig block");
                return false;
            }

            if (string.IsNullOrWhiteSpace(config.RoomCombinerConfig.RoomCombinerKey))
            {
                this.LogError($"Camera Manager {Key} failed to activate: RoomCombinerConfig.RoomCombinerKey is required");
                return false;
            }

            var roomCombinerDevice = DeviceManager.GetDeviceForKey(config.RoomCombinerConfig.RoomCombinerKey) as EssentialsRoomCombiner;
            if (roomCombinerDevice == null)
            {
                this.LogError($"Camera Manager {Key} failed to activate: Room Combiner device with key {config.RoomCombinerConfig.RoomCombinerKey} not found or not an EssentialsRoomCombiner");
                return false;
            }

            roomCombiner = roomCombinerDevice;

            roomCombiner.RoomCombinationScenarioChanged += RoomCombiner_RoomCombinationScenarioChanged;

            if (string.IsNullOrWhiteSpace(config.NetworkSwitchKey))
            {
                this.LogError($"Camera Manager {Key} failed to activate: required network switch identifier (networkSwitchKey) is missing or empty");
                return false;
            }

            var networkSwitchDevice = DeviceManager.GetDeviceForKey(config.NetworkSwitchKey) as INetworkSwitchPoeVlanManager;
            if (networkSwitchDevice == null)
            {
                this.LogError($"Camera Manager {Key} failed to activate: Network Switch device with key {config.NetworkSwitchKey} not found or does not implement INetworkSwitchPoeVlanManager");
                return false;
            }

            networkSwitch = networkSwitchDevice;

            networkSwitch.PortStateChanged += NetworkSwitch_PortStateChanged;

            if (config.RoomCombinerConfig.CombineScenarios == null || !config.RoomCombinerConfig.CombineScenarios.Any())
            {
                this.LogError($"Camera Manager {Key} failed to activate: RoomCombinerConfig.CombineScenarios is null or empty");
                return false;
            }

            HashSet<string> codecKeysInScenarios = new HashSet<string>();
            HashSet<string> cameraKeysInScenarios = new HashSet<string>();
            foreach (var scenario in config.RoomCombinerConfig.CombineScenarios)
            {
                if (scenario.Value == null)
                {
                    this.LogError($"Camera Manager {Key} failed to activate: CombineScenarios['{scenario.Key}'].Value is null");
                    return false;
                }

                if (scenario.Value.CodecConfigs == null || !scenario.Value.CodecConfigs.Any())
                {
                    this.LogError($"Camera Manager {Key} failed to activate: CombineScenarios['{scenario.Key}'].CodecConfigs is null or empty");
                    return false;
                }

                foreach (var codecConfig in scenario.Value.CodecConfigs)
                {
                    if (codecConfig == null)
                    {
                        this.LogError($"Camera Manager {Key} failed to activate: CombineScenarios['{scenario.Key}'].CodecConfigs contains a null item");
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(codecConfig.CodecKey))
                    {
                        this.LogError($"Camera Manager {Key} failed to activate: CombineScenarios['{scenario.Key}'].CodecConfigs contains a CodecConfig where CodecKey is null or empty");
                        return false;
                    }

                    codecKeysInScenarios.Add(codecConfig.CodecKey);

                    if (codecConfig.CameraKeys == null || !codecConfig.CameraKeys.Any())
                    {
                        this.LogError($"Camera Manager {Key} failed to activate: CombineScenarios['{scenario.Key}'].CodecConfigs['{codecConfig.CodecKey}'].CameraKeys is null or empty");
                        return false;
                    }

                    foreach (var cameraKey in codecConfig.CameraKeys)
                    {
                        if (string.IsNullOrWhiteSpace(cameraKey))
                        {
                            this.LogError($"Camera Manager {Key} failed to activate: CombineScenarios['{scenario.Key}'].CodecConfigs['{codecConfig.CodecKey}'].CameraKeys contains a null or empty key");
                            return false;
                        }

                        cameraKeysInScenarios.Add(cameraKey);
                    }
                }
            }

            foreach (var codecKey in codecKeysInScenarios)
            {
                var codecDevice = DeviceManager.GetDeviceForKey(codecKey) as ICiscoCodecCameraFactoryReset;
                if (codecDevice == null)
                {
                    this.LogError($"Camera Manager {Key} failed to activate: Codec device with key {codecKey} not found or does not implement ICiscoCodecCameraFactoryReset");
                    return false;
                }
                managedCodecs.Add(codecKey, codecDevice);
            }

            foreach (var cameraKey in cameraKeysInScenarios)
            {
                var cameraDevice = DeviceManager.GetDeviceForKey(cameraKey) as CiscoCamera;
                if (cameraDevice == null)
                {
                    this.LogError($"Camera Manager {Key} failed to activate: Camera device with key {cameraKey} not found or not a CiscoCamera");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(cameraDevice.NetworkSwitchPort))
                {
                    this.LogError($"Camera Manager {Key} failed to activate: Camera device with key {cameraKey} is missing required migration property NetworkSwitchPort");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(cameraDevice.SerialNumber))
                {
                    this.LogError($"Camera Manager {Key} failed to activate: Camera device with key {cameraKey} is missing required migration property SerialNumber");
                    return false;
                }

                managedCameras.Add(cameraKey, cameraDevice);
            }

            // Late validation (needs resolved camera devices for DefaultCameraId). For each scenario:
            //  - a single camera must not appear under more than one codec (a physical camera can
            //    only attach to one codec at a time);
            //  - within a codec, effective ids (explicit scenario id ?? camera.DefaultCameraId) must
            //    be unique so two cameras never collide on the same slot;
            //  - an explicit scenario id must be non-zero.
            foreach (var scenario in config.RoomCombinerConfig.CombineScenarios)
            {
                var cameraToCodec = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var codecConfig in scenario.Value.CodecConfigs)
                {
                    var effectiveIdOwners = new Dictionary<uint, string>();
                    foreach (var assignment in codecConfig.CameraAssignments)
                    {
                        var cameraKey = assignment.CameraKey;

                        if (assignment.CameraId.HasValue && assignment.CameraId.Value == 0)
                        {
                            this.LogError($"Camera Manager {Key} failed to activate: scenario '{scenario.Key}' codec '{codecConfig.CodecKey}' camera '{cameraKey}' has an invalid cameraId of 0");
                            return false;
                        }

                        if (cameraToCodec.TryGetValue(cameraKey, out var otherCodecKey))
                        {
                            this.LogError($"Camera Manager {Key} failed to activate: scenario '{scenario.Key}' assigns camera '{cameraKey}' to both codec '{otherCodecKey}' and codec '{codecConfig.CodecKey}'. A camera can only be assigned to one codec per scenario.");
                            return false;
                        }
                        cameraToCodec[cameraKey] = codecConfig.CodecKey;

                        if (!managedCameras.TryGetValue(cameraKey, out var cam))
                        {
                            continue;
                        }

                        var effectiveId = assignment.CameraId ?? cam.DefaultCameraId;
                        if (effectiveIdOwners.TryGetValue(effectiveId, out var otherCameraKey))
                        {
                            this.LogError($"Camera Manager {Key} failed to activate: scenario '{scenario.Key}' codec '{codecConfig.CodecKey}' assigns effective cameraId {effectiveId} to both '{otherCameraKey}' and '{cameraKey}' (explicit id or defaultCameraId collision).");
                            return false;
                        }
                        effectiveIdOwners[effectiveId] = cameraKey;
                    }
                }
            }

            foreach (var kvp in managedCodecs)
            {
                var codec = kvp.Value;
                codec.CameraConnected += Codec_CameraConnected;
                codec.CameraDisconnected += Codec_CameraDisconnected;
                codec.CameraAssignedSerialNumberChanged += Codec_CameraAssignedSerialNumberChanged;
            }

            var startupScenario = roomCombiner.CurrentScenario?.Key;
            if (!string.IsNullOrEmpty(startupScenario))
            {
                this.LogDebug($"Camera Manager {Key} startup reconciliation for current scenario '{startupScenario}'");
                RunScenarioReconciliation(startupScenario);
            }
            else
            {
                System.Threading.Interlocked.Exchange(ref startupReconciliationPending, 1);
                this.LogWarning($"Camera Manager {Key} deferring startup reconciliation: current room scenario not resolved yet — will run once the room combiner reports a scenario");
            }

            return base.CustomActivate();
        }

        private void NetworkSwitch_PortStateChanged(object sender, NetworkSwitchPortEventArgs e)
        {
            this.LogVerbose($"Camera Manager {Key} detected network switch port state change on port '{e.Port}' to state '{e.EventType}'");

            if (e.EventType == NetworkSwitchPortEventType.PoEDisabled)
            {
                CameraMigrationState migrationForVlanSwitch = null;
                string cameraKey = null;
                string port = null;

                lock (activeMigrationsLock)
                {
                    var migration = GetMigrationByPort(e.Port);
                    if (migration == null)
                    {
                        this.LogDebug($"Camera Manager {Key} detected PoE disabled event on port '{e.Port}' but no active camera migration is associated with that port");
                        return;
                    }

                    migration.PoeDisabledConfirmed = true;
                    migration.PoeOffDeadlineUtc = DateTime.UtcNow.AddMilliseconds(MaxPoeOffDurationMs);
                    migration.PoeOffSafeguardTriggered = false;
                    migrationForVlanSwitch = migration;
                    cameraKey = migration.CameraKey;
                    port = migration.Port;
                }

                this.LogDebug($"Camera Manager {Key} confirmed PoE disabled for camera '{cameraKey}' on port '{port}'");
                TryIssueVlanSwitch(migrationForVlanSwitch);
            }
            else if (e.EventType == NetworkSwitchPortEventType.VlanChanged)
            {
                bool shouldEnablePoe = false;
                bool shouldStartAttachWait = false;
                string cameraKey = null;
                string sourceCodecKey = null;
                uint sourceCameraId = 0;
                string targetCodecKey = null;
                string port = null;

                lock (activeMigrationsLock)
                {
                    var migration = GetMigrationByPort(e.Port);
                    if (migration == null)
                    {
                        this.LogDebug($"Camera Manager {Key} detected VLAN changed event on port '{e.Port}' with no active migration");
                        return;
                    }

                    migration.VlanChangedConfirmed = true;
                    if (disablePoeCycling)
                    {
                        // PoE cycling disabled: there is no PoE-on step (and therefore no PoEEnabled
                        // event) to drive the attach wait, so advance the cascade straight to the
                        // attach-wait / target-serial-assign step here.
                        if (!migration.AttachWaitStarted)
                        {
                            migration.PoeEnableIssued = true;
                            migration.AttachWaitStarted = true;
                            migration.TargetSerialAssigned = false;
                            migration.WaitingForTargetSlotClear = false;
                            migration.ExpectedTargetSlot = 0;
                            migration.AttachWaitDeadlineUtc = DateTime.UtcNow.AddMilliseconds(AttachWaitTimeoutMs);
                            shouldStartAttachWait = true;
                            cameraKey = migration.CameraKey;
                            sourceCodecKey = migration.SourceCodecKey;
                            sourceCameraId = migration.SourceCameraId;
                            targetCodecKey = migration.TargetCodecKey;
                            port = migration.Port;
                        }
                    }
                    else if (!migration.PoeEnableIssued)
                    {
                        migration.PoeEnableIssued = true;
                        migration.PoeReenableDeadlineUtc = DateTime.UtcNow.AddMilliseconds(AttachWaitTimeoutMs);
                        shouldEnablePoe = true;
                        cameraKey = migration.CameraKey;
                    }
                }

                if (shouldEnablePoe)
                {
                    this.LogDebug($"CAMERA_SWITCHOVER_POE_ON_AFTER_VLAN camera='{cameraKey}' port='{e.Port}' reason='vlanChangedConfirmed'");
                    networkSwitch.SetPortPoeState(e.Port, true);
                    this.LogDebug($"Camera Manager {Key} confirmed VLAN changed for camera '{cameraKey}', re-enabling PoE on port '{e.Port}'");
                }
                else if (shouldStartAttachWait)
                {
                    this.LogDebug($"CAMERA_SWITCHOVER_ATTACH_WAITING camera='{cameraKey}' sourceCodec='{sourceCodecKey}' sourceCameraId='{sourceCameraId}' targetCodec='{targetCodecKey}' port='{port}' vlanChanged='True' poeEnabled='disabled'");
                    this.LogDebug($"Camera Manager {Key} PoE cycling disabled — VLAN changed for camera '{cameraKey}', advancing straight to attach wait without a PoE cycle on port '{e.Port}'");
                    var assignResult = TryAssignSerialToTargetCodec(cameraKey, targetCodecKey, port, "attachWaitStartNoPoe", out var targetSlot, out var blockingCameraKey);
                    lock (activeMigrationsLock)
                    {
                        if (activeMigrations.TryGetValue(cameraKey, out var migration) && migration.AttachWaitStarted)
                        {
                            migration.TargetSerialAssigned = assignResult == TargetSerialAssignResult.Assigned;
                            migration.WaitingForTargetSlotClear = assignResult == TargetSerialAssignResult.SlotBusy;
                            migration.ExpectedTargetSlot = targetSlot;
                            migration.BlockedByCameraKey = blockingCameraKey;
                        }
                    }
                }
            }
            else if (e.EventType == NetworkSwitchPortEventType.PoEEnabled)
            {
                string cameraKey = null;
                string sourceCodecKey = null;
                uint sourceCameraId = 0;
                string targetCodecKey = null;
                string port = null;
                bool vlanChangedConfirmed = false;

                lock (activeMigrationsLock)
                {
                    var migration = GetMigrationByPort(e.Port);
                    if (migration == null)
                    {
                        return;
                    }

                    // Only treat this PoEEnabled as the cascade's attach-wait start if THIS migration
                    // actually drove it: we must have switched the VLAN and re-enabled PoE ourselves.
                    // At startup (and other times) the switch reports the port's current PoE state as a
                    // PoEEnabled event while the camera is still powered on the SOURCE VLAN. Without this
                    // guard that stray event was mistaken for "migration sequence complete" — firing the
                    // target-serial-assign and a false ATTACH_CONFIRMED — even though the VLAN was never
                    // changed, so the camera never physically moved to the target codec.
                    if (!migration.PoeEnableIssued || !migration.VlanChangedConfirmed)
                    {
                        this.LogDebug($"Camera Manager {Key} ignoring PoEEnabled on port '{e.Port}' for camera '{migration.CameraKey}': cascade has not reached the post-VLAN PoE-on step (vlanChanged='{migration.VlanChangedConfirmed}', poeEnableIssued='{migration.PoeEnableIssued}') — stray/initial port state event");
                        return;
                    }

                    migration.AttachWaitStarted = true;
                    migration.TargetSerialAssigned = false;
                    migration.WaitingForTargetSlotClear = false;
                    migration.ExpectedTargetSlot = 0;
                    migration.AttachWaitDeadlineUtc = DateTime.UtcNow.AddMilliseconds(AttachWaitTimeoutMs);
                    cameraKey = migration.CameraKey;
                    sourceCodecKey = migration.SourceCodecKey;
                    sourceCameraId = migration.SourceCameraId;
                    targetCodecKey = migration.TargetCodecKey;
                    port = migration.Port;
                    vlanChangedConfirmed = migration.VlanChangedConfirmed;
                }

                this.LogDebug($"CAMERA_SWITCHOVER_ATTACH_WAITING camera='{cameraKey}' sourceCodec='{sourceCodecKey}' sourceCameraId='{sourceCameraId}' targetCodec='{targetCodecKey}' port='{port}' vlanChanged='{vlanChangedConfirmed}' poeEnabled='True'");
                this.LogDebug($"Camera Manager {Key} confirmed migration sequence complete for camera '{cameraKey}' on port '{port}'");

                var assignResult = TryAssignSerialToTargetCodec(cameraKey, targetCodecKey, port, "attachWaitStart", out var targetSlot, out var blockingCameraKey);
                lock (activeMigrationsLock)
                {
                    if (activeMigrations.TryGetValue(cameraKey, out var migration) && migration.AttachWaitStarted)
                    {
                        migration.TargetSerialAssigned = assignResult == TargetSerialAssignResult.Assigned;
                        migration.WaitingForTargetSlotClear = assignResult == TargetSerialAssignResult.SlotBusy;
                        migration.ExpectedTargetSlot = targetSlot;
                        migration.BlockedByCameraKey = blockingCameraKey;
                    }
                }
            }
        }

        /// <summary>
        /// Pins the managed camera's serial number to its configured slot on the target codec so the
        /// target codec claims the camera as soon as it boots on the target VLAN. Without this, the
        /// camera reaches the target codec's network but the codec never reports it as connected.
        /// </summary>
        private enum TargetSerialAssignResult
        {
            Failed,
            SlotBusy,
            Assigned
        }

        private TargetSerialAssignResult TryAssignSerialToTargetCodec(string cameraKey, string targetCodecKey, string port, string reason, out uint targetSlot, out string blockingCameraKey)
        {
            targetSlot = 0;
            blockingCameraKey = null;

            if (string.IsNullOrEmpty(cameraKey) || string.IsNullOrEmpty(targetCodecKey))
            {
                return TargetSerialAssignResult.Failed;
            }

            if (!managedCameras.TryGetValue(cameraKey, out var camera))
            {
                this.LogDebug($"Camera Manager {Key} cannot assign serial to target codec '{targetCodecKey}' for camera '{cameraKey}': camera not found");
                return TargetSerialAssignResult.Failed;
            }

            if (string.IsNullOrWhiteSpace(camera.SerialNumber))
            {
                this.LogDebug($"Camera Manager {Key} cannot assign serial to target codec '{targetCodecKey}' for camera '{cameraKey}': camera has no serial number");
                return TargetSerialAssignResult.Failed;
            }

            if (!managedCodecs.TryGetValue(targetCodecKey, out var targetCodec))
            {
                this.LogError($"Camera Manager {Key} cannot assign serial to target codec for camera '{cameraKey}': target codec '{targetCodecKey}' not found");
                return TargetSerialAssignResult.Failed;
            }

            var explicitSlot = GetScenarioConfiguredCameraId(roomCombiner?.CurrentScenario?.Key, targetCodecKey, cameraKey);
            camera.SetScenarioCameraId(explicitSlot);
            targetSlot = explicitSlot ?? camera.DefaultCameraId;
            var resolvedTargetSlot = targetSlot;

            var targetCodecAsCisco = targetCodec as CiscoCodec;
            var conflictingCamera = targetCodecAsCisco?.Cameras?.OfType<CiscoCamera>()
                .FirstOrDefault(c => c.CameraId == resolvedTargetSlot
                    && !string.IsNullOrWhiteSpace(c.SerialNumber)
                    && !string.Equals(c.SerialNumber, camera.SerialNumber, StringComparison.OrdinalIgnoreCase));

            if (conflictingCamera != null)
            {
                blockingCameraKey = managedCameras.Values
                    .FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.SerialNumber)
                        && string.Equals(c.SerialNumber, conflictingCamera.SerialNumber, StringComparison.OrdinalIgnoreCase))?.Key;
                this.LogDebug($"CAMERA_SWITCHOVER_TARGET_SLOT_BUSY camera='{cameraKey}' targetCodec='{targetCodecKey}' targetSlot='{targetSlot}' occupyingSerial='{conflictingCamera.SerialNumber}' reason='{reason}' action='waitForSlotClear' port='{port}'");
                this.LogDebug($"Camera Manager {Key} delaying target serial assignment for camera '{cameraKey}' on codec '{targetCodecKey}' slot {targetSlot}: waiting for blocker camera activity to clear naturally (blockingCameraKey='{blockingCameraKey ?? "unknown"}')");
                return TargetSerialAssignResult.SlotBusy;
            }

            this.LogDebug($"CAMERA_SWITCHOVER_TARGET_SERIAL_ASSIGN camera='{cameraKey}' targetCodec='{targetCodecKey}' slot='{targetSlot}' serial='{camera.SerialNumber}' port='{port}' reason='{reason}'");
            targetCodec.SetCameraAssignedSerialNumber(targetSlot, camera.SerialNumber);
            return TargetSerialAssignResult.Assigned;
        }

        private void TryAdvanceMigrationsWaitingForTargetSlotClear(string codecKey, string reason, string changedCameraKey = null)
        {
            if (string.IsNullOrWhiteSpace(codecKey))
            {
                return;
            }

            List<CameraMigrationState> waiting;
            lock (activeMigrationsLock)
            {
                waiting = activeMigrations.Values
                    .Where(m => m.AttachWaitStarted
                        && !m.TargetSerialAssigned
                        && m.WaitingForTargetSlotClear
                        && string.Equals(m.TargetCodecKey, codecKey, StringComparison.OrdinalIgnoreCase)
                        && (string.IsNullOrWhiteSpace(changedCameraKey)
                            || string.Equals(m.BlockedByCameraKey, changedCameraKey, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            foreach (var migration in waiting)
            {
                var assignResult = TryAssignSerialToTargetCodec(
                    migration.CameraKey,
                    migration.TargetCodecKey,
                    migration.Port,
                    reason,
                    out var targetSlot,
                    out var blockingCameraKey);

                lock (activeMigrationsLock)
                {
                    if (!activeMigrations.TryGetValue(migration.CameraKey, out var current)
                        || !ReferenceEquals(current, migration)
                        || !current.AttachWaitStarted)
                    {
                        continue;
                    }

                    current.TargetSerialAssigned = assignResult == TargetSerialAssignResult.Assigned;
                    current.WaitingForTargetSlotClear = assignResult == TargetSerialAssignResult.SlotBusy;
                    current.ExpectedTargetSlot = targetSlot;
                    current.BlockedByCameraKey = blockingCameraKey;
                }
            }
        }

        /// <summary>
        /// Starts a camera migration: registers the migration, issues the factory reset on the source
        /// codec, then (after a short delay) kicks off the PoE-off → clear-serial → VLAN → PoE-on
        /// cascade. The cascade is the proven, feedback-driven machinery with attach-wait + auto-recovery
        /// + target-serial-assign. Callers MUST have confirmed the camera is genuinely online on the
        /// source codec before calling this. Returns false (and does nothing) if a migration for this
        /// camera is already in progress.
        /// </summary>
        private bool TryStartMigration(CiscoCamera camera, CiscoCodec sourceCodec, uint sourceCameraId, string targetCodecKey, string scenarioKey)
        {
            if (camera == null || sourceCodec == null || string.IsNullOrEmpty(targetCodecKey))
            {
                return false;
            }

            lock (activeMigrationsLock)
            {
                if (activeMigrations.ContainsKey(camera.Key))
                {
                    this.LogDebug($"Camera Manager {Key} not starting migration for camera '{camera.Key}': migration to a target is already in progress");
                    return false;
                }
            }

            var migration = new CameraMigrationState
            {
                CameraKey = camera.Key,
                Port = camera.NetworkSwitchPort,
                SourceCodecKey = sourceCodec.Key,
                SourceCameraId = sourceCameraId,
                TargetCodecKey = targetCodecKey
            };

            lock (activeMigrationsLock)
            {
                activeMigrations[camera.Key] = migration;
            }

            this.LogDebug($"CAMERA_SWITCHOVER_FACTORY_RESET_ISSUED camera='{camera.Key}' sourceCodec='{sourceCodec.Key}' sourceCameraId='{sourceCameraId}' targetCodec='{targetCodecKey}' scenario='{scenarioKey}'");
            sourceCodec.CameraFactoryReset(sourceCameraId);

            if (useCameraFactoryResetDisconnectFeedback)
            {
                lock (activeMigrationsLock)
                {
                    if (activeMigrations.TryGetValue(camera.Key, out var current) && ReferenceEquals(current, migration))
                    {
                        migration.WaitingForSourceDisconnect = true;
                        migration.DisconnectWaitDeadlineUtc = DateTime.UtcNow.AddMilliseconds(FactoryResetDisconnectTimeoutMs);
                    }
                }
                this.LogDebug($"Camera Manager {Key} sending factory reset for camera '{camera.Key}' on codec '{sourceCodec.Key}', then waiting for the source codec to report it disconnected before starting the PoE/VLAN cascade (fallback after {FactoryResetDisconnectTimeoutMs}ms)");
            }
            else
            {
                this.LogDebug($"Camera Manager {Key} sending factory reset for camera '{camera.Key}' on codec '{sourceCodec.Key}', then starting PoE/VLAN cascade after {factoryResetSettleMs}ms");
                ScheduleDelayed(factoryResetSettleMs, () => BeginMigrationPoeOffAndClearSerial(migration, sourceCodec));
            }
            return true;
        }

        /// <summary>
        /// Runs a one-shot action after the given delay. Used to space the factory reset and the
        /// start of the PoE/VLAN cascade without blocking the codec event thread.
        /// </summary>
        private void ScheduleDelayed(int delayMs, Action action)
        {
            var timer = new Timer(delayMs) { AutoReset = false };
            timer.Elapsed += (s, e) =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    this.LogError($"Camera Manager {Key} delayed migration action failed: {ex.Message}");
                }
                finally
                {
                    timer.Dispose();
                }
            };
            timer.Start();
        }

        /// <summary>
        /// Starts the CLI-driven migration cascade for a camera that was confirmed online on the wrong
        /// codec: turns PoE off on the camera's port and clears the assigned serial on the source codec.
        /// The remaining steps (VLAN change, PoE on, attach wait) are driven by network-switch port
        /// feedback exactly as before. Does nothing if the migration was superseded/cancelled.
        /// </summary>
        private void BeginMigrationPoeOffAndClearSerial(CameraMigrationState migration, CiscoCodec sourceCodec)
        {
            if (migration == null)
            {
                return;
            }

            lock (activeMigrationsLock)
            {
                if (!activeMigrations.TryGetValue(migration.CameraKey, out var current) || !ReferenceEquals(current, migration))
                {
                    this.LogDebug($"Camera Manager {Key} skipping delayed PoE-off for camera '{migration.CameraKey}': migration is no longer active");
                    return;
                }
            }

            if (disablePoeCycling)
            {
                // PoE cycling disabled: skip the PoE-off step entirely. Clear the source codec's
                // assigned serial and advance straight to the VLAN switch. The PoEDisabled event
                // that normally drives TryIssueVlanSwitch will never fire, so synthesize the
                // confirmation and call it directly (if the serial clear is not yet confirmed, the
                // serial-changed feedback will retry the VLAN switch later).
                this.LogDebug($"CAMERA_SWITCHOVER_MIGRATION_STARTED camera='{migration.CameraKey}' sourceCodec='{migration.SourceCodecKey}' sourceCameraId='{migration.SourceCameraId}' targetCodec='{migration.TargetCodecKey}' port='{migration.Port}' actions='ClearAssignedSerial' poeCycling='disabled'");
                this.LogDebug($"Camera Manager {Key} PoE cycling disabled — skipping PoE-off for camera '{migration.CameraKey}' on port '{migration.Port}', clearing assigned serial and advancing to VLAN switch");
                sourceCodec?.ClearCameraAssignedSerialNumber(migration.SourceCameraId);
                lock (activeMigrationsLock)
                {
                    if (activeMigrations.TryGetValue(migration.CameraKey, out var current) && ReferenceEquals(current, migration))
                    {
                        migration.AssignmentClearDeadlineUtc = DateTime.UtcNow.AddMilliseconds(AssignmentClearTimeoutMs);
                    }
                }

                CameraMigrationState migrationForVlanSwitch = null;
                lock (activeMigrationsLock)
                {
                    if (activeMigrations.TryGetValue(migration.CameraKey, out var current) && ReferenceEquals(current, migration))
                    {
                        migration.PoeDisabledConfirmed = true;
                        migrationForVlanSwitch = migration;
                    }
                }

                if (migrationForVlanSwitch != null)
                {
                    TryIssueVlanSwitch(migrationForVlanSwitch);
                }
                return;
            }

            this.LogDebug($"CAMERA_SWITCHOVER_MIGRATION_STARTED camera='{migration.CameraKey}' sourceCodec='{migration.SourceCodecKey}' sourceCameraId='{migration.SourceCameraId}' targetCodec='{migration.TargetCodecKey}' port='{migration.Port}' actions='PoeOff+ClearAssignedSerial'");
            this.LogDebug($"Camera Manager {Key} turning off PoE for camera '{migration.CameraKey}' on network switch port '{migration.Port}'");
            networkSwitch.SetPortPoeState(migration.Port, false);

            this.LogDebug($"Camera Manager {Key} clearing assigned serial number for camera '{migration.CameraKey}' on source codec '{migration.SourceCodecKey}'");
            sourceCodec?.ClearCameraAssignedSerialNumber(migration.SourceCameraId);
            lock (activeMigrationsLock)
            {
                if (activeMigrations.TryGetValue(migration.CameraKey, out var current) && ReferenceEquals(current, migration))
                {
                    migration.AssignmentClearDeadlineUtc = DateTime.UtcNow.AddMilliseconds(AssignmentClearTimeoutMs);
                }
            }
        }

        private void AttachVerificationTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (System.Threading.Interlocked.Exchange(ref attachVerificationTimerHandlerActive, 1) == 1)
            {
                return;
            }

            try
            {
                TryRunDeferredStartupReconciliation();

                List<string> pendingAttachKeys;
                List<string> pendingPoeSafeguardKeys;
                List<string> pendingPoeReenableRetryKeys;
                List<string> pendingDisconnectWaitKeys;
                List<string> pendingAssignmentClearTimeoutKeys;
                var now = DateTime.UtcNow;
                lock (activeMigrationsLock)
                {
                    pendingAttachKeys = activeMigrations.Values
                        .Where(m => m.AttachWaitStarted && m.AttachWaitDeadlineUtc <= now)
                        .Select(m => m.CameraKey)
                        .ToList();

                    pendingPoeSafeguardKeys = activeMigrations.Values
                        .Where(m => m.PoeDisabledConfirmed
                            && !m.PoeEnableIssued
                            && !m.PoeOffSafeguardTriggered
                            && m.PoeOffDeadlineUtc != DateTime.MinValue
                            && m.PoeOffDeadlineUtc <= now)
                        .Select(m => m.CameraKey)
                        .ToList();

                    pendingPoeReenableRetryKeys = activeMigrations.Values
                        .Where(m => m.VlanChangedConfirmed
                            && m.PoeEnableIssued
                            && !m.AttachWaitStarted
                            && m.PoeReenableDeadlineUtc != DateTime.MinValue
                            && m.PoeReenableDeadlineUtc <= now)
                        .Select(m => m.CameraKey)
                        .ToList();

                    pendingDisconnectWaitKeys = activeMigrations.Values
                        .Where(m => m.WaitingForSourceDisconnect
                            && m.DisconnectWaitDeadlineUtc != DateTime.MinValue
                            && m.DisconnectWaitDeadlineUtc <= now)
                        .Select(m => m.CameraKey)
                        .ToList();

                    pendingAssignmentClearTimeoutKeys = activeMigrations.Values
                        .Where(m => !m.AssignmentClearedConfirmed
                            && m.AssignmentClearDeadlineUtc != DateTime.MinValue
                            && m.AssignmentClearDeadlineUtc <= now)
                        .Select(m => m.CameraKey)
                        .ToList();
                }

                foreach (var migrationKey in pendingDisconnectWaitKeys)
                {
                    CameraMigrationState migrationForCascade = null;
                    string sourceCodecKey = null;
                    lock (activeMigrationsLock)
                    {
                        if (!activeMigrations.TryGetValue(migrationKey, out var migration)
                            || !migration.WaitingForSourceDisconnect
                            || migration.DisconnectWaitDeadlineUtc == DateTime.MinValue
                            || migration.DisconnectWaitDeadlineUtc > now)
                        {
                            continue;
                        }

                        migration.WaitingForSourceDisconnect = false;
                        migrationForCascade = migration;
                        sourceCodecKey = migration.SourceCodecKey;
                    }

                    this.LogDebug($"CAMERA_SWITCHOVER_FACTORY_RESET_DISCONNECT_TIMEOUT camera='{migrationForCascade.CameraKey}' sourceCodec='{sourceCodecKey}' targetCodec='{migrationForCascade.TargetCodecKey}' port='{migrationForCascade.Port}' timeoutMs='{FactoryResetDisconnectTimeoutMs}' action='startCascadeAnyway'");
                    this.LogWarning($"Camera Manager {Key} did not receive a source-codec disconnect for camera '{migrationForCascade.CameraKey}' within {FactoryResetDisconnectTimeoutMs}ms of the factory reset — starting the PoE/VLAN cascade anyway");
                    var sourceCodecForCascade = managedCodecs.TryGetValue(sourceCodecKey, out var sourceDevice) ? sourceDevice as CiscoCodec : null;
                    BeginMigrationPoeOffAndClearSerial(migrationForCascade, sourceCodecForCascade);
                }

                foreach (var migrationKey in pendingAssignmentClearTimeoutKeys)
                {
                    CameraMigrationState migrationForFallback = null;
                    lock (activeMigrationsLock)
                    {
                        if (!activeMigrations.TryGetValue(migrationKey, out var migration)
                            || migration.AssignmentClearedConfirmed
                            || migration.AssignmentClearDeadlineUtc == DateTime.MinValue
                            || migration.AssignmentClearDeadlineUtc > now)
                        {
                            continue;
                        }

                        migration.AssignmentClearedConfirmed = true;
                        migration.AssignmentClearDeadlineUtc = DateTime.MinValue;
                        migrationForFallback = migration;
                    }

                    this.LogDebug($"CAMERA_SWITCHOVER_ASSIGNED_CLEARED_TIMEOUT camera='{migrationForFallback.CameraKey}' sourceCodec='{migrationForFallback.SourceCodecKey}' sourceCameraId='{migrationForFallback.SourceCameraId}' targetCodec='{migrationForFallback.TargetCodecKey}' port='{migrationForFallback.Port}' timeoutMs='{AssignmentClearTimeoutMs}' action='forceVlanSwitchAnyway'");
                    this.LogWarning($"Camera Manager {Key} did not receive assigned-serial clear feedback for camera '{migrationForFallback.CameraKey}' within {AssignmentClearTimeoutMs}ms of clearing the source codec serial — forcing the VLAN switch to avoid a stuck migration");
                    TryIssueVlanSwitch(migrationForFallback);
                }

                foreach (var migrationKey in pendingPoeReenableRetryKeys)
                {
                    string cameraKey;
                    string sourceCodecKey;
                    uint sourceCameraId;
                    string targetCodecKey;
                    string port;
                    lock (activeMigrationsLock)
                    {
                        if (!activeMigrations.TryGetValue(migrationKey, out var migration)
                            || !migration.VlanChangedConfirmed
                            || !migration.PoeEnableIssued
                            || migration.AttachWaitStarted
                            || migration.PoeReenableDeadlineUtc == DateTime.MinValue
                            || migration.PoeReenableDeadlineUtc > now)
                        {
                            continue;
                        }

                        migration.PoeReenableDeadlineUtc = DateTime.UtcNow.AddMilliseconds(AttachWaitTimeoutMs);
                        cameraKey = migration.CameraKey;
                        sourceCodecKey = migration.SourceCodecKey;
                        sourceCameraId = migration.SourceCameraId;
                        targetCodecKey = migration.TargetCodecKey;
                        port = migration.Port;
                    }

                    this.LogDebug($"CAMERA_SWITCHOVER_POE_REENABLE_RETRY camera='{cameraKey}' sourceCodec='{sourceCodecKey}' sourceCameraId='{sourceCameraId}' targetCodec='{targetCodecKey}' port='{port}' reason='poeEnabledEventNotConfirmed'");
                    this.LogDebug($"Camera Manager {Key} retrying PoE re-enable for camera '{cameraKey}' on port '{port}' — PoEEnabled event was not confirmed after VLAN change");
                    networkSwitch.SetPortPoeState(port, true);
                }

                foreach (var migrationKey in pendingPoeSafeguardKeys)
                {
                    string cameraKey;
                    string sourceCodecKey;
                    uint sourceCameraId;
                    string targetCodecKey;
                    string port;
                    lock (activeMigrationsLock)
                    {
                        if (!activeMigrations.TryGetValue(migrationKey, out var migration)
                            || !migration.PoeDisabledConfirmed
                            || migration.PoeEnableIssued
                            || migration.PoeOffSafeguardTriggered
                            || migration.PoeOffDeadlineUtc == DateTime.MinValue
                            || migration.PoeOffDeadlineUtc > now)
                        {
                            continue;
                        }

                        migration.PoeOffSafeguardTriggered = true;
                        cameraKey = migration.CameraKey;
                        sourceCodecKey = migration.SourceCodecKey;
                        sourceCameraId = migration.SourceCameraId;
                        targetCodecKey = migration.TargetCodecKey;
                        port = migration.Port;
                    }

                    this.LogDebug($"CAMERA_SWITCHOVER_POE_SAFEGUARD_TRIGGERED camera='{cameraKey}' sourceCodec='{sourceCodecKey}' sourceCameraId='{sourceCameraId}' targetCodec='{targetCodecKey}' port='{port}' action='forcePoeOnAfterOffTimeout' maxOffMs='{MaxPoeOffDurationMs}'");
                    this.LogDebug($"Camera Manager {Key} forcing PoE on for camera '{cameraKey}' after extended PoE-off interval");
                    networkSwitch.SetPortPoeState(port, true);
                }

                foreach (var migrationKey in pendingAttachKeys)
                {
                    string cameraKey;
                    string sourceCodecKey;
                    uint sourceCameraId;
                    string targetCodecKey;
                    string port;
                    lock (activeMigrationsLock)
                    {
                        if (!activeMigrations.TryGetValue(migrationKey, out var migration)
                            || !migration.AttachWaitStarted
                            || migration.AttachWaitDeadlineUtc > now)
                        {
                            continue;
                        }

                        cameraKey = migration.CameraKey;
                        sourceCodecKey = migration.SourceCodecKey;
                        sourceCameraId = migration.SourceCameraId;
                        targetCodecKey = migration.TargetCodecKey;
                        port = migration.Port;

                        // On attach timeout we always reseed the camera back onto the source codec.
                        // Reasserting the target VLAN/PoE in place never re-prompts the codec to
                        // discover the camera; only forcing it back to the source (which makes the
                        // source codec rediscover it and fire a fresh factory-reset migration cascade)
                        // is proven to recover a stuck attach. Clearing the migration here lets that
                        // rediscovery start a brand-new migration from scratch.
                        activeMigrations.Remove(migration.CameraKey);

                        // Give the normal rediscovery cascade a grace period before the floating-camera
                        // watchdog can intervene on this camera. If the source codec re-reports it (the
                        // proven recovery path), a fresh migration starts well within this window.
                        reconcileNextActionUtc[migration.CameraKey] = DateTime.UtcNow.AddMilliseconds(ReconcileBackoffMs);
                    }

                    int attachFailureCount;
                    lock (activeMigrationsLock)
                    {
                        attachFailureCounts.TryGetValue(cameraKey, out var priorCount);
                        attachFailureCount = priorCount + 1;
                        attachFailureCounts[cameraKey] = attachFailureCount;
                    }

                    this.LogDebug($"CAMERA_SWITCHOVER_ATTACH_FAILED camera='{cameraKey}' sourceCodec='{sourceCodecKey}' sourceCameraId='{sourceCameraId}' targetCodec='{targetCodecKey}' port='{port}' failedAttempts='{attachFailureCount}' action='reseedSourceVlanAndPoe'");
                    this.LogDebug($"CAMERA_SWITCHOVER_ATTACH_AUTOMAGIC_RECOVERY_TRIGGERED camera='{cameraKey}' sourceCodec='{sourceCodecKey}' sourceCameraId='{sourceCameraId}' targetCodec='{targetCodecKey}' port='{port}' phase='failed' failedAttempts='{attachFailureCount}' action='reseedSourceVlanAndPoe'");
                    this.LogDebug($"Camera Manager {Key} attach failed diagnostics camera='{cameraKey}' failedAttempts='{attachFailureCount}' managed='{BuildManagedCameraSnapshot(cameraKey)}' sourceSnapshot='{BuildCodecCameraSnapshot(sourceCodecKey)}' targetSnapshot='{BuildCodecCameraSnapshot(targetCodecKey)}'");

                    if (managedCodecs.TryGetValue(sourceCodecKey, out var sourceCodecDevice))
                    {
                        this.LogDebug($"Camera Manager {Key} re-seeding source VLAN for camera '{cameraKey}' after attach failure to force rediscovery (poeCycling='{(disablePoeCycling ? "disabled" : "enabled")}')");
                        networkSwitch.SetPortVlan(port, sourceCodecDevice.VLanId);
                        if (!disablePoeCycling)
                        {
                            networkSwitch.SetPortPoeState(port, true);
                        }
                    }
                    else
                    {
                        this.LogError($"Camera Manager {Key} cannot run attach failure reseed for camera '{cameraKey}': source codec '{sourceCodecKey}' not found");
                    }
                }

                // Periodic safety-net sweep: catch cameras that ended up floating with no active
                // migration (the recovery dead-end). Throttled to ReconcileSweepIntervalMs.
                if (now >= nextReconcileSweepUtc)
                {
                    nextReconcileSweepUtc = DateTime.UtcNow.AddMilliseconds(ReconcileSweepIntervalMs);
                    TryReconcileFloatingCameras(now);
                }
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref attachVerificationTimerHandlerActive, 0);
            }
        }

        /// <summary>
        /// Safety-net watchdog. For the current scenario, finds cameras that are NOT correctly
        /// placed on their target codec and have no active migration, then nudges them back toward
        /// recovery. This closes the recovery dead-end where, after an attach-failure reseed, the
        /// source codec never re-reports the camera online — leaving it floating with nothing to
        /// restart the migration cascade.
        /// </summary>
        private void TryReconcileFloatingCameras(DateTime now)
        {
            var currentScenario = roomCombiner?.CurrentScenario;
            if (currentScenario == null
                || !config.RoomCombinerConfig.CombineScenarios.TryGetValue(currentScenario.Key, out var scenarioConfig))
            {
                return;
            }

            foreach (var codecConfig in scenarioConfig.CodecConfigs)
            {
                var targetCodecKey = codecConfig.CodecKey;
                foreach (var cameraKey in codecConfig.CameraKeys)
                {
                    if (!managedCameras.TryGetValue(cameraKey, out var camera))
                    {
                        continue;
                    }

                    bool hasActiveMigration;
                    DateTime nextActionUtc;
                    lock (activeMigrationsLock)
                    {
                        hasActiveMigration = activeMigrations.ContainsKey(cameraKey);
                        reconcileNextActionUtc.TryGetValue(cameraKey, out nextActionUtc);
                    }

                    // The migration machinery already owns this camera.
                    if (hasActiveMigration)
                    {
                        continue;
                    }

                    // Correctly placed = confirmed online on the target codec with a matching serial.
                    if (IsCameraOnlineOnCodec(targetCodecKey, camera))
                    {
                        ClearFloatingRecoveryState(cameraKey);
                        continue;
                    }

                    // Floating. Back off so the normal rediscovery cascade (and any in-flight
                    // recovery) gets time to finish before the watchdog intervenes.
                    if (now < nextActionUtc)
                    {
                        continue;
                    }

                    var currentCodecKey = FindCodecKeyWhereCameraOnline(camera);
                    var port = camera.NetworkSwitchPort;

                    if (!string.IsNullOrEmpty(currentCodecKey)
                        && !string.Equals(currentCodecKey, targetCodecKey, StringComparison.OrdinalIgnoreCase))
                    {
                        // Stuck online on the wrong codec with no connect event to drive a migration.
                        // Start the migration cascade now.
                        this.LogDebug($"CAMERA_SWITCHOVER_RECONCILE_WRONG_CODEC camera='{cameraKey}' currentCodec='{currentCodecKey}' targetCodec='{targetCodecKey}' port='{port}' scenario='{currentScenario.Key}' action='startMigration'");
                        if (managedCodecs.TryGetValue(currentCodecKey, out var sourceCodecDevice)
                            && sourceCodecDevice is CiscoCodec sourceCiscoCodec)
                        {
                            ClearFloatingRecoveryState(cameraKey);
                            lock (activeMigrationsLock)
                            {
                                reconcileNextActionUtc[cameraKey] = DateTime.UtcNow.AddMilliseconds(ReconcileBackoffMs);
                            }
                            // Resolve the live source slot by serial so the reset/clear hit the
                            // physical slot, and align the camera pin to it (never the target) while
                            // it is still on the source codec.
                            if (TryResolveOnlineSourceSlot(sourceCiscoCodec, camera, out var actualSourceSlot))
                            {
                                camera.SetScenarioCameraId(actualSourceSlot);
                                TryStartMigration(camera, sourceCiscoCodec, actualSourceSlot, targetCodecKey, currentScenario.Key);
                            }
                            else
                            {
                                this.LogDebug($"Camera Manager {Key} reconcile deferring migration for camera '{cameraKey}': could not resolve live source slot on codec '{currentCodecKey}'");
                            }
                        }
                        else
                        {
                            this.LogError($"Camera Manager {Key} reconcile cannot start migration for camera '{cameraKey}': current codec '{currentCodecKey}' not a managed CiscoCodec");
                        }
                        continue;
                    }

                    // Online nowhere: the recovery dead-end. The camera never came back after a
                    // reseed (firmware hang, cable/PoE fault, or it simply never re-registered).
                    // Bounce PoE on the port to give it another chance to power up and re-register;
                    // once it reports online anywhere, the connect handler or the next sweep routes it.
                    if (string.IsNullOrWhiteSpace(port))
                    {
                        this.LogDebug($"CAMERA_SWITCHOVER_RECONCILE_FLOATING camera='{cameraKey}' targetCodec='{targetCodecKey}' scenario='{currentScenario.Key}' action='none' reason='noNetworkSwitchPort'");
                        continue;
                    }

                    int floatingCount;
                    lock (activeMigrationsLock)
                    {
                        if (!floatingRecoveryCounts.TryGetValue(cameraKey, out var priorCount))
                        {
                            priorCount = 0;
                        }

                        floatingCount = priorCount + 1;
                        floatingRecoveryCounts[cameraKey] = floatingCount;
                    }

                    if (floatingCount > FloatingRecoveryAttemptLimit)
                    {
                        lock (activeMigrationsLock)
                        {
                            reconcileNextActionUtc[cameraKey] = DateTime.MaxValue;
                        }

                        this.LogWarning($"CAMERA_SWITCHOVER_RECONCILE_FLOATING camera='{cameraKey}' targetCodec='{targetCodecKey}' port='{port}' scenario='{currentScenario.Key}' action='none' reason='attemptLimitReached' attempts='{floatingCount - 1}' limit='{FloatingRecoveryAttemptLimit}' managed='{BuildManagedCameraSnapshot(cameraKey)}'");
                        continue;
                    }

                    lock (activeMigrationsLock)
                    {
                        reconcileNextActionUtc[cameraKey] = DateTime.UtcNow.AddMilliseconds(ReconcileBackoffMs);
                    }

                    var probeCodecKey = GetFloatingRecoveryProbeCodecKey(cameraKey, targetCodecKey, scenarioConfig, floatingCount, out var probeIndex, out var probeTotal);
                    if (string.IsNullOrWhiteSpace(probeCodecKey))
                    {
                        this.LogWarning($"CAMERA_SWITCHOVER_RECONCILE_FLOATING camera='{cameraKey}' targetCodec='{targetCodecKey}' port='{port}' scenario='{currentScenario.Key}' action='none' reason='noProbeCodec' attempt='{floatingCount}' managed='{BuildManagedCameraSnapshot(cameraKey)}'");
                        continue;
                    }

                    if (!managedCodecs.TryGetValue(probeCodecKey, out var probeCodecDevice))
                    {
                        this.LogError($"Camera Manager {Key} cannot run floating recovery for camera '{cameraKey}': probe codec '{probeCodecKey}' is not managed");
                        continue;
                    }

                    this.LogDebug($"CAMERA_SWITCHOVER_RECONCILE_FLOATING camera='{cameraKey}' targetCodec='{targetCodecKey}' probeCodec='{probeCodecKey}' probeIndex='{probeIndex}' probeTotal='{probeTotal}' port='{port}' scenario='{currentScenario.Key}' action='probeAndBouncePoe' attempt='{floatingCount}' poeAttempt='{floatingCount}' poeAttemptLimit='{FloatingRecoveryAttemptLimit}' managed='{BuildManagedCameraSnapshot(cameraKey)}'");
                    networkSwitch.SetPortVlan(port, probeCodecDevice.VLanId);
                    networkSwitch.SetPortPoeState(port, false);
                    ScheduleDelayed(MigrationPoeOffDelayMs, () =>
                    {
                        this.LogDebug($"Camera Manager {Key} reconcile re-enabling PoE for floating camera '{cameraKey}' on port '{port}'");
                        networkSwitch.SetPortPoeState(port, true);
                    });
                }
            }
        }

        private string GetFloatingRecoveryProbeCodecKey(
            string cameraKey,
            string targetCodecKey,
            CameraManagerCombineScenarioConfig scenarioConfig,
            int attempt,
            out int probeIndex,
            out int probeTotal)
        {
            probeIndex = 0;
            probeTotal = 0;

            if (scenarioConfig?.CodecConfigs == null || attempt <= 0)
            {
                return null;
            }

            var probeCodecKeys = new List<string>();
            if (!string.IsNullOrWhiteSpace(targetCodecKey))
            {
                probeCodecKeys.Add(targetCodecKey);
            }

            foreach (var codecConfig in scenarioConfig.CodecConfigs)
            {
                if (!string.IsNullOrWhiteSpace(codecConfig?.CodecKey)
                    && !probeCodecKeys.Any(k => string.Equals(k, codecConfig.CodecKey, StringComparison.OrdinalIgnoreCase)))
                {
                    probeCodecKeys.Add(codecConfig.CodecKey);
                }
            }

            // Add codecs that own this camera in any configured scenario. This keeps probing
            // focused to relevant codec candidates and enables deterministic alternation like
            // A -> B -> A -> B when a camera is only mapped to A/B.
            if (!string.IsNullOrWhiteSpace(cameraKey) && config?.RoomCombinerConfig?.CombineScenarios != null)
            {
                foreach (var combineScenario in config.RoomCombinerConfig.CombineScenarios.Values)
                {
                    if (combineScenario?.CodecConfigs == null)
                    {
                        continue;
                    }

                    foreach (var codecConfig in combineScenario.CodecConfigs)
                    {
                        if (codecConfig?.CameraKeys == null)
                        {
                            continue;
                        }

                        if (!codecConfig.CameraKeys.Any(k => string.Equals(k, cameraKey, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }

                        if (!string.IsNullOrWhiteSpace(codecConfig.CodecKey)
                            && !probeCodecKeys.Any(k => string.Equals(k, codecConfig.CodecKey, StringComparison.OrdinalIgnoreCase)))
                        {
                            probeCodecKeys.Add(codecConfig.CodecKey);
                        }
                    }
                }
            }

            // Always include every managed codec in the tail of the probe list. This ensures
            // floating recovery can sweep all possible codec pairings (including 3-way rooms),
            // while still preferring target and scenario-specific codecs first.
            foreach (var managedCodecKey in managedCodecs.Keys)
            {
                if (!string.IsNullOrWhiteSpace(managedCodecKey)
                    && !probeCodecKeys.Any(k => string.Equals(k, managedCodecKey, StringComparison.OrdinalIgnoreCase)))
                {
                    probeCodecKeys.Add(managedCodecKey);
                }
            }

            if (probeCodecKeys.Count == 0)
            {
                return null;
            }

            var selectedIndex = (attempt - 1) % probeCodecKeys.Count;
            probeIndex = selectedIndex + 1;
            probeTotal = probeCodecKeys.Count;
            return probeCodecKeys[selectedIndex];
        }

        private void ClearFloatingRecoveryState(string cameraKey)
        {
            if (string.IsNullOrWhiteSpace(cameraKey))
            {
                return;
            }

            lock (activeMigrationsLock)
            {
                floatingRecoveryCounts.Remove(cameraKey);
                reconcileNextActionUtc.Remove(cameraKey);
            }
        }

        private void ClearAllFloatingRecoveryState()
        {
            lock (activeMigrationsLock)
            {
                floatingRecoveryCounts.Clear();
                reconcileNextActionUtc.Clear();
            }
        }

        private bool IsCameraOnlineOnCodec(string codecKey, CiscoCamera camera)
        {
            if (string.IsNullOrWhiteSpace(codecKey) || camera == null || string.IsNullOrWhiteSpace(camera.SerialNumber))
            {
                return false;
            }

            if (!managedCodecs.TryGetValue(codecKey, out var codecDevice))
            {
                return false;
            }

            var codec = codecDevice as CiscoCodec;
            if (codec == null)
            {
                return false;
            }

            return codec.Cameras?.OfType<CiscoCamera>().Any(c => c.IsOnline
                && !string.IsNullOrEmpty(c.SerialNumber)
                && string.Equals(c.SerialNumber, camera.SerialNumber, StringComparison.OrdinalIgnoreCase)) == true;
        }

        private string FindCodecKeyWhereCameraOnline(CiscoCamera camera)
        {
            if (camera == null || string.IsNullOrWhiteSpace(camera.SerialNumber))
            {
                return null;
            }

            foreach (var kvp in managedCodecs)
            {
                var codec = kvp.Value as CiscoCodec;
                if (codec == null)
                {
                    continue;
                }

                var match = codec.Cameras?.OfType<CiscoCamera>().Any(c => c.IsOnline
                    && !string.IsNullOrEmpty(c.SerialNumber)
                    && string.Equals(c.SerialNumber, camera.SerialNumber, StringComparison.OrdinalIgnoreCase)) == true;
                if (match)
                {
                    return kvp.Key;
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves the live slot (CameraId) that the given managed camera currently occupies on the
        /// specified source codec, matched by serial number and confirmed online. Returns false when
        /// the camera cannot be found online on that codec, so callers never issue a factory reset or
        /// serial clear against a guessed/stale slot.
        /// </summary>
        private bool TryResolveOnlineSourceSlot(CiscoCodec sourceCodec, CiscoCamera camera, out uint sourceSlot)
        {
            sourceSlot = 0;
            if (sourceCodec == null || camera == null || string.IsNullOrWhiteSpace(camera.SerialNumber))
            {
                return false;
            }

            var match = sourceCodec.Cameras?.OfType<CiscoCamera>()
                .FirstOrDefault(c => c.IsOnline
                    && !string.IsNullOrEmpty(c.SerialNumber)
                    && string.Equals(c.SerialNumber, camera.SerialNumber, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                return false;
            }

            sourceSlot = match.CameraId;
            return true;
        }

        private void RoomCombiner_RoomCombinationScenarioChanged(object sender, EventArgs e)
        {
            var currentScenario = roomCombiner.CurrentScenario;

            // A scenario-changed event covers the startup case too, so any pending deferred
            // reconciliation is now satisfied by this run.
            System.Threading.Interlocked.Exchange(ref startupReconciliationPending, 0);

            ClearAllFloatingRecoveryState();

            this.LogDebug($"Camera Manager {Key} detected room combination scenario change to '{currentScenario?.Key}'");

            RunScenarioReconciliation(currentScenario?.Key);
        }

        /// <summary>
        /// Runs the full reconciliation pass for a scenario: factory-reset migrations for cameras
        /// on the wrong codec, then port-state (VLAN/PoE) enforcement.
        /// </summary>
        private void RunScenarioReconciliation(string scenarioKey)
        {
            TryExecuteScenarioCameraResets(scenarioKey);
            TryEnsureScenarioCameraPortStates(scenarioKey);
        }

        /// <summary>
        /// If activation deferred the initial reconciliation because the room combiner had not yet
        /// resolved its scenario, run it now that a scenario is available. Invoked from the
        /// attach-verification timer so it retries every second until the combiner is ready.
        /// </summary>
        private void TryRunDeferredStartupReconciliation()
        {
            if (System.Threading.Volatile.Read(ref startupReconciliationPending) == 0)
            {
                return;
            }

            var scenarioKey = roomCombiner?.CurrentScenario?.Key;
            if (string.IsNullOrEmpty(scenarioKey))
            {
                return;
            }

            if (System.Threading.Interlocked.Exchange(ref startupReconciliationPending, 0) == 0)
            {
                return;
            }

            this.LogDebug($"Camera Manager {Key} running deferred startup reconciliation for scenario '{scenarioKey}' (room combiner resolved its scenario after activation)");
            RunScenarioReconciliation(scenarioKey);
        }

        private void TryEnsureScenarioCameraPortStates(string scenarioKey)
        {
            if (string.IsNullOrEmpty(scenarioKey))
            {
                return;
            }

            if (!config.RoomCombinerConfig.CombineScenarios.TryGetValue(scenarioKey, out var scenarioConfig))
            {
                return;
            }

            foreach (var codecConfig in scenarioConfig.CodecConfigs)
            {
                if (!managedCodecs.TryGetValue(codecConfig.CodecKey, out var codec))
                {
                    continue;
                }

                foreach (var cameraKey in codecConfig.CameraKeys)
                {
                    if (!managedCameras.TryGetValue(cameraKey, out var camera))
                    {
                        continue;
                    }

                    bool hasActiveMigration;
                    lock (activeMigrationsLock)
                    {
                        hasActiveMigration = activeMigrations.ContainsKey(cameraKey);
                    }

                    if (hasActiveMigration)
                    {
                        this.LogDebug($"Camera Manager {Key} skipping port-state ensure for camera '{cameraKey}': active migration in progress");
                        continue;
                    }

                    // Target-online gate: only re-assert the port VLAN/PoE for a camera we can CONFIRM
                    // is already correctly homed on the target codec. This is an idempotent alignment
                    // for cameras that are already migrated. We must NOT move the VLAN for a camera that
                    // is not yet on the target codec: doing so strands the camera on the target VLAN
                    // before the migration cascade (factory reset -> clear serial -> VLAN -> assign serial)
                    // has run. Once stranded on the target VLAN, the source codec can never see the
                    // camera report online on the "wrong codec", so the deferred connect-handler
                    // migration (Codec_CameraConnected -> TryStartMigration) never fires and the camera
                    // floats unpaired. Leaving the port on its current VLAN keeps the camera visible on
                    // its source codec so the reset/connect migration path can move it properly.
                    var port = camera.NetworkSwitchPort;
                    var vlanId = codec.VLanId;

                    if (!IsCameraOnlineOnCodec(codecConfig.CodecKey, camera))
                    {
                        this.LogDebug($"CAMERA_PORT_ENSURE camera='{cameraKey}' targetCodec='{codecConfig.CodecKey}' port='{port}' vlan='{vlanId}' scenario='{scenarioKey}' action='skipped' reason='notOnlineOnTargetCodec' - leaving port VLAN unchanged so the migration path can move the camera");
                        continue;
                    }

                    this.LogDebug($"CAMERA_PORT_ENSURE camera='{cameraKey}' targetCodec='{codecConfig.CodecKey}' port='{port}' vlan='{vlanId}' scenario='{scenarioKey}' poeCycling='{(disablePoeCycling ? "disabled" : "enabled")}'");
                    networkSwitch.SetPortVlan(port, vlanId);
                    if (!disablePoeCycling)
                    {
                        networkSwitch.SetPortPoeState(port, true);
                    }
                }
            }
        }

        private void TryExecuteScenarioCameraResets(string scenarioKey)
        {
            if (string.IsNullOrEmpty(scenarioKey))
            {
                this.LogWarning($"Camera Manager {Key} cannot execute camera reset workflow because the current scenario key is empty");
                return;
            }

            if (config.RoomCombinerConfig.CombineScenarios.TryGetValue(scenarioKey, out var scenarioConfig))
            {
                foreach (var codecConfig in scenarioConfig.CodecConfigs)
                {
                    if (!managedCodecs.TryGetValue(codecConfig.CodecKey, out var codec))
                    {
                        this.LogError($"Camera Manager {Key} error: Codec with key '{codecConfig.CodecKey}' from scenario config not found in managed codecs");
                        continue;
                    }

                    foreach (var cameraKey in codecConfig.CameraKeys)
                    {
                        if (!managedCameras.TryGetValue(cameraKey, out var camera))
                        {
                            this.LogError($"Camera Manager {Key} error: Camera with key '{cameraKey}' from scenario config not found in managed cameras");
                            continue;
                        }

                        var currentParentCodecKey = camera.ParentCodec?.Key;
                        if (string.IsNullOrEmpty(currentParentCodecKey))
                        {
                            this.LogWarning($"Camera Manager {Key} cannot reset camera '{cameraKey}' for scenario '{scenarioKey}' because its parent codec is not available");
                            continue;
                        }

                        if (string.Equals(currentParentCodecKey, codecConfig.CodecKey, StringComparison.Ordinal))
                        {
                            // Already on the target codec: safe to pin the target slot now so
                            // self-heal enforces exactly the slot the manager wants. Pinning is only
                            // ever applied once the camera is physically on the codec that owns the slot.
                            camera.SetScenarioCameraId(codecConfig.GetConfiguredCameraId(cameraKey));
                            this.LogDebug($"Camera Manager {Key} skipping factory reset for camera '{cameraKey}' because it is already assigned to target codec '{codecConfig.CodecKey}' for scenario '{scenarioKey}'");
                            continue;
                        }

                        // Source-online gate: only factory-reset a camera we can CONFIRM is genuinely
                        // online on its current (source) codec. camera.ParentCodec is unreliable here —
                        // at startup it defaults to the configured parent codec and the codecs may have
                        // only just connected (no camera status reported yet). If we cannot see the
                        // camera online on the source codec, we do NOT blindly reset it; the connected
                        // handler will start a migration later if the camera actually shows up online on
                        // the wrong codec. This prevents resetting a camera that is already correctly
                        // paired on the target codec but whose ParentCodec is momentarily stale.
                        var sourceCiscoCodec = camera.ParentCodec;
                        var onlineOnSource = sourceCiscoCodec?.Cameras?.OfType<CiscoCamera>()
                            .Any(c => c.IsOnline
                                      && !string.IsNullOrEmpty(c.SerialNumber)
                                      && string.Equals(c.SerialNumber, camera.SerialNumber, StringComparison.OrdinalIgnoreCase));
                        if (onlineOnSource != true)
                        {
                            this.LogDebug($"Camera Manager {Key} skipping factory reset for camera '{cameraKey}': not confirmed online on source codec '{currentParentCodecKey}' for scenario '{scenarioKey}' — deferring to connect handler once real status is reported");
                            continue;
                        }

                        // Resolve the REAL slot the camera occupies on the source codec (by serial),
                        // so the factory reset + serial clear target the physical source slot and
                        // never a stale or target-pinned CameraId. Do NOT pin the target slot here:
                        // pinning the target while the camera is still on the source makes self-heal
                        // rewrite the source codec and corrupts the migration. The target slot is
                        // pinned only when the camera attaches to the target codec.
                        if (!TryResolveOnlineSourceSlot(sourceCiscoCodec, camera, out var actualSourceSlot))
                        {
                            this.LogDebug($"Camera Manager {Key} skipping factory reset for camera '{cameraKey}': could not resolve its live source slot on codec '{currentParentCodecKey}' — deferring");
                            continue;
                        }

                        // Keep the camera object aligned to its actual source slot so source-side
                        // self-heal stays a no-op for the duration of the migration.
                        camera.SetScenarioCameraId(actualSourceSlot);

                        // Camera is confirmed online on the wrong (source) codec. Start the full
                        // migration cascade: factory reset → (500ms) → PoE off → clear serial → VLAN →
                        // PoE on → attach wait + auto-recovery + target-serial-assign.
                        this.LogDebug($"Camera Manager {Key} confirmed camera '{cameraKey}' online on source codec '{currentParentCodecKey}' slot {actualSourceSlot} — starting migration to '{codecConfig.CodecKey}' for scenario '{scenarioKey}'");
                        TryStartMigration(camera, sourceCiscoCodec, actualSourceSlot, codecConfig.CodecKey, scenarioKey);
                    }
                }
            }
            else
            {
                this.LogDebug($"Camera Manager {Key} has no configuration for room combination scenario '{scenarioKey}'");
            }
        }

        private void Codec_CameraAssignedSerialNumberChanged(object sender, CameraEventArgs e)
        {
            var assignedSerial = e.SerialNumber?.Trim();
            if (!string.IsNullOrEmpty(assignedSerial))
            {
                return;
            }

            var codec = sender as CiscoCodec;

            // Confirmation-driven target slot gating: when a codec confirms a slot's assigned serial
            // has been cleared, any migration waiting for that exact target slot may proceed.
            List<CameraMigrationState> waitingForTargetSlotClear;
            lock (activeMigrationsLock)
            {
                waitingForTargetSlotClear = activeMigrations.Values
                    .Where(m => m.AttachWaitStarted
                        && !m.TargetSerialAssigned
                        && m.WaitingForTargetSlotClear
                        && string.Equals(m.TargetCodecKey, codec?.Key, StringComparison.OrdinalIgnoreCase)
                        && m.ExpectedTargetSlot == e.CameraId)
                    .ToList();
            }

            foreach (var pendingMigration in waitingForTargetSlotClear)
            {
                var assignResult = TryAssignSerialToTargetCodec(
                    pendingMigration.CameraKey,
                    pendingMigration.TargetCodecKey,
                    pendingMigration.Port,
                    "targetSlotClearConfirmed",
                    out var targetSlot,
                    out var blockingCameraKey);

                lock (activeMigrationsLock)
                {
                    if (!activeMigrations.TryGetValue(pendingMigration.CameraKey, out var current)
                        || !ReferenceEquals(current, pendingMigration)
                        || !current.AttachWaitStarted)
                    {
                        continue;
                    }

                    current.TargetSerialAssigned = assignResult == TargetSerialAssignResult.Assigned;
                    current.WaitingForTargetSlotClear = assignResult == TargetSerialAssignResult.SlotBusy;
                    current.ExpectedTargetSlot = targetSlot;
                    current.BlockedByCameraKey = blockingCameraKey;
                }
            }

            CameraMigrationState migration;
            lock (activeMigrationsLock)
            {
                migration = activeMigrations.Values.FirstOrDefault(m =>
                    !m.AssignmentClearedConfirmed
                    && m.SourceCodecKey == codec?.Key
                    && m.SourceCameraId == e.CameraId);
            }

            if (migration == null && codec != null)
            {
                List<CameraMigrationState> pendingForCodec;
                lock (activeMigrationsLock)
                {
                    pendingForCodec = activeMigrations.Values
                        .Where(m => !m.AssignmentClearedConfirmed && m.SourceCodecKey == codec.Key)
                        .ToList();
                }

                if (pendingForCodec.Count == 1)
                {
                    migration = pendingForCodec[0];
                    this.LogWarning($"Camera Manager {Key} matched AssignedSerialNumber clear feedback to camera '{migration.CameraKey}' by source codec fallback. Expected cameraId={migration.SourceCameraId}, feedback cameraId={e.CameraId}");
                }
            }

            if (migration == null)
            {
                return;
            }

            migration.AssignmentClearedConfirmed = true;
            migration.AssignmentClearDeadlineUtc = DateTime.MinValue;
            this.LogDebug($"Camera Manager {Key} confirmed AssignedSerialNumber cleared for camera '{migration.CameraKey}' on codec '{migration.SourceCodecKey}', camera ID {migration.SourceCameraId}");
            TryIssueVlanSwitch(migration);
        }

        private void Codec_CameraDisconnected(object sender, CameraEventArgs e)
        {
            var codec = sender as CiscoCodec;

            var camera = ResolveManagedCamera(codec, e);
            if (camera == null)
            {
                this.LogDebug($"Camera Manager {Key} received CameraDisconnected event from codec {codec?.Key} for camera ID {e.CameraId} / serial {e.SerialNumber} but no managed camera was resolved");
                return;
            }

            // A disconnect from a blocker camera can unblock a waiting target-slot assignment.
            TryAdvanceMigrationsWaitingForTargetSlotClear(codec?.Key, "blockerCameraDisconnected", camera.Key);

            // Factory-reset settle via disconnect feedback: if a migration for this camera is
            // waiting for the source codec to report it dropped (the factory reset taking effect),
            // this disconnect from that source codec is the signal to start the PoE/VLAN cascade
            // now instead of waiting out the fixed settle timer. Resolved before the generic
            // disconnect guards because the source-codec + migration-state match is unambiguous.
            CameraMigrationState resetWaitMigration = null;
            lock (activeMigrationsLock)
            {
                if (activeMigrations.TryGetValue(camera.Key, out var waitingMigration)
                    && waitingMigration.WaitingForSourceDisconnect
                    && string.Equals(waitingMigration.SourceCodecKey, codec?.Key, StringComparison.OrdinalIgnoreCase))
                {
                    waitingMigration.WaitingForSourceDisconnect = false;
                    resetWaitMigration = waitingMigration;
                }
            }

            if (resetWaitMigration != null)
            {
                this.LogDebug($"CAMERA_SWITCHOVER_FACTORY_RESET_DISCONNECT_CONFIRMED camera='{camera.Key}' sourceCodec='{codec?.Key}' sourceCameraId='{e.CameraId}' targetCodec='{resetWaitMigration.TargetCodecKey}' port='{resetWaitMigration.Port}'");
                this.LogDebug($"Camera Manager {Key} source codec '{codec?.Key}' reported camera '{camera.Key}' disconnected after factory reset — starting PoE/VLAN cascade (disconnect-feedback gate)");
                BeginMigrationPoeOffAndClearSerial(resetWaitMigration, codec);
                return;
            }

            // Check if this camera is supposed to be on the codec that sent the disconnect event.
            // If so, this is a transient disconnect during camera initialization — ignore it to avoid a PoE/VLAN loop.
            var currentScenario = roomCombiner.CurrentScenario;
            if (currentScenario != null && config.RoomCombinerConfig.CombineScenarios.TryGetValue(currentScenario.Key, out var scenarioConfig))
            {
                var codecConfig = scenarioConfig.CodecConfigs.FirstOrDefault(cc => cc.CameraKeys.Contains(camera.Key));
                if (codecConfig != null && codecConfig.CodecKey == codec?.Key)
                {
                    this.LogDebug($"Camera Manager {Key} ignoring CameraDisconnected for camera '{camera.Key}' on codec '{codec?.Key}' — this is the target codec for scenario '{currentScenario.Key}'");
                    return;
                }
            }

            var targetCodecKey = GetTargetCodecKeyForCamera(camera.Key);
            if (string.IsNullOrEmpty(targetCodecKey))
            {
                this.LogError($"Camera Manager {Key} could not resolve a target codec for camera '{camera.Key}' in the current scenario");
                return;
            }

            // Phantom-disconnect guard: if this camera is currently present AND ONLINE on its target
            // codec, the disconnect from some other codec is a stale-cache artifact — do NOT start a
            // migration. A serial appearing in the target codec's camera list is NOT enough; the
            // camera must be confirmed online (codec-reported Connected=true), otherwise a stale or
            // ghosted list entry would suppress a migration that should actually run.
            if (managedCodecs.TryGetValue(targetCodecKey, out var targetCodecDevice))
            {
                var targetCodec = targetCodecDevice as CiscoCodec;
                var onTarget = targetCodec?.Cameras?.OfType<CiscoCamera>()
                    .Any(c => c.IsOnline
                              && !string.IsNullOrEmpty(c.SerialNumber)
                              && string.Equals(c.SerialNumber, camera.SerialNumber, StringComparison.OrdinalIgnoreCase));
                if (onTarget == true)
                {
                    this.LogDebug($"CAMERA_SWITCHOVER_PHANTOM_DISCONNECT camera='{camera.Key}' sourceCodec='{codec?.Key}' sourceCameraId='{e.CameraId}' targetCodec='{targetCodecKey}' reason='cameraOnlineOnTargetCodec'");
                    this.LogDebug($"Camera Manager {Key} ignoring phantom CameraDisconnected for camera '{camera.Key}' from codec '{codec?.Key}': camera is already online on its target codec '{targetCodecKey}'");
                    return;
                }
            }

            CameraMigrationState existingMigration;
            lock (activeMigrationsLock)
            {
                activeMigrations.TryGetValue(camera.Key, out existingMigration);
            }

            if (existingMigration != null)
            {
                var sameSource = string.Equals(existingMigration.SourceCodecKey, codec?.Key, StringComparison.OrdinalIgnoreCase);
                var sameTarget = string.Equals(existingMigration.TargetCodecKey, targetCodecKey, StringComparison.OrdinalIgnoreCase);
                var samePort = NormalizePort(existingMigration.Port) == NormalizePort(camera.NetworkSwitchPort);

                if (sameSource && sameTarget && samePort)
                {
                    this.LogDebug($"CAMERA_SWITCHOVER_DUPLICATE_DISCONNECT camera='{camera.Key}' sourceCodec='{existingMigration.SourceCodecKey}' sourceCameraId='{existingMigration.SourceCameraId}' targetCodec='{existingMigration.TargetCodecKey}' port='{existingMigration.Port}' PoEDisabled='{existingMigration.PoeDisabledConfirmed}' AssignedCleared='{existingMigration.AssignmentClearedConfirmed}' VlanIssued='{existingMigration.VlanSwitchIssued}'");

                    var duplicateSerial = e.SerialNumber?.Trim();
                    if (!existingMigration.AssignmentClearedConfirmed && string.IsNullOrEmpty(duplicateSerial))
                    {
                        existingMigration.AssignmentClearedConfirmed = true;
                        existingMigration.AssignmentClearDeadlineUtc = DateTime.MinValue;
                        this.LogDebug($"CAMERA_SWITCHOVER_ASSIGNED_CLEARED_FALLBACK camera='{camera.Key}' sourceCodec='{existingMigration.SourceCodecKey}' sourceCameraId='{existingMigration.SourceCameraId}' targetCodec='{existingMigration.TargetCodecKey}' port='{existingMigration.Port}' reason='duplicateDisconnectEmptySerial'");
                        this.LogDebug($"Camera Manager {Key} confirmed AssignedSerialNumber cleared for camera '{camera.Key}' by duplicate disconnect fallback with empty serial");
                        TryIssueVlanSwitch(existingMigration);
                    }

                    if (!existingMigration.PoeDisabledConfirmed && !existingMigration.VlanSwitchIssued)
                    {
                        this.LogDebug($"CAMERA_SWITCHOVER_POE_DISABLE_RETRY camera='{camera.Key}' sourceCodec='{existingMigration.SourceCodecKey}' targetCodec='{existingMigration.TargetCodecKey}' port='{existingMigration.Port}' reason='previousPoeDisableNotConfirmed'");
                        this.LogDebug($"Camera Manager {Key} retrying PoE disable for camera '{camera.Key}' on port '{existingMigration.Port}' — previous disable was never confirmed");
                        networkSwitch.SetPortPoeState(existingMigration.Port, false);
                    }

                    this.LogDebug($"Camera Manager {Key} ignoring duplicate CameraDisconnected for camera '{camera.Key}' while migration is already in progress");
                    return;
                }
            }

            // No active migration for this camera. With the online-gated trigger, a brand-new
            // migration is started ONLY from the connected-on-wrong-codec handler (after the codec
            // confirms the camera is genuinely online on the wrong codec). A bare CameraDisconnected
            // here is almost always the camera legitimately leaving the source codec because it has
            // already moved to the target codec on its own — starting a migration (PoE off / VLAN /
            // PoE on) would needlessly power-cycle a healthy camera. So we do NOT start a migration
            // from a disconnect; we only ever advance an existing one (handled above).
            this.LogDebug($"Camera Manager {Key} ignoring CameraDisconnected for camera '{camera.Key}' from codec '{codec?.Key}': no active migration — migrations are only started from a confirmed online connect on the wrong codec");
        }

        private void Codec_CameraConnected(object sender, CameraEventArgs e)
        {
            var codec = sender as CiscoCodec;

            var camera = ResolveManagedCamera(codec, e);
            if (camera == null)
            {
                this.LogDebug($"Camera Manager {Key} received CameraConnected event from codec {codec?.Key} for camera ID {e.CameraId} / serial {e.SerialNumber} but no managed camera was resolved");
                return;
            }

            // A connect from a blocker camera often means its migration progressed and target slot
            // ownership changed. Re-check any migrations blocked on this camera's prior slot.
            TryAdvanceMigrationsWaitingForTargetSlotClear(codec?.Key, "blockerCameraConnected", camera.Key);

            // Any managed camera coming online is a good moment to re-apply the current room
            // combination intent. This makes post-recovery behavior deterministic even when the
            // camera surfaced on a stale/wrong codec path outside the normal migration event flow.
            TryReconcileCurrentScenarioAfterCameraOnline(camera.Key, codec?.Key);

            CameraMigrationState activeMigration;
            lock (activeMigrationsLock)
            {
                activeMigrations.TryGetValue(camera.Key, out activeMigration);
            }

            if (activeMigration != null && string.Equals(codec?.Key, activeMigration.TargetCodecKey, StringComparison.OrdinalIgnoreCase))
            {
                int priorFailedAttempts;
                lock (activeMigrationsLock)
                {
                    attachFailureCounts.TryGetValue(camera.Key, out priorFailedAttempts);
                    attachFailureCounts.Remove(camera.Key);
                }
                ClearFloatingRecoveryState(camera.Key);

                this.LogDebug($"CAMERA_SWITCHOVER_ATTACH_CONFIRMED camera='{activeMigration.CameraKey}' sourceCodec='{activeMigration.SourceCodecKey}' sourceCameraId='{activeMigration.SourceCameraId}' targetCodec='{activeMigration.TargetCodecKey}' port='{activeMigration.Port}' failedAttempts='{priorFailedAttempts}'");
                lock (activeMigrationsLock)
                {
                    activeMigrations.Remove(camera.Key);
                }

                // Final guard: ensure the migration port is left with PoE enabled.
                this.LogDebug($"CAMERA_SWITCHOVER_POE_GUARD_ENSURE_ON camera='{activeMigration.CameraKey}' port='{activeMigration.Port}' targetCodec='{activeMigration.TargetCodecKey}'");
                networkSwitch.SetPortPoeState(activeMigration.Port, true);
            }

            // Check if this camera is on the correct codec per the current scenario
            var currentScenario = roomCombiner.CurrentScenario;
            if (currentScenario != null && config.RoomCombinerConfig.CombineScenarios.TryGetValue(currentScenario.Key, out var scenarioConfig))
            {
                var codecConfig = scenarioConfig.CodecConfigs.FirstOrDefault(cc => cc.CameraKeys.Contains(camera.Key));
                if (codecConfig != null && codecConfig.CodecKey != codec?.Key)
                {
                    var targetCodecKey = codecConfig.CodecKey;

                    // Debounce: if a migration is already running for this camera, ignore repeat
                    // connect events on the wrong codec. The camera bounces back onto the source
                    // codec several times while it re-pairs/reboots; without this guard each bounce
                    // would fire another factory reset and the migration would never settle.
                    lock (activeMigrationsLock)
                    {
                        if (activeMigrations.ContainsKey(camera.Key))
                        {
                            this.LogDebug($"Camera Manager {Key} ignoring connect for camera '{camera.Key}' on wrong codec '{codec?.Key}': migration to '{targetCodecKey}' already in progress");
                            return;
                        }
                    }

                    // Online gate: only start the migration once the codec confirms the camera is
                    // genuinely connected (IsOnline). This is the "could confirm it could work" check —
                    // we never blindly start the PoE/VLAN cascade on a camera the codec cannot see.
                    if (!camera.IsOnline)
                    {
                        this.LogDebug($"Camera Manager {Key} detected camera '{camera.Key}' on wrong codec '{codec?.Key}' but codec does not report it online yet — deferring migration until online is confirmed");
                        return;
                    }

                    // Camera is online on the wrong codec. Send the factory reset to start re-pairing,
                    // then start the CLI cascade after a short delay. We do NOT wait for the source
                    // codec to report the camera disconnected — that bounce loop is what prevented the
                    // migration from completing.
                    this.LogDebug($"Camera Manager {Key} detected camera '{camera.Key}' online on codec '{codec?.Key}' but should be on codec '{targetCodecKey}' per scenario '{currentScenario.Key}'. Confirmed online — starting migration.");

                    TryStartMigration(camera, codec, e.CameraId, targetCodecKey, currentScenario.Key);
                    return;
                }
            }

            // Camera is on the correct codec — proceed with normal setup.
            // Resolve the slot this camera should occupy on this codec for the current scenario: an
            // explicit per-scenario cameraId wins; otherwise fall back to the existing effective-id
            // logic. Pin/reset the camera object first so self-heal enforces the SAME slot (single
            // source of truth, loop-safe) and SourceId is mirrored when an explicit id is present.
            var explicitScenarioId = GetScenarioConfiguredCameraId(currentScenario?.Key, codec?.Key, camera.Key);
            camera.SetScenarioCameraId(explicitScenarioId);
            var targetSlot = explicitScenarioId ?? GetEffectiveCameraId(camera, codec);

            // Clear stale serial assignments on any slot that isn't the resolved target slot.
            // Two sources of stale slots:
            //   1. matchingCameras: the codec's local CiscoCamera collection (synced via Path B)
            //   2. e.CameraId from the event itself, when the codec attached the camera at a slot
            //      that the local collection hasn't yet absorbed (timing race that produces the
            //      "no cameras on codec X have a matching serial number" warning).
            // Combine both sources so we always clear the slot the codec just told us about,
            // even when Path B hasn't caught up yet.
            var slotsToClear = new HashSet<uint>();
            var codecCameras = codec?.Cameras;
            if (codecCameras != null)
            {
                var ciscoCodecCameras = codecCameras.OfType<CiscoCamera>().ToList();
                var matchingCameras = ciscoCodecCameras
                    .Where(c => !string.IsNullOrEmpty(c.SerialNumber)
                                && string.Equals(c.SerialNumber, e.SerialNumber, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (!matchingCameras.Any())
                {
                    this.LogDebug($"Camera Manager {Key} CameraConnected for serial {e.SerialNumber} on codec '{codec?.Key}': local camera collection has not absorbed the serial yet — using event slot {e.CameraId} as authoritative");
                }
                foreach (var matchingCamera in matchingCameras)
                {
                    if (matchingCamera.CameraId != targetSlot)
                    {
                        slotsToClear.Add(matchingCamera.CameraId);
                    }
                }
            }
            if (e.CameraId != targetSlot)
            {
                slotsToClear.Add(e.CameraId);
            }
            foreach (var staleSlot in slotsToClear)
            {
                this.LogDebug($"CAMERA_SWITCHOVER_TARGET_SLOT_CLEAR camera='{camera.Key}' codec='{codec?.Key}' staleSlot='{staleSlot}' configuredSlot='{targetSlot}' serial='{e.SerialNumber}'");
                codec.ClearCameraAssignedSerialNumber(staleSlot);
                this.LogDebug($"Camera Manager {Key} clearing stale serial assignment for camera '{camera.Key}' on codec '{codec?.Key}' slot {staleSlot} (configured slot is {targetSlot})");
            }

            var codecCameraReset = sender as ICiscoCodecCameraFactoryReset;
            if (codecCameraReset != null)
            {
                this.LogDebug($"CAMERA_SWITCHOVER_TARGET_SLOT_ASSIGN camera='{camera.Key}' codec='{codec?.Key}' effectiveSlot='{targetSlot}' configuredSlot='{camera.DefaultCameraId}' serial='{camera.SerialNumber}' attachedSlot='{e.CameraId}'");
                this.LogDebug($"Camera Manager {Key} assigning serial '{camera.SerialNumber}' to slot {targetSlot} on codec '{codec?.Key}' for camera '{camera.Key}'");
                codecCameraReset.SetCameraAssignedSerialNumber(targetSlot, camera.SerialNumber);
            }
            else
            {
                this.LogError($"Camera Manager {Key} error: sender of CameraConnected event is not a codec when handling camera connect for camera '{camera.Key}'");
            }
        }

        private void TryReconcileCurrentScenarioAfterCameraOnline(string cameraKey, string codecKey)
        {
            var scenarioKey = roomCombiner?.CurrentScenario?.Key;
            if (string.IsNullOrWhiteSpace(scenarioKey))
            {
                return;
            }

            this.LogDebug($"Camera Manager {Key} running immediate scenario reconciliation after camera '{cameraKey}' came online on codec '{codecKey}' (scenario='{scenarioKey}')");
            RunScenarioReconciliation(scenarioKey);
        }

        private void TryIssueVlanSwitch(CameraMigrationState migration)
        {
            if (migration == null || migration.VlanSwitchIssued)
            {
                return;
            }

            if (!migration.AssignmentClearedConfirmed)
            {
                TryConfirmAssignmentClearedFromSourceCodecState(migration);
            }

            if (!migration.PoeDisabledConfirmed || !migration.AssignmentClearedConfirmed)
            {
                this.LogDebug($"CAMERA_SWITCHOVER_WAITING camera='{migration?.CameraKey}' sourceCodec='{migration?.SourceCodecKey}' sourceCameraId='{migration?.SourceCameraId}' targetCodec='{migration?.TargetCodecKey}' port='{migration?.Port}' PoEDisabled='{migration?.PoeDisabledConfirmed}' AssignedCleared='{migration?.AssignmentClearedConfirmed}'");
                this.LogVerbose($"Camera Manager {Key} holding VLAN switch for camera '{migration?.CameraKey}': PoEDisabled={migration?.PoeDisabledConfirmed} AssignedCleared={migration?.AssignmentClearedConfirmed}");
                return;
            }

            if (!managedCodecs.TryGetValue(migration.TargetCodecKey, out var targetCodec))
            {
                this.LogError($"Camera Manager {Key} cannot change VLAN for camera '{migration.CameraKey}': target codec '{migration.TargetCodecKey}' not found");
                return;
            }

            this.LogDebug($"CAMERA_SWITCHOVER_READY camera='{migration.CameraKey}' sourceCodec='{migration.SourceCodecKey}' sourceCameraId='{migration.SourceCameraId}' targetCodec='{migration.TargetCodecKey}' port='{migration.Port}' prereqs='PoEDisabled+AssignedCleared'");
            migration.VlanSwitchIssued = true;
            migration.AttachWaitStarted = false;
            migration.AttachWaitDeadlineUtc = DateTime.MinValue;
            var targetVlanId = targetCodec.VLanId;
            this.LogDebug($"Camera Manager {Key} confirmed feedback prerequisites for camera '{migration.CameraKey}', changing VLAN on port '{migration.Port}' to {targetVlanId} for target codec '{migration.TargetCodecKey}'");
            networkSwitch.SetPortVlan(migration.Port, targetVlanId);
        }

        private void TryConfirmAssignmentClearedFromSourceCodecState(CameraMigrationState migration)
        {
            if (migration == null || migration.AssignmentClearedConfirmed)
            {
                return;
            }

            if (!managedCodecs.TryGetValue(migration.SourceCodecKey, out var sourceCodecDevice))
            {
                return;
            }

            var sourceCodec = sourceCodecDevice as CiscoCodec;
            var sourceCodecCamera = sourceCodec?.Cameras?.OfType<CiscoCamera>()
                .FirstOrDefault(c => c.CameraId == migration.SourceCameraId);

            if (sourceCodecCamera == null || string.IsNullOrWhiteSpace(sourceCodecCamera.SerialNumber))
            {
                migration.AssignmentClearedConfirmed = true;
                migration.AssignmentClearDeadlineUtc = DateTime.MinValue;
                this.LogDebug($"CAMERA_SWITCHOVER_ASSIGNED_CLEARED_FALLBACK camera='{migration.CameraKey}' sourceCodec='{migration.SourceCodecKey}' sourceCameraId='{migration.SourceCameraId}' targetCodec='{migration.TargetCodecKey}' port='{migration.Port}' reason='sourceCodecCameraMissingOrEmptySerial'");
                this.LogDebug($"Camera Manager {Key} confirmed AssignedSerialNumber cleared for camera '{migration.CameraKey}' by source codec camera state fallback");
            }
        }

        private CameraMigrationState GetMigrationByPort(string port)
        {
            var normalizedPort = NormalizePort(port);
            lock (activeMigrationsLock)
            {
                return activeMigrations.Values.FirstOrDefault(m => NormalizePort(m.Port) == normalizedPort);
            }
        }

        private static string NormalizePort(string port)
        {
            return string.IsNullOrWhiteSpace(port) ? string.Empty : port.Trim().ToLowerInvariant();
        }

        private CiscoCamera ResolveManagedCamera(CiscoCodec codec, CameraEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.SerialNumber))
            {
                var bySerial = managedCameras.Values.FirstOrDefault(c => c.SerialNumber == e.SerialNumber);
                if (bySerial != null)
                {
                    return bySerial;
                }
            }

            var byCodecAndId = managedCameras.Values.FirstOrDefault(c => c.ParentCodec?.Key == codec?.Key && c.CameraId == e.CameraId);
            if (byCodecAndId != null)
            {
                return byCodecAndId;
            }

            var byId = managedCameras.Values.Where(c => c.CameraId == e.CameraId).ToList();
            return byId.Count == 1 ? byId[0] : null;
        }

        private string GetTargetCodecKeyForCamera(string cameraKey)
        {
            var currentScenario = roomCombiner.CurrentScenario;
            if (currentScenario == null || !config.RoomCombinerConfig.CombineScenarios.TryGetValue(currentScenario.Key, out var scenarioConfig))
            {
                return null;
            }

            var codecConfig = scenarioConfig.CodecConfigs.FirstOrDefault(cc => cc.CameraKeys.Contains(cameraKey));
            return codecConfig?.CodecKey;
        }

        /// <summary>
        /// Returns the explicitly configured camera id (slot) for a camera on a given codec in a
        /// given scenario, or null when the camera was declared without an explicit id (string form)
        /// or is not present under that codec in that scenario. Null means "use the camera's
        /// defaultCameraId / existing effective-id logic" (today's behavior).
        /// </summary>
        private uint? GetScenarioConfiguredCameraId(string scenarioKey, string targetCodecKey, string cameraKey)
        {
            if (string.IsNullOrEmpty(scenarioKey) || string.IsNullOrEmpty(targetCodecKey) || string.IsNullOrEmpty(cameraKey))
            {
                return null;
            }

            if (config?.RoomCombinerConfig?.CombineScenarios == null
                || !config.RoomCombinerConfig.CombineScenarios.TryGetValue(scenarioKey, out var scenarioConfig)
                || scenarioConfig?.CodecConfigs == null)
            {
                return null;
            }

            var codecConfig = scenarioConfig.CodecConfigs.FirstOrDefault(cc => cc.CodecKey == targetCodecKey);
            return codecConfig?.GetConfiguredCameraId(cameraKey);
        }

        private string BuildManagedCameraSnapshot(string cameraKey)
        {
            if (!managedCameras.TryGetValue(cameraKey, out var camera))
            {
                return "missing";
            }

            var parentCodecKey = camera.ParentCodec?.Key ?? "null";
            var serial = string.IsNullOrWhiteSpace(camera.SerialNumber) ? "empty" : camera.SerialNumber;
            var port = string.IsNullOrWhiteSpace(camera.NetworkSwitchPort) ? "empty" : camera.NetworkSwitchPort;
            return $"parent={parentCodecKey};cameraId={camera.CameraId};serial={serial};port={port}";
        }

        private string BuildCodecCameraSnapshot(string codecKey)
        {
            if (string.IsNullOrWhiteSpace(codecKey))
            {
                return "codecKeyEmpty";
            }

            if (!managedCodecs.TryGetValue(codecKey, out var codecDevice))
            {
                return "codecMissing";
            }

            var codec = codecDevice as CiscoCodec;
            if (codec == null)
            {
                return "codecTypeMismatch";
            }

            var cameras = codec.Cameras?.OfType<CiscoCamera>().ToList();
            if (cameras == null || cameras.Count == 0)
            {
                return "count=0";
            }

            return string.Join(",", cameras.Select(c =>
            {
                var serial = string.IsNullOrWhiteSpace(c.SerialNumber) ? "empty" : c.SerialNumber;
                return $"{c.CameraId}:{serial}";
            }));
        }

        private uint GetEffectiveCameraId(CiscoCamera camera, CiscoCodec targetCodec)
        {
            if (camera == null || targetCodec == null)
            {
                return camera?.DefaultCameraId ?? 7;
            }

            // If camera requires maintaining its configured ID, use it directly
            if (camera.MaintainConfiguredCameraId)
            {
                return camera.DefaultCameraId;
            }

            lock (activeMigrationsLock)
            {
                var now = DateTime.UtcNow;

                // Confirmed assignments the codec has already reported back in its Cameras collection.
                var usedIds = new HashSet<uint>(
                    targetCodec.Cameras?.OfType<CiscoCamera>()
                        .Where(c => !string.IsNullOrEmpty(c.SerialNumber))
                        .Select(c => c.CameraId) ?? Enumerable.Empty<uint>());

                // This camera can only occupy one codec at a time, so drop any reservation it holds
                // anywhere (a re-decision on this codec, or a leftover from a codec it left). Also
                // expire stale reservations so migrations/missed feedback can never leak an id.
                foreach (var codecReservations in pendingCameraIdReservations.Values)
                {
                    codecReservations.Remove(camera.Key);
                    var expired = codecReservations
                        .Where(kv => (now - kv.Value.ReservedUtc) > CameraIdReservationTtl)
                        .Select(kv => kv.Key)
                        .ToList();
                    foreach (var staleKey in expired)
                    {
                        codecReservations.Remove(staleKey);
                    }
                }

                if (!pendingCameraIdReservations.TryGetValue(targetCodec.Key, out var reservations))
                {
                    reservations = new Dictionary<string, CameraIdReservation>();
                    pendingCameraIdReservations[targetCodec.Key] = reservations;
                }

                // Include still-outstanding reservations from OTHER cameras targeting this codec so
                // two in-flight allocations can never collide on the same id.
                foreach (var reservation in reservations.Values)
                {
                    usedIds.Add(reservation.CameraId);
                }

                // Try to find an available ID from the pool [7, 8, 9]
                foreach (var id in new[] { 7u, 8u, 9u })
                {
                    if (!usedIds.Contains(id))
                    {
                        reservations[camera.Key] = new CameraIdReservation { CameraId = id, ReservedUtc = now };
                        this.LogDebug($"Camera Manager {Key} allocating dynamic camera ID {id} to camera '{camera.Key}' on codec '{targetCodec.Key}' (pool=[7,8,9], in-use={string.Join(",", usedIds.OrderBy(x => x))})");
                        return id;
                    }
                }

                // All IDs in pool are in use; fall back to default (and log a warning)
                this.LogWarning($"Camera Manager {Key} could not find available ID in pool [7,8,9] for camera '{camera.Key}' on codec '{targetCodec.Key}' (all in use: {string.Join(",", usedIds.OrderBy(x => x))}), falling back to default {camera.DefaultCameraId}");
                return camera.DefaultCameraId;
            }
        }

        private class CameraIdReservation
        {
            public uint CameraId { get; set; }
            public DateTime ReservedUtc { get; set; }
        }

        private class CameraMigrationState
        {
            public string CameraKey { get; set; }
            public string Port { get; set; }
            public string SourceCodecKey { get; set; }
            public uint SourceCameraId { get; set; }
            public string TargetCodecKey { get; set; }
            public bool PoeDisabledConfirmed { get; set; }
            public bool AssignmentClearedConfirmed { get; set; }
            public DateTime AssignmentClearDeadlineUtc { get; set; }
            public bool VlanSwitchIssued { get; set; }
            public bool VlanChangedConfirmed { get; set; }
            public bool PoeEnableIssued { get; set; }
            public DateTime PoeOffDeadlineUtc { get; set; }
            public bool PoeOffSafeguardTriggered { get; set; }
            public bool AttachWaitStarted { get; set; }
            public bool TargetSerialAssigned { get; set; }
            public bool WaitingForTargetSlotClear { get; set; }
            public uint ExpectedTargetSlot { get; set; }
            public string BlockedByCameraKey { get; set; }
            public DateTime AttachWaitDeadlineUtc { get; set; }
            public DateTime PoeReenableDeadlineUtc { get; set; }
            public bool WaitingForSourceDisconnect { get; set; }
            public DateTime DisconnectWaitDeadlineUtc { get; set; }
        }
    }
}
