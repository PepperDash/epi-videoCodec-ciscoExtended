using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions.Panels;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Xml;
using Newtonsoft.Json;
using PepperDash.Core;
using Serilog.Events;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions
{
    /// <summary>
    /// json config to build xml commands
    /// xml config for command raw data to configure UIExtensions via API
    /// Json attributes for config, xml attributes for command structure
    /// </summary>
    [XmlRoot("Extensions")]
    public class Extensions : ICiscoCodecUiExtensions
    {
        private IKeyed parent;
        [XmlElement("Version")]
        public string Version { get; set; }

        [XmlIgnore]
        [JsonProperty("configId")] //0-40
        public ushort ConfigId { get; set; }

        [XmlElement("Panel")]
        [JsonProperty("panels")]
        public List<Panel> Panels { get; set; }

        [XmlIgnore]
        [JsonProperty("doNotSendXml")]
        public bool SkipXml { get; set; }
        // Lets you skip sending the XML command on init if they are added externally

        //other extensions later

        [JsonIgnore]
        [XmlIgnore]
        public PanelsHandler PanelsHandler { get; set; }

        /// <summary>
        /// Initializes the Extensions object, setting up the PanelsHandler and sending the XML command if SkipXml is false.
        /// </summary>
        /// <param name="parent">The parent device that this Extensions object belongs to.</param>
        /// <param name="enqueueCommand">Action to enqueue the command for sending.</param>
        /// <remarks>
        /// This method initializes the PanelsHandler with the provided parent and command enqueue action.
        /// If SkipXml is false, it constructs the XML command string using the xCommand method
        /// and enqueues it for sending.
        /// </remarks>
        /// <exception cref="Exception">Thrown if XML serialization fails.</exception>       

        public void Initialize(IKeyed parent, Action<string> enqueueCommand)
        {
            this.parent = parent;

            Debug.LogMessage(LogEventLevel.Debug, "Extensions Initialize, Panels from config: null: {0}, length: {1}", parent, Panels == null, Panels.Count);
            PanelsHandler = new PanelsHandler(parent, enqueueCommand, Panels);
            //Debug.LogMessage(LogEventLevel.Warning, xCommand(), parent);

            if (SkipXml)
            {
                return;
            }

            var xml = xCommand();

            Debug.LogDebug(parent, "Sending XML data: {xml}", xml);

            enqueueCommand(xml);
        }

        /// <summary>
        /// Updates the Extensions configuration by sending the XML command if SkipXml is false.
        /// </summary>
        /// <param name="enqueueCommand">Action to enqueue the command for sending.</param>
        public void Update(Action<string> enqueueCommand)
        {
            if (SkipXml)
            {
                return;
            }

            var xml = xCommand();

            Debug.LogDebug(parent, "Sending XML data: {xml}", xml);

            enqueueCommand(xml);
        }



        /// <summary>
        /// Generates the xCommand string for configuring UI Extensions on the Cisco codec.
        /// </summary>
        /// <returns>The complete xCommand string including ConfigId and XML configuration data.</returns>
        public string xCommand() => $@"xCommand UserInterface Extensions Set ConfigId: {ConfigId}
{ToXmlString()}.{CiscoCodec.Delimiter}";

        /// <summary>
        /// converts the props on this object with xml attributes to 
        /// an xml string for the xCommand
        /// </summary>
        /// <returns></returns>
        private string ToXmlString()
        {
            try
            {
                return XmlConverter.SerializeObject(this);
            }
            catch (Exception ex)
            {
                Debug.LogMessage(ex, "XML Command Serialize Failed", null);
                return "";
            }
        }
    }
}