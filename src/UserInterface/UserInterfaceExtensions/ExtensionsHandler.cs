using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.WebView;


namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions
{
	public class ExtensionsHandler
	{
		private readonly IKeyed _parent;
		private readonly Action<string> EnqueueCommand;

		public Action<WebViewDisplayActionArgs> UiWebViewDisplayAction { get; set; }

		public Action<WebViewDisplayClearActionArgs> UiWebViewClearAction { get; set; }

		public event EventHandler<UiExtensionsClickedEventArgs> UiExtensionsClickedEvent;
		public event EventHandler<WebViewChangedEventArgs> UiWebViewChangedEvent;

		public WebViewStatus CurrentUiWebViewStatus { get; private set; }

		public ExtensionsHandler(IKeyed parent, Action<string> enqueueCommand)
		{
			_parent = parent;
			EnqueueCommand = enqueueCommand;
			//set the action that will run when called with args from elsewhere via interface
			UiWebViewDisplayAction =
				new Action<WebViewDisplayActionArgs>((args) =>
				{
					var webViewDisplay = new WebViewDisplay { Header = args.Header, Url = args.Url, Mode = args.Mode, Title = args.Title, Target = args.Target };

					EnqueueCommand(webViewDisplay.xCommand());
				});
			UiWebViewClearAction = new Action<WebViewDisplayClearActionArgs>((args) =>
			{
				var target = string.IsNullOrEmpty(args.Target) ? "Controller" : args.Target;

				EnqueueCommand($"xCommand UserInterface WebView Clear Target:{target}{CiscoCodec.Delimiter}");
			});

			EnqueueCommand($"xFeedback Register Event/UserInterface/WebView/Display{CiscoCodec.Delimiter}");
			EnqueueCommand($"xFeedback Register Event/UserInterface/WebView/Cleared{CiscoCodec.Delimiter}");

		}

		public void ParseStatus(List<WebView.WebView> webViews)
		{
			if (webViews == null || webViews.Count > 1)
			{
				return;
			}
			//assume 1 navigator only allows 1 webview to display at a time
			//api testing shows only one after changing or closing and reopening
			CurrentUiWebViewStatus = new WebViewStatus(webViews[0]);
			UiWebViewChangedEvent?.Invoke(this, new WebViewChangedEventArgs(CurrentUiWebViewStatus));
		}

		public void ParseErrorStatus(JToken statusToken)
		{
			try
			{
				var status = JsonConvert.DeserializeObject<Status>(statusToken.ToString());
				if (status?.XPath?.Value != null && status?.XPath?.Value != WebViewDisplay.xStatusPath)
				{
					Debug.LogMessage(Serilog.Events.LogEventLevel.Error, "[UiExtensionsHandler] XPath Uknown [Parse Status Error] XPath: {0}. Reason:  {1}", _parent, status.XPath.Value, status.Reason.Value);
					return;
				}

				UiWebViewChangedEvent?.Invoke(this, new WebViewChangedEventArgs(new WebViewStatus(status)));
			}
			catch (Exception e)
			{
				Debug.LogMessage(Serilog.Events.LogEventLevel.Error, $"[UiExtensionsHandler] Parse Status Error: {e.Message} {e.StackTrace}", _parent, null);
			}
		}

		public void ParseStatus(Panels.CiscoCodecEvents.Panel panel)
		{
			Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "[UiExtensionsHandler] Parse Status Panel Clicked: {0}", _parent, panel.Clicked.PanelId.Value);

			if (panel.Clicked == null || panel.Clicked.PanelId == null || panel.Clicked.PanelId.Value == null)
			{
				return;
			}

			UiExtensionsClickedEvent?.Invoke(this, new UiExtensionsClickedEventArgs(true, panel.Clicked.PanelId.Value));
		}
	}
}
