using System.Collections.Generic;
using Newtonsoft.Json;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras
{

    /// <summary>
    /// Configuration properties for the Camera Manager. 
    /// These are used to configure the Camera Manager's behavior and link it to other devices like the network switch and room combiner.
    /// </summary>
    /// <example>
    ///   {
    ///   "key": "cameraManager1",
    ///   "name": "Camera Manager",
    ///   "type": "cameramanager",
    ///   "properties": {
    ///       "networkSwitchKey": "networkSwitch1",
    ///       "roomCombinerConfig": {
    ///         "roomCombinerKey": "roomCombiner1",
    ///         "combineScenarios": {
    ///           "divided": {
    ///             "codecConfigs": [
    ///               { "codecKey": "codecA", "cameraKeys": ["cameraA"},
    ///               { "codecKey": "codecB", "cameraKeys": ["cameraB"]},
    ///               { "codecKey": "codecC", "cameraKeys": ["cameraC"]},
    ///             ]
    ///           },
    ///           "combined": {
    ///             "codecConfigs": [
    ///             { "codecKey": "codecB", "cameraKeys": ["cameraA", "cameraB", "cameraC"]},
    ///             ]
    ///           },
    ///           "abCombined": {
    ///           "codecConfigs": [
    ///             { "codecKey": "codecB", "cameraKeys": ["cameraA", "cameraB"]},
    ///             { "codecKey": "codecC", "cameraKeys": ["cameraC"]},
    ///             ]
    ///           },
    ///           "bcCombined": {
    ///           "codecConfigs": [
    ///             { "codecKey": "codecB", "cameraKeys": ["cameraB", "cameraC"]},
    ///             { "codecKey": "codecA", "cameraKeys": ["cameraA"]},
    ///             ]
    ///           }
    ///       }
    ///     }
    ///   }
    /// </example>
    public class CameraManagerPropertiesConfig
    {

        /// <summary>
        /// Essentials device key of the network switch that controls the cameras'
        /// PoE power and VLAN assignment. The referenced device must implement
        /// <see cref="INetworkSwitchPoeVlanManager"/>.
        /// </summary>
        [JsonProperty("networkSwitchKey")]
        public string NetworkSwitchKey { get; set; }

        // /// <summary>
        // /// Maximum milliseconds to wait for the camera-ghost (unpaired) event after
        // /// issuing a factory-reset command. Defaults to 30 000 ms.
        // /// </summary>
        // [JsonProperty("cameraGhostTimeoutMs")]
        // public int CameraGhostTimeoutMs { get; set; }

        // /// <summary>
        // /// Maximum milliseconds to wait for the camera to appear on the target codec
        // /// after PoE power is restored. Defaults to 60 000 ms.
        // /// </summary>
        // [JsonProperty("cameraConnectTimeoutMs")]
        // public int CameraConnectTimeoutMs { get; set; }

        // /// <summary>
        // /// Delay in milliseconds before turning on PoE power for a camera after a factory reset. Defaults to 5000 ms.
        // /// </summary>
        // [JsonProperty("cameraPoeOnDelayMs")]
        // public int CameraPoeOnDelayMs { get; set; } = 5000;

        /// <summary>
        /// Configuration for the room combiner device that the Camera Manager will use.
        /// </summary>
        [JsonProperty("roomCombinerConfig")]
        public CameraManagerRoomCombinerConfig RoomCombinerConfig { get; set; }

    }

    /// <summary>
    /// Configuration for the room combiner device that the Camera Manager will use to assign cameras to codecs in combination scenarios.
    /// </summary>
    public class CameraManagerRoomCombinerConfig
    {
        /// <summary>
        /// Key of the room combiner device
        /// </summary>
        [JsonProperty("roomCombinerKey")]
        public string RoomCombinerKey { get; set; }

        /// <summary>
        /// Dictionary of camera combine scenarios. The key is the key of a scenario in the room combiner, 
        /// and the value is an object that contains the codec key(s) and list(s) of camera keys that should be assigned to that codec in that scenario.
        /// </summary>
        public Dictionary<string, CameraManagerCombineScenarioConfig> CombineScenarios { get; set; }
    }

    /// <summary>
    /// Configuration for a codec and its assigned cameras in a specific combine scenario. 
    /// This is used by the Camera Manager to know which cameras to assign to which codecs when a combine scenario is activated.
    /// </summary>
    public class CameraManagerCombineScenarioConfig
    {
        /// <summary>
        /// Key of the codec that the cameras should be assigned to in this combine scenario
        /// </summary>
        [JsonProperty("codecConfigs")]
        public List<CameraManagerCodecConfig> CodecConfigs { get; set; }
    }

    public class CameraManagerCodecConfig
    {
        /// <summary>
        /// Key of the codec that the cameras should be assigned to in this combine scenario
        /// </summary>
        [JsonProperty("codecKey")]
        public string CodecKey { get; set; }

        /// <summary>
        /// Keys of the cameras that should be assigned to the codec in this combine scenario
        /// </summary>
        [JsonProperty("cameraKeys")]
        public List<string> CameraKeys { get; set; }
    }
}