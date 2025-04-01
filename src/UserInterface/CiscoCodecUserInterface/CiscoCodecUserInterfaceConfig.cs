using epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceExtensions;
using Newtonsoft.Json;

namespace epi_videoCodec_ciscoExtended.UserInterface.CiscoCodecUserInterface
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
