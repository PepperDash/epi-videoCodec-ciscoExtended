using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.CiscoCodecUserInterface.MobileControl;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceWebViewDisplay;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.VideoCodec.Interfaces;
using System;
using System.Collections.Generic;
using static PepperDash.Essentials.Plugin.CiscoRoomOsCodec.CiscoCodecConfiguration;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions
{
	public class UserInterfaceExtensionsHandler : ICiscoCodecUiExtensionsHandler
	{
		private readonly IKeyed _parent;
		private readonly Action<string> EnqueueCommand;

		public Action<UiWebViewDisplayActionArgs> UiWebViewDisplayAction { get; set; }

		public Action<UiWebViewDisplayClearActionArgs> UiWebViewClearAction { get; set; }

		public event EventHandler<UiExtensionsClickedEventArgs> UiExtensionsClickedEvent;
		public event EventHandler<UiWebViewChanagedEventArgs> UiWebViewChanagedEvent;

		public UiWebViewStatus CurrentUiWebViewStatus { get; private set; }

		public UserInterfaceExtensionsHandler(IKeyed parent, Action<string> enqueueCommand)

		{
			_parent = parent;
			EnqueueCommand = enqueueCommand;
			//set the action that will run when called with args from elsewhere via interface
			UiWebViewDisplayAction =
				new Action<UiWebViewDisplayActionArgs>((args) =>
				{
					//Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "WebViewDisplayAction URL: {0}", _parent, args.Url.MaskQParamTokenInUrl());
					Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "WebViewDisplayAction Header: {0}", _parent, args.Header);
					Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "WebViewDisplayAction Mode: {0}", _parent, args.Mode);
					Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "WebViewDisplayAction Title: {0}", _parent, args.Title);
					Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "WebViewDisplayAction Target: {0}", _parent, args.Target);
					UiWebViewDisplay uwvd = new UiWebViewDisplay { Header = args.Header, Url = args.Url, Mode = args.Mode, Title = args.Title, Target = args.Target };
					//coms.SendText(uwvd.xCommand());
					EnqueueCommand(uwvd.xCommand());
				});
			UiWebViewClearAction = new Action<UiWebViewDisplayClearActionArgs>((args) =>
			{
				var target = args.Target;
				if (args.Target == null || args.Target == "")
				{
					target = "Controller";
				}
				Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "WebViewClearAction: {0}", _parent, args);
                EnqueueCommand($"xCommand UserInterface WebView Clear Target:{target}{CiscoCodec.Delimiter}");
            });

			EnqueueCommand($"xFeedback Register Event/UserInterface/WebView/Display{CiscoCodec.Delimiter}");
			EnqueueCommand($"xFeedback Register Event/UserInterface/WebView/Cleared{CiscoCodec.Delimiter}");

		}

		public void ParseStatus(List<UiWebView> wvs)
		{
			if (wvs == null || wvs.Count != 1)
			{
				return;
			}
			//assume 1 navigator only allows 1 webview to display at a time
			//api testing shows only one after changing or closing and reopening
			CurrentUiWebViewStatus = new UiWebViewStatus(wvs[0]);
			UiWebViewChanagedEvent?.Invoke(this, new UiWebViewChanagedEventArgs(CurrentUiWebViewStatus));
		}

		public void ParseErrorStatus(JToken statusToken)
		{
			try
			{
				var status = JsonConvert.DeserializeObject<Status>(statusToken.ToString());
				if(status?.XPath?.Value != null && status?.XPath?.Value != UiWebViewDisplay.xStatusPath)
				{
					Debug.LogMessage(Serilog.Events.LogEventLevel.Error, "[UiExtensionsHandler] XPath Uknown [Parse Status Error] XPath: {0}. Reason:  {1}", _parent, status.XPath.Value, status.Reason.Value);
					return;
				}
				Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "[UiExtensionsHandler] XPath match: [Parse Status Error] XPath: {0}. Reason:  {1}", _parent, status.XPath.Value, status.Reason.Value);
				UiWebViewChanagedEvent?.Invoke(this, new UiWebViewChanagedEventArgs(new UiWebViewStatus(status)));
			}
			catch (Exception e)
			{
				Debug.LogMessage(Serilog.Events.LogEventLevel.Error, $"[UiExtensionsHandler] Parse Status Error: {e.Message} {e.StackTrace}", _parent, null);
			}
		}

		public void ParseStatus(Panels.CiscoCodecEvents.Panel panel)
		{
			Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "[UiExtensionsHandler] Parse Status Panel Clicked: {0}", _parent, panel.Clicked.PanelId.Value);
			if (panel.Clicked != null && panel.Clicked.PanelId != null && panel.Clicked.PanelId.Value != null)
			{
				Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "[UiExtensionsHandler] Parse Status Panel Clicked Raise Event: {0}", _parent, panel.Clicked.PanelId.Value);
				UiExtensionsClickedEvent?.Invoke(this, new UiExtensionsClickedEventArgs(true, panel.Clicked.PanelId.Value));
			}
		}
	}
}
