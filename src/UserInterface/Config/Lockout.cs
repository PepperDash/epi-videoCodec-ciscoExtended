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

    /// <summary>
    /// Priority determines which lockout will be displayed when a new lockout is triggered while another lockout is active. 
    /// The lockout with the highest priority will be displayed. If two lockouts have the same priority, 
    /// the most recently triggered lockout will be displayed.
    /// </summary>
    [JsonProperty("priority")]
    public int Priority { get; set; } = 0;
  }
}
