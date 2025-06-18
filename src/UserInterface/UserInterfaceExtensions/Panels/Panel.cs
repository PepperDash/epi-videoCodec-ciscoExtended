using epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceWebViewDisplay;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using PepperDash.Essentials.Core;

namespace epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceExtensions.Panels
    {
    public class Panel : ICiscoCodecUiExtensionsPanel
        {
        public event EventHandler ClickedEvent;
        internal void OnClickedEvent() { ClickedEvent?.Invoke(this, EventArgs.Empty); }

        [XmlElement("Order")]
        [JsonProperty("order")]
        public ushort Order { get; set; }

        [XmlElement("PanelId")]
        [JsonProperty("panelId")]
        public string PanelId { get; set; }

        [XmlElement("Location")]
        [JsonProperty("location")]
        public string Location { get; set; }

        [XmlElement("Icon")]
        [JsonProperty("icon")]
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public eCiscoPanelIcons Icon { get; set; }

        [XmlElement("CustomIcon")]
        public CustomIconWrapper CustomIcon { get; set; } = new CustomIconWrapper();

        // JSON: iconId → maps to CustomIcon.Id
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

        // JSON: base64 image → maps to CustomIcon.Content
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

        [XmlElement("Name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        [XmlIgnore]
        public string Url { get; set; }

        [JsonProperty("mobileControlPath", NullValueHandling = NullValueHandling.Ignore)]
        [XmlIgnore]
        public string MobileControlPath { get; set; }

        [JsonProperty("uiWebViewDisplays", NullValueHandling = NullValueHandling.Ignore)]
        [XmlIgnore]
        public List<UiWebViewDisplayConfig> UiWebViewDisplays { get; set; }

        [JsonProperty("deviceActions", NullValueHandling = NullValueHandling.Ignore)]
        [XmlIgnore]
        public List<DeviceActionWrapper> DeviceActions { get; set; }
        }

    public class CustomIconWrapper
        {
        [XmlElement("Id")]
        public string Id { get; set; }

        [XmlElement("Content")]
        public string Content { get; set; }
        }

    public enum eCiscoPanelIcons
        {
        Briefing,
        Camera,
        Concierge,
        Disc,
        Handset,
        Help,
        Helpdesk,
        Home,
        Hvac,
        Info,
        Input,
        Language,
        Laptop,
        Lightbulb,
        Media,
        Microphone,
        Power,
        Proximity,
        Record,
        Spark,
        Tv,
        Webex,
        General,
        Sliders,
        Custom
        }
    }
