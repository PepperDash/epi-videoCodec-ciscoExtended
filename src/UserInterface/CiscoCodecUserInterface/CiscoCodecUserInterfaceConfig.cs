using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions;
using Newtonsoft.Json;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.CiscoCodecUserInterface
{
    public class CiscoCodecUserInterfaceConfig : ICiscoCodecUserInterfaceConfig
    {
        [JsonProperty("extensions")]
        public Extensions Extensions { get; set; }

        [JsonProperty("videoCodecKey")]
        public string VideoCodecKey { get; set; }

		[JsonProperty("enableLockoutPoll", NullValueHandling = NullValueHandling.Ignore)]
		public bool? EnableLockoutPoll { get; set; }
	}
}
