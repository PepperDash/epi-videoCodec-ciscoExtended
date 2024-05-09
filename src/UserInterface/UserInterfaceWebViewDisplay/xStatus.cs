using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceWebViewDisplay
{
	public class Status
	{
		public string Value { get; set; }
		public CiscoCodecStatus.Reason Reason { get; set; }

		public string status { get; set; }
	}

	public class XPath
	{
		public string Value { get; set; }
	}

	public class Type
	{
		public string Value { get; set; }
	}

	public class URL
	{
		public string Value { get; set; }
	}

	public class WebView
	{
		public Status Status { get; set; }
		public Type Type { get; set; }
		public URL URL { get; set; }
		public string id { get; set; }

	}
}
