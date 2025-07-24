using System.Collections.Generic;
using Newtonsoft.Json;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Config
{
	/// <summary>
	/// Configuration settings for the Navigator.
	/// </summary>
	public class NavigatorConfig : UserInterfaceConfig
	{
		/// <summary>
		/// The default room key for the navigator.
		/// </summary>
		[JsonProperty("defaultRoomKey")]
		public string DefaultRoomKey { get; set; }

		[JsonProperty("useDirectServer")]
		public bool UseDirectServer { get; set; }

		[JsonProperty("lockout")]
		public Lockout Lockout { get; set; }

		[JsonProperty("customLockouts")]
		public List<Lockout> CustomLockouts { get; set; }
	}
}
