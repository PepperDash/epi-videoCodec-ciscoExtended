using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceWebViewDisplay
{
    public class UiWebViewDisplayConfig
    {
        [JsonProperty("target", NullValueHandling = NullValueHandling.Ignore)]
        public string Target { get; set; }

        [JsonProperty("mode", NullValueHandling = NullValueHandling.Ignore)]
        public string Mode { get; set; }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title { get; set; }

        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        public string Url { get; set; }

        [JsonProperty("header", NullValueHandling = NullValueHandling.Ignore)]
        public string Header { get; set; }

        [JsonProperty("options", NullValueHandling = NullValueHandling.Ignore)]
        public string Options { get; set; }

        [JsonProperty("queryParams", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> QueryParams { get; set; }

        // Static collection to hold all instances of UiWebViewDisplayConfig
        private static List<UiWebViewDisplayConfig> _webViewConfigs = new List<UiWebViewDisplayConfig>();

        // Constructor to add instances to the collection
        public UiWebViewDisplayConfig()
            {
            _webViewConfigs.Add(this);
            }

        // Public method to get all instances
        public static List<UiWebViewDisplayConfig> GetWebViewConfigs()
            {
            return _webViewConfigs;
            }
        }

}
