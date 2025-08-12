using System;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.WebView
{
  public class WebViewChangedEventArgs : EventArgs
  {
    public WebViewStatus UiWebViewStatus { get; set; }

    public WebViewChangedEventArgs(WebViewStatus webViewStatus)
    {
      UiWebViewStatus = webViewStatus;
    }

    public WebViewChangedEventArgs()
    {
    }
  }
}
