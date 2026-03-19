using Newtonsoft.Json;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras
{
    /// <summary>
    /// Configuration properties for a Cisco Codec Camera. These are used to create the camera device and link it to the parent codec device.
    /// </summary>
    public class CiscoCodecCameraPropertiesConfig
    {
        [JsonProperty("defaultParentCodecKey")]
        public string DefaultParentCodecKey { get; set; }

        [JsonProperty("defaultCameraId")]
        public uint DefaultCameraId { get; set; }

        [JsonProperty("serialNumber")]
        public string SerialNumber { get; set; }

        [JsonProperty("hardwareId")]
        public string HardwareID { get; set; }

        [JsonProperty("macAddress")]
        public string MacAddress { get; set; }

        [JsonProperty("flipImage")]
        public bool? FlipImage { get; set; }

        [JsonProperty("sourceId")]
        public uint SourceId { get; set; }

        /// <summary>
        /// Optional property to specify the network switch port the camera is connected to.
        /// This is used by the CameraManager to change port settings when the camera is switched to a different codec.
        /// </summary>
        [JsonProperty("networkSwitchPort")]
        public string NetworkSwitchPort { get; set; }
    }
}

