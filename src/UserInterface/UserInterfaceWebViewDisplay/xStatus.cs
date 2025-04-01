using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static epi_videoCodec_ciscoExtended.CiscoCodecStatus;

namespace epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceWebViewDisplay
{
	public class Status 
	{
		//Error Status
		[JsonProperty("XPath", NullValueHandling = NullValueHandling.Ignore)]
		public XPath XPath { get; set; }

		[JsonProperty("Reason", NullValueHandling = NullValueHandling.Ignore)]
		public CiscoCodecStatus.Reason Reason { get; set; }

		[JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
		public string StatusString { get; set; }

		//web view status
		[JsonProperty("Value", NullValueHandling = NullValueHandling.Ignore)]
		public string Value { get; set; }
	}

	public class UiWebView
	{
		public Status Status { get; set; }

		public CiscoCodecStatus.Type Type { get; set; }

		[JsonProperty("URL")]
		public Url URL { get; set; }

		public string id { get; set; }
	}
}
