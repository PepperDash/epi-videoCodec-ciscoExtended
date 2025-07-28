using Newtonsoft.Json;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.WebView;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Config
{
  public class Lockout
  {
    [JsonProperty("mobileControlPath")]
    public string MobileControlPath { get; set; }


    [JsonProperty("uiWebViewDisplay")]
    public WebViewDisplayConfig UiWebViewDisplay { get; set; }

    [JsonProperty("pollInterval")]
    public int PollIntervalMs { get; set; }

    [JsonProperty("deviceKey")]
    public string DeviceKey { get; set; }

    [JsonProperty("feedbackKey")]
    public string FeedbackKey { get; set; }

    [JsonProperty("lockOnFalse")]
    public bool LockOnFalse { get; set; }
  }
}
