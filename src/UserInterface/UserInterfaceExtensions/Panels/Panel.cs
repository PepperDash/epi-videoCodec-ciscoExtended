﻿using epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceWebViewDisplay;
using Newtonsoft.Json;
using System;
using System.Xml.Serialization;

namespace epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceExtensions.Panels
{
    public class Panel : ICiscoCodecUiExtensionsPanel
    {
        /// <summary>
        /// clicked event to subscribe to 
        /// </summary>
        public event EventHandler ClickedEvent;
        /// <summary>
        /// trigger clicked event from parsing fb
        /// </summary>
        internal void OnClickedEvent() { ClickedEvent?.Invoke(this, EventArgs.Empty); }

        [XmlElement("Order")]
        [JsonProperty("order")]
        public ushort Order { get; set; }

        [XmlElement("PanelId")]
        [JsonProperty("panelId")]
        public string PanelId { get; set; }

        /// <summary>
        ///  CallControls, ControlPanel, Hidden
        /// </summary>
        [XmlElement("Location")]
        [JsonProperty("location")]
        public string Location { get; set; }

        /// <summary>
        /// The icon on the button. Use one of the preinstalled icons from the list or select Custom to use a custom icon that has been uploaded to the device.
        /// Briefing, Camera, Concierge, Disc, Handset, Help, Helpdesk, Home, Hvac, Info, Input, Language, Laptop, 
        /// Lightbulb, Media, Microphone, Power, Proximity, Record, Spark, Tv, Webex, General, Custom
        /// </summary>
        [XmlElement("Icon")]
        [JsonProperty("icon")]
        public string Icon { get; set; }

        /// <summary>
        /// only needed for custom icons
        /// </summary>
        [XmlElement("IconId")]
        [JsonProperty("iconId")]
        public string IconId { get; set; }

        [XmlElement("Name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("mobileControlPath", NullValueHandling = NullValueHandling.Ignore)]
        [XmlIgnore]
		public string MobileControlPath { get; set; }

        [JsonProperty("uiWebViewDisplay", NullValueHandling = NullValueHandling.Ignore)]
		[XmlIgnore]
		public UiWebViewDisplayConfig UiWebViewDisplay { get; set; }

	}
}
