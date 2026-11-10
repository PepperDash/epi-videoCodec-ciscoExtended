using System.Collections.Generic;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.WebView
{
	public class WebViewStatus
	{
		/// <summary>Every web view the status message listed (any target, any status).</summary>
		public IReadOnlyList<WebView> WebViews { get; private set; } = new WebView[0];

		/// <summary>
		/// The first listed web view. A navigator can list several: use <see cref="WebViews"/>. Setting it
		/// replaces <see cref="WebViews"/> with that one view (none for null), as before.
		/// </summary>
		[System.Obsolete("A status can list several web views; use WebViews.")]
		public WebView UiWebView
		{
			get => WebViews.Count > 0 ? WebViews[0] : null;
			set => WebViews = value == null ? new WebView[0] : new[] { value };
		}

		public Status ErrorStatus { get; set; }

		public bool IsError { get; set; }

		public WebViewStatus(IReadOnlyList<WebView> webViews)
		{
			WebViews = webViews ?? new WebView[0];
		}

		[System.Obsolete("A status can list several web views; use WebViewStatus(IReadOnlyList<WebView>).")]
		public WebViewStatus(WebView webView)
			: this(webView == null ? new WebView[0] : new[] { webView })
		{
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
