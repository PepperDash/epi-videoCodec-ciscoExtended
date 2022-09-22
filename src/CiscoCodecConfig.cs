﻿using System.Collections.Generic;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.Codec;

using Newtonsoft.Json;

namespace epi_videoCodec_ciscoExtended
{
    public class CiscoCodecConfig
    {
        [JsonProperty("communicationMonitorProperties")]
        public CommunicationMonitorConfig CommunicationMonitorProperties { get; set; }

        [JsonProperty("favorites")]
        public List<CodecActiveCallItem> Favorites { get; set; }

        /// <summary>
        /// Valid values: "Local" or "Corporate"
        /// </summary>
        [JsonProperty("phonebookMode")]
        public string PhonebookMode { get; set; }

        [JsonProperty("showSelfViewByDefault")]
        public bool ShowSelfViewByDefault { get; set; }

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

        [JsonProperty("phonebookDisableAutoPopulate")]
        public bool PhonebookDisableAutoPopulate { get; set; }

        [JsonProperty("phonebookDisableAutoDial")]
        public bool PhonebookDisableAutoDial { get; set; }

        [JsonProperty("UiBranding")]
        public BrandingLogoProperties UiBranding { get; set; }

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



        public CiscoCodecConfig()
        {
            CameraInfo = new List<CameraInfo>();
        }
    }

    public class SharingProperties
    {
        [JsonProperty("autoShareContentWhileInCall")]
        public bool AutoShareContentWhileInCall { get; set; }
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
}