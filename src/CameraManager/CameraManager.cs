

using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Org.BouncyCastle.Asn1.X509;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Core.Web.RequestHandlers;
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

        public CameraManager(string key, string name, CameraManagerPropertiesConfig config)
            : base(key, name)
        {
            this.config = config;

        }

        /// <summary>
        /// Custom activation to link the Camera Manager to the room combiner, network switch, codecs, and cameras based on the keys provided in the configuration.
        /// </summary>
        /// <returns></returns>
        public override bool CustomActivate()
        {
            var roomCombinerDevice = DeviceManager.GetDeviceForKey(config.RoomCombinerConfig.RoomCombinerKey) as EssentialsRoomCombiner;
            if (roomCombinerDevice == null)
            {
                this.LogError($"Camera Manager {Key} failed to activate: Room Combiner device with key {config.RoomCombinerConfig.RoomCombinerKey} not found or not an EssentialsRoomCombiner");
                return false;
            }

            roomCombiner = roomCombinerDevice;

            roomCombiner.RoomCombinationScenarioChanged += RoomCombiner_RoomCombinationScenarioChanged;

            var networkSwitchDevice = DeviceManager.GetDeviceForKey(config.NetworkSwitchKey) as INetworkSwitchPoeVlanManager;
            if (networkSwitchDevice == null)
            {
                this.LogError($"Camera Manager {Key} failed to activate: Network Switch device with key {config.NetworkSwitchKey} not found or does not implement INetworkSwitchPoeVlanManager");
                return false;
            }

            networkSwitch = networkSwitchDevice;

            networkSwitch.PortStateChanged += NetworkSwitch_PortStateChanged;

            HashSet<string> codecKeysInScenarios = new HashSet<string>();
            HashSet<string> cameraKeysInScenarios = new HashSet<string>();
            foreach (var scenario in config.RoomCombinerConfig.CombineScenarios)
            {
                foreach (var config in scenario.Value.CodecConfigs)
                {
                    codecKeysInScenarios.Add(config.CodecKey);
                    foreach (var cameraKey in config.CameraKeys)
                    {
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
                managedCameras.Add(cameraKey, cameraDevice);
            }

            foreach (var kvp in managedCodecs)
            {
                var codec = kvp.Value;
                codec.CameraConnected += Codec_CameraConnected;
                codec.CameraDisconnected += Codec_CameraDisconnected;
            }

            return base.CustomActivate();
        }

        private void NetworkSwitch_PortStateChanged(object sender, NetworkSwitchPortEventArgs e)
        {
            this.LogDebug($"Camera Manager {Key} detected network switch port state change on port '{e.Port}' to state '{e.EventType}'");

            // If a port reports it's POE state as disabled, we want to find the camera with that port in our managed cameras and switch the VLAN ID of that port to the VLAN ID of the codec that camera is assigned to in the current scenario. This will help ensure that when the camera reconnects it comes back on the correct codec and not as a ghost on the old one
            if (e.EventType == NetworkSwitchPortEventType.PoEDisabled)
            {
                var camera = managedCameras.Values.FirstOrDefault(c => c.NetworkSwitchPort == e.Port);
                if (camera != null)
                {
                    var currentScenario = roomCombiner.CurrentScenario;
                    if (config.RoomCombinerConfig.CombineScenarios.TryGetValue(currentScenario.Key, out var scenarioConfig))
                    {
                        var codecConfig = scenarioConfig.CodecConfigs.FirstOrDefault(cc => cc.CameraKeys.Contains(camera.Key));
                        if (codecConfig != null)
                        {
                            var targetCodecKey = codecConfig.CodecKey;
                            if (managedCodecs.TryGetValue(targetCodecKey, out var targetCodec))
                            {
                                var targetVlanId = targetCodec.VLanId;
                                this.LogDebug($"Camera Manager {Key} changing VLAN for camera '{camera.Key}' on network switch port '{camera.NetworkSwitchPort}' to VLAN ID {targetVlanId} for target codec '{targetCodecKey}' due to PoE disabled event");
                                networkSwitch.SetPortVlan(camera.NetworkSwitchPort, targetVlanId);
                            }
                            else
                            {
                                this.LogError($"Camera Manager {Key} error: target codec with key '{targetCodecKey}' from scenario config not found in managed codecs when handling network switch port state change");
                            }
                        }
                        else
                        {
                            this.LogError($"Camera Manager {Key} error: no codec config found in current scenario for camera '{camera.Key}' when handling network switch port state change");
                        }
                    }
                    else
                    {
                        this.LogError($"Camera Manager {Key} error: current room combination scenario '{currentScenario.Key}' not found in scenario config when handling network switch port state change");
                    }
                }
                else
                {
                    this.LogDebug($"Camera Manager {Key} detected PoE disabled event on port '{e.Port}' but no managed camera is assigned to that port");
                }
            }
            else if (e.EventType == NetworkSwitchPortEventType.VlanChanged)
            {
                // Once a port's VLAN is changed, we want to enable PoE on that port again to help ensure that the camera reconnects and pairs with the correct codec based on the new VLAN
                networkSwitch.SetPortPoeState(e.Port, true);
                this.LogDebug($"Camera Manager {Key} re-enabled PoE on network switch port '{e.Port}' after VLAN change to help ensure camera reconnects and pairs with correct codec based on new VLAN");
            }
        }

        private void RoomCombiner_RoomCombinationScenarioChanged(object sender, EventArgs e)
        {
            var currentScenario = roomCombiner.CurrentScenario;

            this.LogInformation($"Camera Manager {Key} detected room combination scenario change to '{currentScenario}'");

            if (config.RoomCombinerConfig.CombineScenarios.TryGetValue(currentScenario.Key, out var scenarioConfig))
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

                        // Here we would implement the logic to assign the camera to the codec, e.g. by calling a method on the codec interface
                        this.LogDebug($"Camera Manager {Key} would assign camera '{cameraKey}' to codec '{codecConfig.CodecKey}' based on scenario '{currentScenario.Key}'");
                        this.LogDebug($"Camera Manager {Key} sending factory reset command for camera '{cameraKey}' on codec '{camera.ParentCodec.Key}' to trigger re-pairing with correct codec based on new scenario");
                        camera.ParentCodec.CameraFactoryReset(camera.CameraId);
                    }
                }
            }
            else
                {
                    this.LogInformation($"Camera Manager {Key} has no configuration for room combination scenario '{currentScenario}'");
                }
            }

        private void Codec_CameraDisconnected(object sender, CameraEventArgs e)
        {
            var codec = sender as CiscoCodec;

            var camera = managedCameras.FirstOrDefault((c) => c.Value.SerialNumber == e.SerialNumber).Value;
            if (camera == null)
            {
                this.LogDebug($"Camera Manager {Key} received CameraDisconnected event from codec {codec?.Key} for camera serial number {e.SerialNumber} but no managed camera has that serial number");
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

            // When a camera disconnects from a non-target codec, turn off PoE and clear the assigned serial number to trigger the VLAN change cascade
            this.LogDebug($"Camera Manager {Key} handling CameraDisconnected event for camera '{camera.Key}' (Serial Number {e.SerialNumber})");
            this.LogDebug($"Camera Manager {Key} turning off PoE for camera '{camera.Key}' on network switch port '{camera.NetworkSwitchPort}'");
            networkSwitch.SetPortPoeState(camera.NetworkSwitchPort, false);

            this.LogDebug($"Camera Manager {Key} clearing assigned serial number for camera '{camera.Key}' on codec");
            (sender as CiscoCodec)?.ClearCameraAssignedSerialNumber(e.CameraId);
        }

        private void Codec_CameraConnected(object sender, CameraEventArgs e)
        {
            var codec = sender as CiscoCodec;

            var camera = managedCameras.FirstOrDefault((c) => c.Value.SerialNumber == e.SerialNumber).Value;
            if (camera == null)
            {
                this.LogWarning($"Camera Manager {Key} received CameraConnected event from codec {codec?.Key} for camera serial number {e.SerialNumber} but no managed camera has that serial number");
                return;
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
                codec.SetCameraAssignedSerialNumber(camera.CameraId, camera.SerialNumber);
            }
            else
            {
                this.LogError($"Camera Manager {Key} error: sender of CameraConnected event is not a codec when handling camera connect for camera '{camera.Key}'");
            }
        }
    }
}