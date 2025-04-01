using Newtonsoft.Json;

namespace epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceExtensions.Panels
{
    public class CiscoCodecEvents
    {
        public class PanelId : ValueProperty
        {
            public string _value;

            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("Value")]
            public string Value { get { return _value; } set { _value = value; OnValueChanged(); } }
        }

        public class Clicked
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("PanelId")]
            public PanelId PanelId { get; set; }
        }

        /// <summary>
        /// follows json fb structure
        /// </summary>
        public class Panel : ValueProperty // /Event/UserInterface/Extensions/Panel
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("Clicked")]
            public Clicked Clicked { get; set; }
        }
    }
}
