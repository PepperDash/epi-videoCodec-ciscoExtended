using Newtonsoft.Json;
using System.Collections.Generic;
using System.Xml.Serialization;
using epi_videoCodec_ciscoExtended.UserInterfaceExtensions.Panels;
using epi_videoCodec_ciscoExtended.Xml;
using PepperDash.Core;
using Serilog.Events;
using Crestron.SimplSharp;
using System;
using epi_videoCodec_ciscoExtended.UserInterfaceExtensions;
using epi_videoCodec_ciscoExtended.UserInterfaceWebViewDisplay;

namespace epi_videoCodec_ciscoExtended.UserInterfaceExtensions
{
	/// <summary>
	/// json config to build xml commands
	/// xml config for command raw data to configure UIExtensions via API
	/// Json attributes for config, xml attributes for command structure
	/// </summary>
	[XmlRoot("Extensions")]
	public class Extensions: ICiscoCodecUiExtensions
	{
		[XmlElement("Version")]
		public string Version { get; set; }

		[JsonProperty("configId")] //0-40
		public ushort ConfigId { get; set; }

		[XmlElement("Panel")]
		[JsonProperty("panel")]
		public List<Panel> Panels { get; set; }
		//other extensions later

		public PanelsHandler PanelsHandler { get; set; }

		public void Initialize(IKeyed parent, IBasicCommunication coms)
		{
			PanelsHandler = new PanelsHandler(parent, coms, Panels);
			coms.SendText(xCommand());
		}
		
		/// <summary>
		/// string literal for multiline command
		/// </summary>
		/// <returns></returns>
		public string xCommand() => $@"xCommand UserInterface Extensions Set ConfigId: {ConfigId}
{toXmlString()}
.{CiscoCodec.Delimiter}";

		/// <summary>
		/// converts the props on this object with xml attributes to 
		/// an xml string for the xCommand
		/// </summary>
		/// <returns></returns>
		string toXmlString()
		{
			try
			{
				return XmlConverter.SerializeObject(this);
			}
			catch (System.Exception ex)
			{
				Debug.LogMessage(ex, "XML Command Serialize Failed", null);
				return "";
			}
		}
	}
}