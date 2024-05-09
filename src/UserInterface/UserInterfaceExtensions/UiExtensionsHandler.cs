using epi_videoCodec_ciscoExtended.UserInterface.CiscoCodecUserInterface.MobileControl;
using epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceWebViewDisplay;
using PepperDash.Core;
using PepperDash.Essentials.Devices.Common.VideoCodec.Interfaces;
using System;

namespace epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceExtensions
{
    public class UiExtensionsHandler : ICiscoCodecUiExtensionsHandler, IVideoCodecUiExtensionsHandler
    {
        private readonly IKeyed _parent;
        private readonly Action<string> EnqueuCommand;

        public Action<UiWebViewDisplayActionArgs> UiWebViewDisplayAction { get; set; }

        public event EventHandler<UiExtensionsClickedEventArgs> UiExtensionsClickedEvent;

        public UiExtensionsHandler(IKeyed parent, Action<string> enqueueCommand)

        {
            _parent = parent;
            EnqueuCommand = enqueueCommand;
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
