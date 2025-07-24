using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceWebViewDisplay;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.CiscoCodecUserInterface.MobileControl
{
	public class Lockout
	{
		[JsonProperty("mobileControlPath")]
		public string MobileControlPath { get; set; }

		[JsonProperty("uiWebViewDisplay")]
		public UiWebViewDisplayConfig UiWebViewDisplay { get; set; }

		[JsonProperty("pollInterval")]
		public int PollIntervalMs { get; set; }
	}

	public class McVideoCodecUserInterfaceConfig : CiscoCodecUserInterfaceConfig
	{
		[JsonProperty("defaultRoomKey")]
		public string DefaultRoomKey { get; set; }

		[JsonProperty("useDirectServer")]
		public bool UseDirectServer { get; set; }

		[JsonProperty("lockout")]
		public Lockout Lockout { get; set; }
	}
}
