using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.WebView;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Config
{
    /// <summary>
    /// Represents a panel configuration for Cisco Codec UI Extensions.
    /// This class implements the ICiscoCodecUiExtensionsPanel interface and provides properties
    /// for panel configuration such as order, ID, location, icon, name, and custom icon.
    /// It also includes event handling for panel click events.
    /// The class supports serialization to XML and JSON formats for configuration purposes.
    /// </summary>
    public class Panel
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

        /// <inheritdoc />
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
        public List<WebViewDisplayConfig> UiWebViewDisplays { get; set; }

        /// <summary>
        /// Collection of device actions associated with the panel.
        /// This property contains actions that can be executed when the panel is interacted with.
        /// </summary>
        [JsonProperty("deviceActions", NullValueHandling = NullValueHandling.Ignore)]
        [XmlIgnore]
        public List<DeviceActionWrapper> DeviceActions { get; set; }

        /// <summary>
        /// Feedback configuration for the panel.
        /// This property defines how the panel should respond to feedback from associated devices.
        /// </summary>
        [JsonProperty("panelFeedback", NullValueHandling = NullValueHandling.Ignore)]
        [XmlIgnore]
        public PanelFeedback PanelFeedback { get; set; }

        /// <summary>
        /// Collection of feedback configurations for the panel.
        /// This property allows multiple feedback sources to control different aspects of the panel.
        /// Supports both numbered feedback properties (panelFeedback1, panelFeedback2, etc.) and the collection.
        /// </summary>
        [JsonIgnore]
        [XmlIgnore]
        public List<PanelFeedback> PanelFeedbacks { get; set; } = new List<PanelFeedback>();

        /// <summary>
        /// First additional feedback configuration.
        /// </summary>
        [JsonProperty("panelFeedback1", NullValueHandling = NullValueHandling.Ignore)]
        [XmlIgnore]
        public PanelFeedback PanelFeedback1 { get; set; }

        /// <summary>
        /// Second additional feedback configuration.
        /// </summary>
        [JsonProperty("panelFeedback2", NullValueHandling = NullValueHandling.Ignore)]
        [XmlIgnore]
        public PanelFeedback PanelFeedback2 { get; set; }

        /// <summary>
        /// Third additional feedback configuration.
        /// </summary>
        [JsonProperty("panelFeedback3", NullValueHandling = NullValueHandling.Ignore)]
        [XmlIgnore]
        public PanelFeedback PanelFeedback3 { get; set; }

        /// <summary>
        /// Fourth additional feedback configuration.
        /// </summary>
        [JsonProperty("panelFeedback4", NullValueHandling = NullValueHandling.Ignore)]
        [XmlIgnore]
        public PanelFeedback PanelFeedback4 { get; set; }

        /// <summary>
        /// Fifth additional feedback configuration.
        /// </summary>
        [JsonProperty("panelFeedback5", NullValueHandling = NullValueHandling.Ignore)]
        [XmlIgnore]
        public PanelFeedback PanelFeedback5 { get; set; }

        /// <summary>
        /// Gets all configured panel feedbacks (including the legacy single feedback and numbered feedbacks).
        /// </summary>
        /// <returns>Collection of all non-null panel feedback configurations.</returns>
        public List<PanelFeedback> GetAllPanelFeedbacks()
        {
            var feedbacks = new List<PanelFeedback>();

            // Add legacy single feedback for backward compatibility
            if (PanelFeedback != null)
                feedbacks.Add(PanelFeedback);

            // Add numbered feedbacks
            if (PanelFeedback1 != null)
                feedbacks.Add(PanelFeedback1);
            if (PanelFeedback2 != null)
                feedbacks.Add(PanelFeedback2);
            if (PanelFeedback3 != null)
                feedbacks.Add(PanelFeedback3);
            if (PanelFeedback4 != null)
                feedbacks.Add(PanelFeedback4);
            if (PanelFeedback5 != null)
                feedbacks.Add(PanelFeedback5);

            // Add any feedbacks from the collection
            feedbacks.AddRange(PanelFeedbacks ?? new List<PanelFeedback>());

            return feedbacks;
        }
    }

    /// <summary>
    /// Represents feedback configuration for a panel.
    /// This class defines how feedback should be handled for a panel, including the device key,
    /// feedback key, property to change, and the values for true and false states.
    /// It also supports string and integer feedback property values for more complex feedback scenarios.
    /// </summary>
    public class PanelFeedback
    {
        /// <summary>
        /// Device key for the panel feedback.
        /// This is used to identify the device that provides feedback for the panel.
        /// The device MUST implement IHasFeedback.
        /// </summary>
        [JsonProperty("deviceKey")]
        public string DeviceKey { get; set; }

        /// <summary>
        /// Feedback key for the panel.
        /// This key is used to identify the specific feedback to be monitored.
        /// </summary>
        [JsonProperty("feedbackKey")]
        public string FeedbackKey { get; set; }

        /// <summary>
        /// Property to change based on feedback.
        /// This property indicates which aspect of the panel should be updated in response to feedback.
        /// Valid values are defined in the EPanelProperty enum.
        /// </summary>
        [JsonProperty("propertyToChange")]
        public EPanelProperty PropertyToChange { get; set; }

        /// <summary>
        /// Feedback event type.
        /// This property defines the type of feedback event that will trigger changes in the panel.
        /// It can be a boolean, string, or integer type, as defined in the eFeedbackEventType enum.
        /// </summary>
        [JsonProperty("feedbackEventType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public eFeedbackEventType FeedbackEventType { get; set; }

        /// <summary>
        /// Value to set when feedback is true.
        /// This property defines the value that should be applied to the panel when the feedback condition is true.
        /// It is applicable for boolean feedback events.
        /// </summary>
        [JsonProperty("truePropertyValue", NullValueHandling = NullValueHandling.Ignore)]
        public string TruePropertyValue { get; set; }

        /// <summary>
        /// Value to set when feedback is false.
        /// This property defines the value that should be applied to the panel when the feedback condition is false.
        /// It is applicable for boolean feedback events.
        /// </summary>
        [JsonProperty("falsePropertyValue", NullValueHandling = NullValueHandling.Ignore)]
        public string FalsePropertyValue { get; set; }

        /// <summary>
        /// Dictionary of string feedback property values.
        /// This dictionary maps string keys to their corresponding values, allowing for dynamic updates
        /// based on feedback events. It is used when the feedback event type is a string.
        /// </summary>
        [JsonProperty("stringFeedbackPropertyValues", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> StringFeedbackPropertyValues { get; set; }

        /// <summary>
        /// Dictionary of integer feedback property values.
        /// This dictionary maps integer keys to their corresponding values, allowing for dynamic updates
        /// based on feedback events. It is used when the feedback event type is an integer.
        /// </summary>
        [JsonProperty("intFeedbackPropertyValues", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<int, string> IntFeedbackPropertyValues { get; set; }
    }

    /// <summary>
    /// Defines the properties that can be changed on a panel based on feedback.
    /// </summary>
    public enum EPanelProperty
    {
        /// <summary>
        /// Text property of the panel.
        /// </summary>
        Text,
        /// <summary>
        /// Color property of the panel.
        /// </summary>
        Color,
        /// <summary>
        /// Location property of the panel.
        /// </summary>
        Location,
    }

    /// <summary>
    /// Wrapper class for custom icon XML serialization.
    /// </summary>
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


