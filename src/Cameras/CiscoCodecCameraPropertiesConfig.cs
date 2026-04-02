using Newtonsoft.Json;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras
{
    /// <summary>
    /// Configuration properties for a Cisco Codec Camera. These are used to create the camera device and link it to the parent codec device.
    /// </summary>
    /// <example>
    /// {
    ///     "key": "camera1",
    ///     "name": "Camera 1",
    ///     "type": "ciscocamera",
    ///     "properties": {
    ///         "defaultParentCodecKey": "codec1",
    ///         "defaultCameraId": 1,
    ///         "serialNumber": "123456789",
    ///         "macAddress": "00:1A:2B:3C:4D:5E",
    ///         "flipImage": false,
    ///         "sourceId": 1,
    ///         "networkSwitchPort": "Gi/0/1",
    ///        }
    /// }
    /// </example>
    public class CiscoCodecCameraPropertiesConfig
    {
        [JsonProperty("defaultParentCodecKey")]
        public string DefaultParentCodecKey { get; set; }

        [JsonProperty("defaultCameraId")]
        public uint DefaultCameraId { get; set; }

        [JsonProperty("serialNumber")]
        public string SerialNumber { get; set; }

        // [JsonProperty("hardwareId")]
        // public string HardwareID { get; set; }

        [JsonProperty("macAddress")]
        public string MacAddress { get; set; }

        [JsonProperty("flipImage")]
        public bool? FlipImage { get; set; }

        /// <summary>
        /// The source ID of the camera on the codec. This is used to link the camera to the correct video source on the codec for switching and management purposes.
        /// 
        /// </summary>
        
        // TODO: Need to test this at runtime and implement logic to set the source ID on the codec's corresponding camera object if it doesn't match this value.
        // This is important for ensuring the camera is correctly linked to the codec's video sources for switching and management purposes.
        [JsonProperty("sourceId")]
        public uint SourceId { get; set; }

        /// <summary>
        /// Optional property to specify the network switch port the camera is connected to.
        /// This is used by the CameraManager to change port settings when the camera is switched to a different codec.
        /// </summary>
        [JsonProperty("networkSwitchPort")]
        public string NetworkSwitchPort { get; set; }

        /// <summary>
        /// Optional property to specify whether to maintain the configured camera ID when the camera is switched to a different codec. 
        /// This is used by the CameraManager to determine whether to change the camera ID when switching codecs.
        /// </summary>
        [JsonProperty("maintainConfiguredCameraId")]
        public bool? MaintainConfiguredCameraId { get; set; }
    }
}

