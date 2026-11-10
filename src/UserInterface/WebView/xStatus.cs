using Newtonsoft.Json;
using static PepperDash.Essentials.Plugin.CiscoRoomOsCodec.CiscoCodecStatus;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.WebView
{
	public class Status
	{
		//Error Status
		[JsonProperty("XPath", NullValueHandling = NullValueHandling.Ignore)]
		public XPath XPath { get; set; }

		[JsonProperty("Reason", NullValueHandling = NullValueHandling.Ignore)]
		public Reason Reason { get; set; }

		[JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
		public string StatusString { get; set; }

		//web view status
		[JsonProperty("Value", NullValueHandling = NullValueHandling.Ignore)]
		public string Value { get; set; }
	}

	public class WebView
	{
		public Status Status { get; set; }

		public Type Type { get; set; }

		[JsonProperty("URL")]
		public Url URL { get; set; }

		/// <summary>Controller (navigator), OSD or PersistentWebApp.</summary>
		[JsonProperty("Target")]
		public WebViewTarget Target { get; set; }

		public string id { get; set; }
	}

	public class WebViewTarget
	{
		public string Value { get; set; }
	}
}
