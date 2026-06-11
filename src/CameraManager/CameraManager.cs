

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

        private readonly Timer attachVerificationTimer;
        private int attachVerificationTimerHandlerActive;
        // Only touched from inside AttachVerificationTimer_Elapsed (single-threaded via the
        // re-entrancy guard), so it needs no lock.
        private DateTime nextReconcileSweepUtc = DateTime.MinValue;

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

        // Safety-net reconciliation cadence and per-camera backoff. The periodic sweep catches
        // cameras that ended up floating after a recovery dead-end (e.g. the source codec never
        // re-reported the camera online after a reseed, so no fresh migration cascade ever
        // restarted). The backoff keeps the watchdog from disrupting an in-flight recovery.
        private const int ReconcileSweepIntervalMs = 30000;
        private const int ReconcileBackoffMs = AttachWaitTimeoutMs;

        public CameraManager(string key, string name, CameraManagerPropertiesConfig config)
            : base(key, name)
        {
            this.config = config;
            factoryResetSettleMs = config != null && config.FactoryResetSettleMs > 0
                ? config.FactoryResetSettleMs
                : DefaultFactoryResetSettleMs;
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
                TryExecuteScenarioCameraResets(startupScenario);
                TryEnsureScenarioCameraPortStates(startupScenario);
            }
            else
            {
                this.LogWarning($"Camera Manager {Key} could not run startup reconciliation because current room scenario is empty");
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
                string cameraKey = null;

                lock (activeMigrationsLock)
                {
                    var migration = GetMigrationByPort(e.Port);
                    if (migration == null)
                    {
                        this.LogDebug($"Camera Manager {Key} detected VLAN changed event on port '{e.Port}' with no active migration");
                        return;
                    }

                    migration.VlanChangedConfirmed = true;
                    if (!migration.PoeEnableIssued)
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

                TryAssignSerialToTargetCodec(cameraKey, targetCodecKey, port, "attachWaitStart");
            }
        }

        /// <summary>
        /// Pins the managed camera's serial number to its configured slot on the target codec so the
        /// target codec claims the camera as soon as it boots on the target VLAN. Without this, the
        /// camera reaches the target codec's network but the codec never reports it as connected.
        /// </summary>
        private void TryAssignSerialToTargetCodec(string cameraKey, string targetCodecKey, string port, string reason)
        {
            if (string.IsNullOrEmpty(cameraKey) || string.IsNullOrEmpty(targetCodecKey))
            {
                return;
            }

            if (!managedCameras.TryGetValue(cameraKey, out var camera))
            {
                this.LogDebug($"Camera Manager {Key} cannot assign serial to target codec '{targetCodecKey}' for camera '{cameraKey}': camera not found");
                return;
            }

            if (string.IsNullOrWhiteSpace(camera.SerialNumber))
            {
                this.LogDebug($"Camera Manager {Key} cannot assign serial to target codec '{targetCodecKey}' for camera '{cameraKey}': camera has no serial number");
                return;
            }

            if (!managedCodecs.TryGetValue(targetCodecKey, out var targetCodec))
            {
                this.LogError($"Camera Manager {Key} cannot assign serial to target codec for camera '{cameraKey}': target codec '{targetCodecKey}' not found");
                return;
            }

            this.LogDebug($"CAMERA_SWITCHOVER_TARGET_SERIAL_ASSIGN camera='{cameraKey}' targetCodec='{targetCodecKey}' slot='{camera.DefaultCameraId}' serial='{camera.SerialNumber}' port='{port}' reason='{reason}'");
            targetCodec.SetCameraAssignedSerialNumber(camera.DefaultCameraId, camera.SerialNumber);
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
            this.LogDebug($"Camera Manager {Key} sending factory reset for camera '{camera.Key}' on codec '{sourceCodec.Key}', then starting PoE/VLAN cascade after {factoryResetSettleMs}ms");
            sourceCodec.CameraFactoryReset(sourceCameraId);

            ScheduleDelayed(factoryResetSettleMs, () => BeginMigrationPoeOffAndClearSerial(migration, sourceCodec));
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

            this.LogDebug($"CAMERA_SWITCHOVER_MIGRATION_STARTED camera='{migration.CameraKey}' sourceCodec='{migration.SourceCodecKey}' sourceCameraId='{migration.SourceCameraId}' targetCodec='{migration.TargetCodecKey}' port='{migration.Port}' actions='PoeOff+ClearAssignedSerial'");
            this.LogDebug($"Camera Manager {Key} turning off PoE for camera '{migration.CameraKey}' on network switch port '{migration.Port}'");
            networkSwitch.SetPortPoeState(migration.Port, false);

            this.LogDebug($"Camera Manager {Key} clearing assigned serial number for camera '{migration.CameraKey}' on source codec '{migration.SourceCodecKey}'");
            sourceCodec?.ClearCameraAssignedSerialNumber(migration.SourceCameraId);
        }

        private void AttachVerificationTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (System.Threading.Interlocked.Exchange(ref attachVerificationTimerHandlerActive, 1) == 1)
            {
                return;
            }

            try
            {
                List<string> pendingAttachKeys;
                List<string> pendingPoeSafeguardKeys;
                List<string> pendingPoeReenableRetryKeys;
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
                        this.LogDebug($"Camera Manager {Key} re-seeding source VLAN/PoE for camera '{cameraKey}' after attach failure to force rediscovery");
                        networkSwitch.SetPortVlan(port, sourceCodecDevice.VLanId);
                        networkSwitch.SetPortPoeState(port, true);
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
                        lock (activeMigrationsLock)
                        {
                            reconcileNextActionUtc.Remove(cameraKey);
                        }
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
                            lock (activeMigrationsLock)
                            {
                                reconcileNextActionUtc[cameraKey] = DateTime.UtcNow.AddMilliseconds(ReconcileBackoffMs);
                            }
                            TryStartMigration(camera, sourceCiscoCodec, camera.CameraId, targetCodecKey, currentScenario.Key);
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

                    lock (activeMigrationsLock)
                    {
                        reconcileNextActionUtc[cameraKey] = DateTime.UtcNow.AddMilliseconds(ReconcileBackoffMs);
                    }

                    this.LogDebug($"CAMERA_SWITCHOVER_RECONCILE_FLOATING camera='{cameraKey}' targetCodec='{targetCodecKey}' port='{port}' scenario='{currentScenario.Key}' action='bouncePoe' managed='{BuildManagedCameraSnapshot(cameraKey)}'");
                    networkSwitch.SetPortPoeState(port, false);
                    ScheduleDelayed(MigrationPoeOffDelayMs, () =>
                    {
                        this.LogDebug($"Camera Manager {Key} reconcile re-enabling PoE for floating camera '{cameraKey}' on port '{port}'");
                        networkSwitch.SetPortPoeState(port, true);
                    });
                }
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

        private void RoomCombiner_RoomCombinationScenarioChanged(object sender, EventArgs e)
        {
            var currentScenario = roomCombiner.CurrentScenario;

            this.LogDebug($"Camera Manager {Key} detected room combination scenario change to '{currentScenario?.Key}'");

            TryExecuteScenarioCameraResets(currentScenario?.Key);
            TryEnsureScenarioCameraPortStates(currentScenario?.Key);
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

                    var port = camera.NetworkSwitchPort;
                    var vlanId = codec.VLanId;
                    this.LogDebug($"CAMERA_PORT_ENSURE camera='{cameraKey}' targetCodec='{codecConfig.CodecKey}' port='{port}' vlan='{vlanId}' scenario='{scenarioKey}'");
                    networkSwitch.SetPortVlan(port, vlanId);
                    networkSwitch.SetPortPoeState(port, true);
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

                        // Camera is confirmed online on the wrong (source) codec. Start the full
                        // migration cascade: factory reset → (500ms) → PoE off → clear serial → VLAN →
                        // PoE on → attach wait + auto-recovery + target-serial-assign.
                        this.LogDebug($"Camera Manager {Key} confirmed camera '{cameraKey}' online on source codec '{currentParentCodecKey}' — starting migration to '{codecConfig.CodecKey}' for scenario '{scenarioKey}'");
                        TryStartMigration(camera, sourceCiscoCodec, camera.CameraId, codecConfig.CodecKey, scenarioKey);
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
                    reconcileNextActionUtc.Remove(camera.Key);
                }

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
            // Clear stale serial assignments on any slot that isn't the configured DefaultCameraId.
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
                    if (matchingCamera.CameraId != camera.DefaultCameraId)
                    {
                        slotsToClear.Add(matchingCamera.CameraId);
                    }
                }
            }
            if (e.CameraId != camera.DefaultCameraId)
            {
                slotsToClear.Add(e.CameraId);
            }
            foreach (var staleSlot in slotsToClear)
            {
                this.LogDebug($"CAMERA_SWITCHOVER_TARGET_SLOT_CLEAR camera='{camera.Key}' codec='{codec?.Key}' staleSlot='{staleSlot}' configuredSlot='{camera.DefaultCameraId}' serial='{e.SerialNumber}'");
                codec.ClearCameraAssignedSerialNumber(staleSlot);
                this.LogDebug($"Camera Manager {Key} clearing stale serial assignment for camera '{camera.Key}' on codec '{codec?.Key}' slot {staleSlot} (configured slot is {camera.DefaultCameraId})");
            }

            var codecCameraReset = sender as ICiscoCodecCameraFactoryReset;
            if (codecCameraReset != null)
            {
                this.LogDebug($"CAMERA_SWITCHOVER_TARGET_SLOT_ASSIGN camera='{camera.Key}' codec='{codec?.Key}' configuredSlot='{camera.DefaultCameraId}' serial='{camera.SerialNumber}' attachedSlot='{e.CameraId}'");
                this.LogDebug($"Camera Manager {Key} assigning serial '{camera.SerialNumber}' to configured slot {camera.DefaultCameraId} on codec '{codec?.Key}' for camera '{camera.Key}'");
                codecCameraReset.SetCameraAssignedSerialNumber(camera.DefaultCameraId, camera.SerialNumber);
            }
            else
            {
                this.LogError($"Camera Manager {Key} error: sender of CameraConnected event is not a codec when handling camera connect for camera '{camera.Key}'");
            }
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

        private class CameraMigrationState
        {
            public string CameraKey { get; set; }
            public string Port { get; set; }
            public string SourceCodecKey { get; set; }
            public uint SourceCameraId { get; set; }
            public string TargetCodecKey { get; set; }
            public bool PoeDisabledConfirmed { get; set; }
            public bool AssignmentClearedConfirmed { get; set; }
            public bool VlanSwitchIssued { get; set; }
            public bool VlanChangedConfirmed { get; set; }
            public bool PoeEnableIssued { get; set; }
            public DateTime PoeOffDeadlineUtc { get; set; }
            public bool PoeOffSafeguardTriggered { get; set; }
            public bool AttachWaitStarted { get; set; }
            public DateTime AttachWaitDeadlineUtc { get; set; }
            public DateTime PoeReenableDeadlineUtc { get; set; }
        }
    }
}
