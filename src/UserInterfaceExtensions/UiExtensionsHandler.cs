using epi_videoCodec_ciscoExtended.UserInterfaceWebViewDisplay;
using PepperDash.Core;
using PepperDash.Essentials.Devices.Common.VideoCodec.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace epi_videoCodec_ciscoExtended.UserInterfaceExtensions
{
	public class UiExtensionsHandler : ICiscoCodecUiExtensionsHandler, IVideoCodecUiExtensionsHandler
	{
		private readonly IKeyed _parent;
		private readonly IBasicCommunication _coms;
		private readonly Action<string> EnqueuCommand;

		public Action<UiWebViewDisplayActionArgs> UiWebViewDisplayAction { get; set; }

		public event EventHandler<UiExtensionsClickedEventArgs> UiExtensionsClickedEvent;

		public UiExtensionsHandler(IKeyed parent, IBasicCommunication coms, Action<string> enqueueCommand)

		{
			_parent = parent;
			_coms = coms;
			EnqueuCommand = enqueueCommand;
			//set the action that will run when called with args from elsewhere via interface
			UiWebViewDisplayAction =
				new Action<UiWebViewDisplayActionArgs>((UiWebViewDisplayActionArgs args) =>
				{
					Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "WebViewDisplayAction URL: {0}", _parent, args.Url);
					Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "WebViewDisplayAction Header: {0}", _parent, args.Header);
					Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "WebViewDisplayAction Mode: {0}", _parent, args.Mode);
					Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "WebViewDisplayAction Title: {0}", _parent, args.Title);
					Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "WebViewDisplayAction Target: {0}", _parent, args.Target);
					UiWebViewDisplay uwvd = new UiWebViewDisplay { Header = args.Header, Url = args.Url, Mode = args.Mode, Title = args.Title };
					//coms.SendText(uwvd.xCommand());
					EnqueuCommand(uwvd.xCommand());
				});

		}

		public void ParseStatus(Panels.CiscoCodecEvents.Panel panel)
		{
			Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "UiExtensionsHandler Parse Status Panel Clicked: {0}", _parent, panel.Clicked.PanelId.Value);
			if (panel.Clicked != null && panel.Clicked.PanelId != null && panel.Clicked.PanelId.Value != null)
			{
				Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "UiExtensionsHandler Parse Status Panel Clicked Raise Event: {0}", _parent, panel.Clicked.PanelId.Value);
				UiExtensionsClickedEvent?.Invoke(this, new UiExtensionsClickedEventArgs(true, panel.Clicked.PanelId.Value));
			}
		}
	}
}
