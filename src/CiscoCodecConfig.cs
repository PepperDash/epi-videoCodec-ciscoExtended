using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.Codec;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.WebView;
using UiExtensions = PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Config.UiExtensions;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// Configuration class for Cisco video codec settings and properties.
    /// Contains all configurable options for codec behavior, UI extensions, sharing, and external sources.
    /// </summary>
    public class CiscoCodecConfig
    {
        /// <summary>
        /// Gets or sets the communication monitor configuration properties.
        /// </summary>
        [JsonProperty("communicationMonitorProperties")]
        public CommunicationMonitorConfig CommunicationMonitorProperties { get; set; }

        /// <summary>
        /// Gets or sets the list of favorite call destinations.
        /// </summary>
        [JsonProperty("favorites")]
        public List<CodecActiveCallItem> Favorites { get; set; }

        /// <summary>
        /// Valid values: "Local" or "Corporate"
        /// </summary>
        [JsonProperty("phonebookMode")]
        public string PhonebookMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether self view should be shown by default.
        /// </summary>
        [JsonProperty("showSelfViewByDefault")]
        public bool ShowSelfViewByDefault { get; set; }

        /// <summary>
        /// Gets or sets the default monitor role for self view.
        /// Valid values are defined in the EMonitorRole enum.
        /// This property determines where the self view will be displayed on the codec's monitors.
        /// </summary>
        [JsonProperty("selfViewDefaultMonitorRole", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public EMonitorRole? SelfViewDefaultMonitorRole { get; set; }

        /// <summary>
        /// Gets or sets the content sharing configuration properties.
        /// </summary>
        [JsonProperty("sharing")]
        public SharingProperties Sharing { get; set; }

        /// <summary>
        /// Enables external source switching capability
        /// </summary>
        [JsonProperty("externalSourceListEnabled")]
        public bool ExternalSourceListEnabled { get; set; }

        /// <summary>
        /// The name of the routing input port on the codec to which the external switch is connected
        /// </summary>
        [JsonProperty("externalSourceInputPort")]
        public string ExternalSourceInputPort { get; set; }

        /// <summary>
        /// Optionsal property to set the limit of any phonebook queries for directory or searching
        /// </summary>
        [JsonProperty("phonebookResultsLimit")]
        public uint PhonebookResultsLimit { get; set; }

        [JsonProperty("overrideMeetingsLimit")]
        public bool OverrideMeetingsLimit { get; set; }

        public bool EnableCommDebugOnStartup { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to get the phonebook on startup.
        /// If true, the codec will fetch the phonebook data when it starts up.
        /// Default is true.
        /// </summary>
        [JsonProperty("getPhonebookOnStartup")]
        public bool GetPhonebookOnStartup { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to get bookings on startup.
        /// If true, the codec will fetch the bookings data when it starts up.
        /// Default is true.
        /// </summary>
        [JsonProperty("getBookingsOnStartup")]
        public bool GetBookingsOnStartup { get; set; } = true;

        [JsonProperty("phonebookDisableAutoPopulate")]
        public bool PhonebookDisableAutoPopulate { get; set; }

        [JsonProperty("phonebookDisableAutoDial")]
        public bool PhonebookDisableAutoDial { get; set; }

        [JsonProperty("UiBranding")]
        public BrandingLogoProperties UiBranding { get; set; }

        [JsonProperty("enableCameraConfigFromRoomCameraList")]
        public bool EnableCameraConfigFromRoomCameraList { get; set; }

        [JsonProperty("cameraInfo")]
        public List<CameraInfo> CameraInfo { get; set; }

        [JsonProperty("defaultTrackingMode")]
        public string DefaultCameraTrackingMode { get; set; }
        //Valid options "speakerTrack", "presenterTrack", "speaker", or "presenter"

        [JsonProperty("timeFormatSpecifier")]
        public string TimeFormatSpecifier { get; set; }

        [JsonProperty("dateFormatSpecifier")]
        public string DateFormatSpecifier { get; set; }

        [JsonProperty("joinableCooldownSeconds")]
        public int JoinableCooldownSeconds { get; set; }

        [JsonProperty("endAllCallsOnMeetingJoin")]
        public bool EndAllCallsOnMeetingJoin { get; set; }

        /// <summary>
        /// Indicates whether to use the Persistent Web App for lockout scenarios on the navigator panels
        /// </summary>
        [JsonProperty("usePersistentWebAppForLockout")]
        public bool UsePersistentWebAppForLockout { get; set; }


        /// <summary>
        /// These are key-value pairs, uint id, string type
        /// They are used to pass back UI Extension Widget events
        /// </summary>
        [JsonProperty("Widgets")]
        public Dictionary<string, WidgetConfig> WidgetBlocks { get; set; }

        [JsonProperty("extensions")]
        public UiExtensions Extensions { get; set; }

        [JsonProperty("emergency")]
        public Emergency Emergency { get; set; }

        [JsonProperty("defaultProvisioningMode", NullValueHandling = NullValueHandling.Ignore)]
        public string DefaultProvisioningMode { get; set; }

        public CiscoCodecConfig()
        {
            CameraInfo = new List<CameraInfo>();
            PhonebookMode = "corporate";
        }
    }

    public class SharingProperties
    {
        [JsonProperty("autoShareContentWhileInCall")]
        public bool AutoShareContentWhileInCall { get; set; }

        [JsonProperty("defaultShareLocalOnly")]
        public bool DefaultShareLocalOnly { get; set; }
    }

    public class BrandingLogoProperties
    {
        [JsonProperty("enable")]
        public bool Enable { get; set; }

        [JsonProperty("brandingUrl")]
        public string BrandingUrl { get; set; }
    }

    /// <summary>
    /// Describes configuration information for the near end cameras
    /// </summary>
    public class CameraInfo
    {
        public int CameraNumber { get; set; }
        public string Name { get; set; }
        public int SourceId { get; set; }
    }

    public class WidgetConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; } // e.g. "blinds"

        [JsonProperty("value")]
        public string Value { get; set; } // e.g. "increment"

        [JsonProperty("type")]
        public string Type { get; set; } // e.g. "Pressed"

        [JsonProperty("bridgeIndex")]
        public uint? BridgeIndex { get; set; }
    }

    public class Emergency
    {
        [JsonProperty("mobileControlPath")]
        public string MobileControlPath { get; set; }

        [JsonProperty("uiWebViewDisplay")]
        public WebViewDisplay UiWebViewDisplay { get; set; }
    }
}