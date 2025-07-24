using Newtonsoft.Json;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Config
{
	public class NavigatorConfig : UserInterfaceConfig
	{
		[JsonProperty("defaultRoomKey")]
		public string DefaultRoomKey { get; set; }

		[JsonProperty("useDirectServer")]
		public bool UseDirectServer { get; set; }

		[JsonProperty("lockout")]
		public Lockout Lockout { get; set; }
	}
}
