namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.WebView
{
	public class WebViewStatus
	{
		public WebView UiWebView { get; set; }

		public Status ErrorStatus { get; set; }

		public bool IsError { get; set; }

		public WebViewStatus(WebView webView)
		{
			UiWebView = webView;
		}

		public WebViewStatus(Status errorStatus)
		{
			ErrorStatus = errorStatus;
			IsError = true;
		}

		public WebViewStatus()
		{
		}
	}
}
