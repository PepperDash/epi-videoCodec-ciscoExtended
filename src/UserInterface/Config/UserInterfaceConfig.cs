using Newtonsoft.Json;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Config
{
    public class UserInterfaceConfig
    {
        [JsonProperty("extensions")]
        public UiExtensions Extensions { get; set; }

        [JsonProperty("videoCodecKey")]
        public string VideoCodecKey { get; set; }

        [JsonProperty("enableLockoutPoll", NullValueHandling = NullValueHandling.Ignore)]
        public bool? EnableLockoutPoll { get; set; }
    }
}
