using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceWebViewDisplay;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PepperDash.Essentials.Core;

namespace epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceExtensions.Panels
    {
    /// <summary>
    /// Represents a panel configuration for Cisco Codec UI Extensions.
    /// This class implements the ICiscoCodecUiExtensionsPanel interface and provides properties
    /// for panel configuration such as order, ID, location, icon, name, and custom icon.
    /// It also includes event handling for panel click events.
    /// The class supports serialization to XML and JSON formats for configuration purposes.
    /// </summary>
    public class Panel : ICiscoCodecUiExtensionsPanel
        {
        /// <inheritdoc />
        public event EventHandler ClickedEvent;


        internal void OnClickedEvent() { ClickedEvent?.Invoke(this, EventArgs.Empty); }


        /// <inheritdoc />
        [XmlElement("Order")]
        [JsonProperty("order")]
        public ushort Order { get; set; }

        /// <inheritdoc />
        [XmlElement("PanelId")]
        [JsonProperty("panelId")]
        public string PanelId { get; set; }

        [XmlElement("Location")]
        [JsonProperty("location")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ECiscoPanelLocation Location { get; set; }

        /// <inheritdoc />
        [XmlElement("Color")]
        [JsonProperty("color")]
        public string Color { get; set; }

        /// <inheritdoc />
        [XmlElement("Icon")]
        [JsonProperty("icon")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ECiscoPanelIcons Icon { get; set; }

        /// <summary>
        /// Custom icon wrapper for XML serialization.
        /// </summary>
        [XmlElement("CustomIcon")]
        public CustomIconWrapper CustomIcon { get; set; } = new CustomIconWrapper();

        /// <inheritdoc />
        [XmlIgnore]
        [JsonProperty("iconId")]
        public string IconId
            {
            get => CustomIcon?.Id;
            set
                {
                if (!string.IsNullOrEmpty(value))
                    {
                    if (CustomIcon == null)
                        CustomIcon = new CustomIconWrapper();

                    CustomIcon.Id = value;
                    }
                }
            }

        /// <summary>
        /// Base64 image content for custom icon.
        /// This property maps to CustomIcon.Content and represents the base64-encoded image data.
        /// </summary>
        [XmlIgnore]
        [JsonProperty("customIconContent")]
        public string CustomIconContent
            {
            get => CustomIcon?.Content;
            set
                {
                if (!string.IsNullOrEmpty(value))
                    {
                    if (CustomIcon == null)
                        CustomIcon = new CustomIconWrapper();

                    CustomIcon.Content = value;
                    }
                }
            }

        /// <inheritdoc />
        [XmlElement("Name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// URL for web-based panel content.
        /// This property specifies the URL that should be loaded when the panel is activated.
        /// </summary>
        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        [XmlIgnore]
        public string Url { get; set; }

        /// <summary>
        /// Path for mobile control configuration.
        /// This property specifies the path to mobile control resources or configuration.
        /// </summary>
        [JsonProperty("mobileControlPath", NullValueHandling = NullValueHandling.Ignore)]
        [XmlIgnore]
        public string MobileControlPath { get; set; }

        /// <summary>
        /// Collection of UI web view display configurations.
        /// This property contains configurations for web view displays associated with the panel.
        /// </summary>
        [JsonProperty("uiWebViewDisplays", NullValueHandling = NullValueHandling.Ignore)]
        [XmlIgnore]
        public List<UiWebViewDisplayConfig> UiWebViewDisplays { get; set; }

        /// <summary>
        /// Collection of device actions associated with the panel.
        /// This property contains actions that can be executed when the panel is interacted with.
        /// </summary>
        [JsonProperty("deviceActions", NullValueHandling = NullValueHandling.Ignore)]
        [XmlIgnore]
        public List<DeviceActionWrapper> DeviceActions { get; set; }
        }

    public class CustomIconWrapper
        {
        /// <summary>
        /// The unique identifier of the custom icon.
        /// </summary>
        [XmlElement("Id")]
        public string Id { get; set; }

        /// <summary>
        /// The base64-encoded content of the custom icon.
        /// </summary>
        [XmlElement("Content")]
        public string Content { get; set; }
        }

    /// <summary>
    /// Defines the available predefined icons for Cisco codec panels.
    /// </summary>
    public enum ECiscoPanelIcons
        {
        /// <summary>Briefing icon.</summary>
        Briefing,
        /// <summary>Camera icon.</summary>
        Camera,
        /// <summary>Concierge icon.</summary>
        Concierge,
        /// <summary>Disc icon.</summary>
        Disc,
        /// <summary>Handset icon.</summary>
        Handset,
        /// <summary>Help icon.</summary>
        Help,
        /// <summary>Helpdesk icon.</summary>
        Helpdesk,
        /// <summary>Home icon.</summary>
        Home,
        /// <summary>HVAC icon.</summary>
        Hvac,
        /// <summary>Info icon.</summary>
        Info,
        /// <summary>Input icon.</summary>
        Input,
        /// <summary>Language icon.</summary>
        Language,
        /// <summary>Laptop icon.</summary>
        Laptop,
        /// <summary>Lightbulb icon.</summary>
        Lightbulb,
        /// <summary>Media icon.</summary>
        Media,
        /// <summary>Microphone icon.</summary>
        Microphone,
        /// <summary>Power icon.</summary>
        Power,
        /// <summary>Proximity icon.</summary>
        Proximity,
        /// <summary>Record icon.</summary>
        Record,
        /// <summary>Spark icon.</summary>
        Spark,
        /// <summary>TV icon.</summary>
        Tv,
        /// <summary>Webex icon.</summary>
        Webex,
        /// <summary>General icon.</summary>
        General,
        /// <summary>Sliders icon.</summary>
        Sliders,
        /// <summary>Custom icon (requires custom icon configuration).</summary>
        Custom
        }

    /// <summary>
    /// Defines the available locations where panels can be displayed on the Cisco codec interface.
    /// </summary>
    public enum ECiscoPanelLocation
    {
        /// <summary>Panel appears on the home screen.</summary>
        HomeScreen,
        /// <summary>Panel appears in call controls.</summary>
        CallControls,
        /// <summary>Panel appears on both home screen and call controls.</summary>
        HomeScreenAndCallControls,
        /// <summary>Panel appears in the control panel.</summary>
        ControlPanel,
        /// <summary>Panel is hidden from view.</summary>
        Hidden,
    }
}


