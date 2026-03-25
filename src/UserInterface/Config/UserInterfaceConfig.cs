using Newtonsoft.Json;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Config
{
    public class UserInterfaceConfig
    {
        /// <summary>
        /// UI Extensions Configuration
        /// </summary>
        [JsonProperty("extensions")]
        public UiExtensions Extensions { get; set; }

        /// <summary>
        /// Video Codec Key for Cisco RoomOS Codec
        /// </summary>
        [JsonProperty("videoCodecKey")]
        public string VideoCodecKey { get; set; }

        /// <summary>
        /// Enable Lockout Polling
        /// </summary>
        [JsonProperty("enableLockoutPoll", NullValueHandling = NullValueHandling.Ignore)]
        public bool? EnableLockoutPoll { get; set; }

        /// <summary>
        /// MAC Address of the Touch Panel
        /// Used to identify the Touch Panel in Cisco RoomOS Codec to switch to the Persistent Web App mode
        /// </summary>
        [JsonProperty("macAddress")]
        public string MacAddress { get; set; }
    }
}
