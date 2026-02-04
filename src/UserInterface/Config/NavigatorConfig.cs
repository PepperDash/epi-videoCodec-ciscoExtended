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

		/// <summary>
		/// Configuration for lockout settings for room combining.
		/// Used as the default lockout for the navigator in Room Combination Scenarios.
		/// </summary>
		[JsonProperty("lockout")]
		public Lockout Lockout { get; set; }

		/// <summary>
		/// List of custom lockouts for the navigator.
		/// Each lockout can have its own mobile control path, UI web view display settings,
		/// poll interval, device key, feedback key, and lock on false behavior.
		/// This allows for flexible lockout configurations based on different conditions or requirements.
		/// </summary>
		[JsonProperty("customLockouts")]
		public List<Lockout> CustomLockouts { get; set; }

		// [JsonProperty("usePersistentWebAppForLockout")]
		// public bool UsePersistentWebAppForLockout { get; set; }

	}
}
