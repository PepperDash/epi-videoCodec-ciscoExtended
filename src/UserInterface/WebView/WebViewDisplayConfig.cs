using System.Collections.Generic;
using Newtonsoft.Json;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.WebView
{
    public class WebViewDisplayConfig
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
        private static List<WebViewDisplayConfig> webViewConfigs = new List<WebViewDisplayConfig>();

        // Constructor to add instances to the collection
        public WebViewDisplayConfig()
        {
            webViewConfigs.Add(this);
        }

        // Public method to get all instances
        public static List<WebViewDisplayConfig> GetWebViewConfigs()
        {
            return webViewConfigs;
        }
    }

}
