

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

        private readonly Timer attachVerificationTimer;

        private const int AttachWaitTimeoutMs = 45000;
        private const int MaxPoeOffDurationMs = 60000;
        private const int MaxAttachRecoveryAttempts = 1;

        public CameraManager(string key, string name, CameraManagerPropertiesConfig config)
            : base(key, name)
        {
            this.config = config;
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
                this.LogInformation($"Camera Manager {Key} startup reconciliation for current scenario '{startupScenario}'");
                TryExecuteScenarioCameraResets(startupScenario);
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
                        shouldEnablePoe = true;
                        cameraKey = migration.CameraKey;
                    }
                }

                if (shouldEnablePoe)
                {
                    networkSwitch.SetPortPoeState(e.Port, true);
                    this.LogDebug($"Camera Manager {Key} confirmed VLAN changed for camera '{cameraKey}', re-enabling PoE on port '{e.Port}'");
                }
            }
            else if (e.EventType == NetworkSwitchPortEventType.PoEEnabled)
            {
                string cameraKey = null;
                string sourceCodecKey = null;
                string sourceCameraId = null;
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

                    migration.AttachWaitStarted = true;
                    migration.AttachWaitDeadlineUtc = DateTime.UtcNow.AddMilliseconds(AttachWaitTimeoutMs);
                    cameraKey = migration.CameraKey;
                    sourceCodecKey = migration.SourceCodecKey;
                    sourceCameraId = migration.SourceCameraId;
                    targetCodecKey = migration.TargetCodecKey;
                    port = migration.Port;
                    vlanChangedConfirmed = migration.VlanChangedConfirmed;
                }

                this.LogInformation($"CAMERA_SWITCHOVER_ATTACH_WAITING camera='{cameraKey}' sourceCodec='{sourceCodecKey}' sourceCameraId='{sourceCameraId}' targetCodec='{targetCodecKey}' port='{port}' vlanChanged='{vlanChangedConfirmed}' poeEnabled='True'");
                this.LogDebug($"Camera Manager {Key} confirmed migration sequence complete for camera '{cameraKey}' on port '{port}'");
            }
        }

        private void AttachVerificationTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            List<CameraMigrationState> pendingAttach;
            List<CameraMigrationState> pendingPoeSafeguard;
            lock (activeMigrationsLock)
            {
                pendingAttach = activeMigrations.Values
                    .Where(m => m.AttachWaitStarted && m.AttachWaitDeadlineUtc <= DateTime.UtcNow)
                    .ToList();

                pendingPoeSafeguard = activeMigrations.Values
                    .Where(m => m.PoeDisabledConfirmed
                        && !m.PoeEnableIssued
                        && !m.PoeOffSafeguardTriggered
                        && m.PoeOffDeadlineUtc != DateTime.MinValue
                        && m.PoeOffDeadlineUtc <= DateTime.UtcNow)
                    .ToList();
            }

            foreach (var migration in pendingPoeSafeguard)
            {
                migration.PoeOffSafeguardTriggered = true;
                this.LogInformation($"CAMERA_SWITCHOVER_POE_SAFEGUARD_TRIGGERED camera='{migration.CameraKey}' sourceCodec='{migration.SourceCodecKey}' sourceCameraId='{migration.SourceCameraId}' targetCodec='{migration.TargetCodecKey}' port='{migration.Port}' action='forcePoeOnAfterOffTimeout' maxOffMs='{MaxPoeOffDurationMs}'");
                this.LogDebug($"Camera Manager {Key} forcing PoE on for camera '{migration.CameraKey}' after extended PoE-off interval");
                networkSwitch.SetPortPoeState(migration.Port, true);
            }

            foreach (var migration in pendingAttach)
            {
                if (migration.AttachRecoveryAttempts >= MaxAttachRecoveryAttempts)
                {
                    this.LogInformation($"CAMERA_SWITCHOVER_ATTACH_FAILED camera='{migration.CameraKey}' sourceCodec='{migration.SourceCodecKey}' sourceCameraId='{migration.SourceCameraId}' targetCodec='{migration.TargetCodecKey}' port='{migration.Port}' attempts='{migration.AttachRecoveryAttempts}' action='reseedSourceVlanAndPoe'");
                    this.LogInformation($"CAMERA_SWITCHOVER_ATTACH_AUTOMAGIC_RECOVERY_TRIGGERED camera='{migration.CameraKey}' sourceCodec='{migration.SourceCodecKey}' sourceCameraId='{migration.SourceCameraId}' targetCodec='{migration.TargetCodecKey}' port='{migration.Port}' phase='failed' action='reseedSourceVlanAndPoe' attempts='{migration.AttachRecoveryAttempts}'");
                    this.LogDebug($"Camera Manager {Key} attach failed diagnostics camera='{migration.CameraKey}' managed='{BuildManagedCameraSnapshot(migration.CameraKey)}' sourceSnapshot='{BuildCodecCameraSnapshot(migration.SourceCodecKey)}' targetSnapshot='{BuildCodecCameraSnapshot(migration.TargetCodecKey)}'");
                    lock (activeMigrationsLock)
                    {
                        activeMigrations.Remove(migration.CameraKey);
                    }

                    if (managedCodecs.TryGetValue(migration.SourceCodecKey, out var sourceCodecDevice))
                    {
                        this.LogDebug($"Camera Manager {Key} re-seeding source VLAN/PoE for camera '{migration.CameraKey}' after attach failure to force rediscovery");
                        networkSwitch.SetPortVlan(migration.Port, sourceCodecDevice.VLanId);
                        networkSwitch.SetPortPoeState(migration.Port, true);
                    }
                    else
                    {
                        this.LogError($"Camera Manager {Key} cannot run attach failure reseed for camera '{migration.CameraKey}': source codec '{migration.SourceCodecKey}' not found");
                    }
                    continue;
                }

                migration.AttachRecoveryAttempts++;
                migration.AttachWaitStarted = true;
                migration.AttachWaitDeadlineUtc = DateTime.UtcNow.AddMilliseconds(AttachWaitTimeoutMs);

                this.LogInformation($"CAMERA_SWITCHOVER_ATTACH_TIMEOUT camera='{migration.CameraKey}' sourceCodec='{migration.SourceCodecKey}' sourceCameraId='{migration.SourceCameraId}' targetCodec='{migration.TargetCodecKey}' port='{migration.Port}' attempts='{migration.AttachRecoveryAttempts}' action='reassertTargetVlanAndPoe'");
                this.LogInformation($"CAMERA_SWITCHOVER_ATTACH_AUTOMAGIC_RECOVERY_TRIGGERED camera='{migration.CameraKey}' sourceCodec='{migration.SourceCodecKey}' sourceCameraId='{migration.SourceCameraId}' targetCodec='{migration.TargetCodecKey}' port='{migration.Port}' phase='timeout' action='reassertTargetVlanAndPoe' attempts='{migration.AttachRecoveryAttempts}'");
                this.LogDebug($"Camera Manager {Key} attach timeout diagnostics camera='{migration.CameraKey}' managed='{BuildManagedCameraSnapshot(migration.CameraKey)}' sourceSnapshot='{BuildCodecCameraSnapshot(migration.SourceCodecKey)}' targetSnapshot='{BuildCodecCameraSnapshot(migration.TargetCodecKey)}'");
                this.LogDebug($"Camera Manager {Key} reasserting target VLAN/PoE for camera '{migration.CameraKey}' after attach timeout");

                if (!managedCodecs.TryGetValue(migration.TargetCodecKey, out var targetCodecDevice))
                {
                    this.LogError($"Camera Manager {Key} cannot run attach timeout recovery for camera '{migration.CameraKey}': target codec '{migration.TargetCodecKey}' not found");
                    continue;
                }

                networkSwitch.SetPortVlan(migration.Port, targetCodecDevice.VLanId);
                networkSwitch.SetPortPoeState(migration.Port, true);
            }
        }

        private void RoomCombiner_RoomCombinationScenarioChanged(object sender, EventArgs e)
        {
            var currentScenario = roomCombiner.CurrentScenario;

            this.LogInformation($"Camera Manager {Key} detected room combination scenario change to '{currentScenario?.Key}'");

            TryExecuteScenarioCameraResets(currentScenario?.Key);
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

                        // Here we would implement the logic to assign the camera to the codec, e.g. by calling a method on the codec interface
                        this.LogDebug($"Camera Manager {Key} would assign camera '{cameraKey}' to codec '{codecConfig.CodecKey}' based on scenario '{scenarioKey}'");
                        this.LogDebug($"Camera Manager {Key} sending factory reset command for camera '{cameraKey}' on codec '{currentParentCodecKey}' to trigger re-pairing with correct codec based on new scenario");
                        camera.ParentCodec.CameraFactoryReset(camera.CameraId);
                    }
                }
            }
            else
            {
                this.LogInformation($"Camera Manager {Key} has no configuration for room combination scenario '{scenarioKey}'");
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
                    this.LogInformation($"CAMERA_SWITCHOVER_DUPLICATE_DISCONNECT camera='{camera.Key}' sourceCodec='{existingMigration.SourceCodecKey}' sourceCameraId='{existingMigration.SourceCameraId}' targetCodec='{existingMigration.TargetCodecKey}' port='{existingMigration.Port}' PoEDisabled='{existingMigration.PoeDisabledConfirmed}' AssignedCleared='{existingMigration.AssignmentClearedConfirmed}' VlanIssued='{existingMigration.VlanSwitchIssued}'");

                    var duplicateSerial = e.SerialNumber?.Trim();
                    if (!existingMigration.AssignmentClearedConfirmed && string.IsNullOrEmpty(duplicateSerial))
                    {
                        existingMigration.AssignmentClearedConfirmed = true;
                        this.LogInformation($"CAMERA_SWITCHOVER_ASSIGNED_CLEARED_FALLBACK camera='{camera.Key}' sourceCodec='{existingMigration.SourceCodecKey}' sourceCameraId='{existingMigration.SourceCameraId}' targetCodec='{existingMigration.TargetCodecKey}' port='{existingMigration.Port}' reason='duplicateDisconnectEmptySerial'");
                        this.LogDebug($"Camera Manager {Key} confirmed AssignedSerialNumber cleared for camera '{camera.Key}' by duplicate disconnect fallback with empty serial");
                        TryIssueVlanSwitch(existingMigration);
                    }

                    this.LogDebug($"Camera Manager {Key} ignoring duplicate CameraDisconnected for camera '{camera.Key}' while migration is already in progress");
                    return;
                }
            }

            var migration = new CameraMigrationState
            {
                CameraKey = camera.Key,
                Port = camera.NetworkSwitchPort,
                SourceCodecKey = codec?.Key,
                SourceCameraId = e.CameraId,
                TargetCodecKey = targetCodecKey
            };

            var initialDisconnectSerial = e.SerialNumber?.Trim();
            if (string.IsNullOrEmpty(initialDisconnectSerial))
            {
                migration.AssignmentClearedConfirmed = true;
                this.LogInformation($"CAMERA_SWITCHOVER_ASSIGNED_CLEARED_FALLBACK camera='{camera.Key}' sourceCodec='{migration.SourceCodecKey}' sourceCameraId='{migration.SourceCameraId}' targetCodec='{migration.TargetCodecKey}' port='{migration.Port}' reason='initialDisconnectEmptySerial'");
                this.LogDebug($"Camera Manager {Key} confirmed AssignedSerialNumber cleared for camera '{camera.Key}' from initial disconnect with empty serial");
            }

            lock (activeMigrationsLock)
            {
                activeMigrations[camera.Key] = migration;
            }

            // Feedback-driven sequence: issue both actions, then wait for both confirmations
            // (PoEDisabled + AssignedSerialNumber cleared) before switching VLAN.
            this.LogDebug($"Camera Manager {Key} handling CameraDisconnected event for camera '{camera.Key}' (Serial Number {e.SerialNumber})");
            this.LogDebug($"Camera Manager {Key} turning off PoE for camera '{camera.Key}' on network switch port '{camera.NetworkSwitchPort}'");
            networkSwitch.SetPortPoeState(camera.NetworkSwitchPort, false);

            this.LogDebug($"Camera Manager {Key} clearing assigned serial number for camera '{camera.Key}' on codec");
            codec?.ClearCameraAssignedSerialNumber(e.CameraId);
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
                this.LogInformation($"CAMERA_SWITCHOVER_ATTACH_CONFIRMED camera='{activeMigration.CameraKey}' sourceCodec='{activeMigration.SourceCodecKey}' sourceCameraId='{activeMigration.SourceCameraId}' targetCodec='{activeMigration.TargetCodecKey}' port='{activeMigration.Port}' attempts='{activeMigration.AttachRecoveryAttempts}'");
                lock (activeMigrationsLock)
                {
                    activeMigrations.Remove(camera.Key);
                }

                // Final guard: ensure the migration port is left with PoE enabled.
                this.LogInformation($"CAMERA_SWITCHOVER_POE_GUARD_ENSURE_ON camera='{activeMigration.CameraKey}' port='{activeMigration.Port}' targetCodec='{activeMigration.TargetCodecKey}'");
                networkSwitch.SetPortPoeState(activeMigration.Port, true);
            }

            // Check if this camera is on the correct codec per the current scenario
            var currentScenario = roomCombiner.CurrentScenario;
            if (currentScenario != null && config.RoomCombinerConfig.CombineScenarios.TryGetValue(currentScenario.Key, out var scenarioConfig))
            {
                var codecConfig = scenarioConfig.CodecConfigs.FirstOrDefault(cc => cc.CameraKeys.Contains(camera.Key));
                if (codecConfig != null && codecConfig.CodecKey != codec?.Key)
                {
                    // Camera is on the wrong codec — factory reset it to trigger the PoE/VLAN cascade
                    this.LogDebug($"Camera Manager {Key} detected camera '{camera.Key}' connected on codec '{codec?.Key}' but should be on codec '{codecConfig.CodecKey}' per scenario '{currentScenario.Key}'. Sending factory reset.");
                    codec?.CameraFactoryReset(e.CameraId);
                    return;
                }
            }

            // Camera is on the correct codec — proceed with normal setup
            var codecCameras = codec?.Cameras;
            if (codecCameras != null)
            {
                var ciscoCodecCameras = codecCameras.OfType<CiscoCamera>().ToList();
                var matchingCameras = ciscoCodecCameras.Where(c => c.SerialNumber == e.SerialNumber);
                if (matchingCameras.Any())
                {
                    foreach (var matchingCamera in matchingCameras)
                    {
                        // check if the camera ID of each matching camera matches the camera.SerialNumber.
                        // If not, clear the assigned serial number on the codec for the camera ID of matchingCamera
                        if (matchingCamera.CameraId != camera.CameraId)
                        {
                            codec.ClearCameraAssignedSerialNumber(matchingCamera.CameraId);
                            this.LogDebug($"Camera Manager {Key} found matching camera '{camera.Key}' for CameraConnected event with serial number {e.SerialNumber}, clearing assigned serial number on codec to ensure correct pairing");
                        }
                    }
                }
                else
                {
                    this.LogWarning($"Camera Manager {Key} received CameraConnected event for camera serial number {e.SerialNumber} but no cameras on codec '{codec.Key}' have a matching serial number");
                }
            }

            var codecCameraReset = sender as ICiscoCodecCameraFactoryReset;
            if (codecCameraReset != null)
            {
                this.LogDebug($"Camera Manager {Key} setting assigned serial number for camera '{camera.Key}' on codec '{codec?.Key}' to ensure correct pairing");
                codecCameraReset.SetCameraAssignedSerialNumber(camera.CameraId, camera.SerialNumber);
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
                this.LogInformation($"CAMERA_SWITCHOVER_WAITING camera='{migration?.CameraKey}' sourceCodec='{migration?.SourceCodecKey}' sourceCameraId='{migration?.SourceCameraId}' targetCodec='{migration?.TargetCodecKey}' port='{migration?.Port}' PoEDisabled='{migration?.PoeDisabledConfirmed}' AssignedCleared='{migration?.AssignmentClearedConfirmed}'");
                this.LogVerbose($"Camera Manager {Key} holding VLAN switch for camera '{migration?.CameraKey}': PoEDisabled={migration?.PoeDisabledConfirmed} AssignedCleared={migration?.AssignmentClearedConfirmed}");
                return;
            }

            if (!managedCodecs.TryGetValue(migration.TargetCodecKey, out var targetCodec))
            {
                this.LogError($"Camera Manager {Key} cannot change VLAN for camera '{migration.CameraKey}': target codec '{migration.TargetCodecKey}' not found");
                return;
            }

            this.LogInformation($"CAMERA_SWITCHOVER_READY camera='{migration.CameraKey}' sourceCodec='{migration.SourceCodecKey}' sourceCameraId='{migration.SourceCameraId}' targetCodec='{migration.TargetCodecKey}' port='{migration.Port}' prereqs='PoEDisabled+AssignedCleared'");
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
                this.LogInformation($"CAMERA_SWITCHOVER_ASSIGNED_CLEARED_FALLBACK camera='{migration.CameraKey}' sourceCodec='{migration.SourceCodecKey}' sourceCameraId='{migration.SourceCameraId}' targetCodec='{migration.TargetCodecKey}' port='{migration.Port}' reason='sourceCodecCameraMissingOrEmptySerial'");
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
            public int AttachRecoveryAttempts { get; set; }
        }
    }
}
