using Newtonsoft.Json.Linq;
using PepperDash.Essentials.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceWebViewDisplay
{
	public interface ICiscoCodecUiExtensionsWebViewDisplayHandler: ICiscoCodecUiExtensionsWebViewDisplayActions
	{
		void ParseErrorStatus(JToken statusToken);
		void ParseStatus(List<UiWebView> wvs);

		event EventHandler<UiWebViewChanagedEventArgs> UiWebViewChanagedEvent;
		UiWebViewStatus CurrentUiWebViewStatus { get; }
	}

	public class UiWebViewStatus
	{
		public UiWebView UiWebView { get; set; }

		public Status ErrorStatus { get; set; }

		public bool IsError { get; set; }

		public UiWebViewStatus(UiWebView webView)
		{
			UiWebView = webView;
		}

		public UiWebViewStatus(Status errorStatus)
		{
			ErrorStatus = errorStatus;
			IsError = true;
		}

		public UiWebViewStatus()
		{
		}
	}

	public class UiWebViewChanagedEventArgs : EventArgs
	{
		public UiWebViewStatus UiWebViewStatus { get; set; }

		public UiWebViewChanagedEventArgs(UiWebViewStatus webViewStatus)
		{
			UiWebViewStatus = webViewStatus;
		}

		public UiWebViewChanagedEventArgs()
		{
		}
	}
}
