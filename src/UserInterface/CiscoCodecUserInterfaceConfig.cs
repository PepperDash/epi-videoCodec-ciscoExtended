using epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace epi_videoCodec_ciscoExtended.UserInterface
{
	public class CiscoCodecUserInterfaceConfig
	{
		[JsonProperty("extensions")]
		public Extensions Extensions { get; set; }

		[JsonProperty("videoCodecKey")]
		public string VideoCodecKey { get; set; }
	}

	public class CiscoCodecUserInterfaceMobileControlConfig : CiscoCodecUserInterfaceConfig
	{
		[JsonProperty("defaultRoomKey")]
		public string DefaultRoomKey { get; set; }

		[JsonProperty("useDirectServer")]
		public bool UseDirectServer { get; set; }
	}
}
