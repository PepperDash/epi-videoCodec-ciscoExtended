using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp.Net;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Config;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.RoomCombiner;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.WebView;
using Serilog.Events;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Navigator
{
    internal class NavigatorRoomCombinerLockoutHandler : IKeyed
    {
        private NavigatorController mcTpController;

        private ExtensionsHandler extensionsHandler;

        private RoomCombinerHandler combinerHandler;

        private readonly NavigatorConfig props;

        public string Key { get; }

        private readonly System.Timers.Timer lockoutPollTimer;

        private string defaultRoomKey;
        private string primaryRoomKey;

        private readonly WebViewDisplayConfig defaultUiWebViewDisplayConfig = new WebViewDisplayConfig()
        {
            Title = "Mobile Control",
            Target = "Controller",
            Mode = "Modal"
        };

        internal NavigatorRoomCombinerLockoutHandler(
            NavigatorController ui,
            NavigatorConfig props
        )
        {
            this.props = props;
            mcTpController = ui;

            Key = ui.Key + "-NavigatorRoomCombinerLockout";

            lockoutPollTimer = new System.Timers.Timer(
                                      this.props?.Lockout?.PollIntervalMs > 0 ? this.props.Lockout.PollIntervalMs : 5000
                                  )
            {
                Enabled = false,
                AutoReset = true
            };

            lockoutPollTimer.Elapsed += (s, a) =>
            {
                Debug.LogMessage(LogEventLevel.Verbose, "Lockout Poll Timer Elapsed", this);
                if (!mcTpController.LockedOut)
                {
                    Debug.LogMessage(LogEventLevel.Verbose, "_mcTpController.LockedOut: {LockedOut}", this, mcTpController.LockedOut);
                    //if not in lockout state and was previously locked out
                    CancelLockoutTimer();
                    return;
                }

                mcTpController.Parent.EnqueueCommand(WebViewDisplay.xCommandStatus());
            };
        }

        internal void Activate(NavigatorController parent)
        {
            //set private props after activation so everything is instantiated
            if (parent == null)
            {
                Debug.LogMessage(LogEventLevel.Debug, "Error: parent navigator controller is null", this);
                return;
            }

            mcTpController = parent;

            extensionsHandler = parent.UiExtensionsHandler;

            combinerHandler = parent.RoomCombinerHandler;

            if (extensionsHandler == null)
            {
                Debug.LogMessage(LogEventLevel.Debug, "[Warning]: VideoCodecUiExtensionsHandler is null. Skipping VideoCodecMobileControlRouter Subscriptions", this);
                return;
            }

            mcTpController.Parent.IsReadyChange += (s, a) =>
            {
                if (!mcTpController.Parent.IsReady) return;

                //send lockout if in lockout state
                HandleRoomCombineScenarioChanged();
            };

            if (combinerHandler.EssentialsRoomCombiner != null)
            {
                //subscribe to events for routing buttons from codec ui to mobile control
                combinerHandler.EssentialsRoomCombiner.RoomCombinationScenarioChanged +=
                    Combiner_RoomCombinationScenarioChanged_Lockout_EventHandler;
            }

            extensionsHandler.UiExtensionsClickedEvent +=
                VideoCodecUiExtensionsClickedMcEventHandler;

            defaultRoomKey = mcTpController?.DefaultRoomKey;
        }

        private void Combiner_RoomCombinationScenarioChanged_Lockout_EventHandler(
            object sender,
            EventArgs e
        )
        {
            HandleRoomCombineScenarioChanged();
        }

        private void HandleRoomCombineScenarioChanged()
        {
            try
            {
                var combiner = combinerHandler?.EssentialsRoomCombiner;
                var currentScenario = combiner?.CurrentScenario;
                var uimap = currentScenario?.UiMap;

                if (uimap == null)
                {
                    Debug.LogMessage(LogEventLevel.Debug, "uimap is null", this);
                    return;
                }

                var currentScenarioRoomKey = (
                    uimap?.FirstOrDefault((kv) => kv.Key == defaultRoomKey)
                )?.Value;


                if (!uimap.TryGetValue("primary", out primaryRoomKey))
                {
                    Debug.LogMessage(LogEventLevel.Debug, "Primary room key not found in UiMap for scenario: {ScenarioKey}", this, currentScenario.Key);
                }

                if (currentScenarioRoomKey == null)
                {
                    Debug.LogMessage(LogEventLevel.Debug, "[ERROR] UiMap default room key: {DefaultRoomKey} Error: UiMap must have an entry keyed to default room key with value of room connection for room state {ScenarioKey} or lockout", this, defaultRoomKey, currentScenario.Key);
                    return;
                }
                if (currentScenarioRoomKey == "lockout")
                {
                    Debug.LogMessage(LogEventLevel.Debug, "UiMap default room key {DefaultRoomKey} is in lockout state", this, defaultRoomKey);

                    mcTpController.LockedOut = true;

                    ClearCiscoCodecUiWebViewController();

                    extensionsHandler.UiWebViewChanagedEvent += LockoutUiWebViewChanagedEventHandler;

                    mcTpController.Parent.EnqueueCommand(WebViewDisplay.xCommandStatus());

                    if (mcTpController.EnableLockoutPoll)
                    {
                        // Start the timer when lockout occurs                      
                        lockoutPollTimer.Start();
                        return;
                    }

                    return;
                }

                CancelLockoutTimer();
                Debug.LogMessage(LogEventLevel.Debug, "ui with default room key {DefaultRoomKey} is not locked out", this, defaultRoomKey);
            }
            catch (Exception ex)
            {
                Debug.LogMessage(ex, "Error in Combiner_RoomCombinationScenarioChanged_Lockout_EventHandler", this);
            }
        }

        private void CancelLockoutTimer()
        {
            Debug.LogMessage(LogEventLevel.Verbose, "Canceling Lockout Poll Timer for: {Key}", this, mcTpController.Key);

            extensionsHandler.UiWebViewChanagedEvent -= LockoutUiWebViewChanagedEventHandler;

            mcTpController.LockedOut = false;

            ClearCiscoCodecUiWebViewController();

            lockoutPollTimer.Stop();
        }

        public void LockoutUiWebViewChanagedEventHandler(object sender, WebViewChangedEventArgs args)
        {
            bool isError = args?.UiWebViewStatus?.IsError ?? false;

            if (isError) //isError means no webview open
            {
                Debug.LogMessage(LogEventLevel.Debug, "Error in UiWebViewChanagedEventHandler.  XPath: {XPath}Reason: {Reason}", this, args?.UiWebViewStatus?.ErrorStatus?.XPath?.Value, args?.UiWebViewStatus?.ErrorStatus?.Reason?.Value);

                //if web view not open and in lockout send lockout to web view
                if (mcTpController.LockedOut == true)
                {
                    SendLockout(defaultRoomKey, primaryRoomKey);
                }
                return;
            }

            if (mcTpController.LockedOut == false)
            {
                WebView.WebView uiWebView = args?.UiWebViewStatus?.UiWebView;
                extensionsHandler.UiWebViewClearAction?.Invoke(
                    new WebViewDisplayClearActionArgs() { Target = "Controller" }
                );
            }
        }

        private void SendLockout(string thisUisDefaultRoomKey, string primaryRoomKey)
        {
            Debug.LogMessage(LogEventLevel.Debug, "UiMap default room key: {DefaultRoomKey} is in lockout state", this, thisUisDefaultRoomKey);

            var path = props?.Lockout?.MobileControlPath;

            if (path == null || path.Length == 0)
                path = "/lockout";

            var webViewConfig =
                props?.Lockout?.UiWebViewDisplay == null
                    ? defaultUiWebViewDisplayConfig
                    : props.Lockout.UiWebViewDisplay;

            if (!string.IsNullOrEmpty(primaryRoomKey))
            {
                if (webViewConfig.QueryParams == null)
                {
                    webViewConfig.QueryParams = new Dictionary<string, string>();
                }

                webViewConfig.QueryParams["primaryRoomName"] =
                            DeviceManager.GetDeviceForKey(primaryRoomKey) is IKeyName room ? room.Name : primaryRoomKey;
            }
            SendCiscoCodecUiToWebViewMcUrl(path, webViewConfig);
        }

        private async void VideoCodecUiExtensionsClickedMcEventHandler(
            object sender,
            UiExtensionsClickedEventArgs e
        )
        {
            Debug.LogMessage(LogEventLevel.Debug, "VideoCodecUiExtensionsClickedMcEventHandler: {Id}", this, e.Id);
            try
            {
                //navigator button click build url and use VideoCodecUiExtensionsHandler action to send to mobile control
                var panelId = e.Id;
                var extensions = props.Extensions;
                if (extensions == null || !extensions.Panels.Any())
                {
                    Debug.LogMessage(LogEventLevel.Debug, "No extensions found for VideoCodecMobileControlRouter", this);
                    return;
                }
                var panels = extensions.Panels;
                var mcPanel = panels.Find((pp) => pp.PanelId == panelId);
                if (mcPanel == null)
                {
                    Debug.LogMessage(LogEventLevel.Debug, "Panel not found for VideoCodecMobileControlRouter", this);
                    return;
                }
                if (mcPanel.DeviceActions != null && mcPanel.DeviceActions.Count > 0)
                {
                    foreach (DeviceActionWrapper action in mcPanel.DeviceActions)
                    {
                        if (action == null)
                        {
                            Debug.LogMessage(LogEventLevel.Debug, "DeviceAction is null", this);
                            continue;
                        }
                        Debug.LogMessage(LogEventLevel.Debug, "Running DeviceAction {MethodName}", this, action.MethodName);
                        await DeviceJsonApi.DoDeviceActionAsync(action);
                    }
                }

                if (!string.IsNullOrEmpty(mcPanel.Url))
                {
                    Debug.LogMessage(LogEventLevel.Debug, "Sending URL to WebView: {Url}", this, mcPanel.Url);

                    foreach (WebViewDisplayConfig webView in mcPanel.UiWebViewDisplays)
                    {
                        SendCiscoCodecUiToWebViewUrl(mcPanel.Url, webView);
                    }

                    return;
                }

                if (mcPanel.MobileControlPath == null || mcPanel.MobileControlPath.Length == 0)
                {
                    Debug.LogMessage(LogEventLevel.Debug, "MobileControlPath not found for {PanelName}", this, mcPanel.Name);
                    return;
                }
                if (mcPanel.UiWebViewDisplays == null)
                {
                    Debug.LogMessage(LogEventLevel.Debug, "[Warning] UiWebViewDisplay not found for {PanelName} using default Title: {Title}, Mode: {Mode}, Target: {Target}", this, mcPanel.Name, defaultUiWebViewDisplayConfig.Title, defaultUiWebViewDisplayConfig.Mode, defaultUiWebViewDisplayConfig);
                }

                foreach (WebViewDisplayConfig webView in mcPanel.UiWebViewDisplays)
                {
                    SendCiscoCodecUiToWebViewMcUrl(mcPanel.MobileControlPath, webView);
                }
            }
            catch (Exception ex)
            {
                Debug.LogMessage(ex, "Error Sending Mc URL to Cisco Ui", this);
                Debug.LogMessage(LogEventLevel.Debug, "Error Sending Mc URL to Cisco Ui: {Message}", this, ex.Message);
            }


        }

        /// <summary>
        /// Send the cisco ui to a webview with mc app url + path using the webViewConfig
        /// </summary>
        /// <param name="mcPath"></param>
        /// <param name="webViewConfig"></param>
        public void SendCiscoCodecUiToWebViewMcUrl(
            string mcPath,
            WebViewDisplayConfig webViewConfig, bool prependmcUrl = true

        )
        {
            Debug.LogMessage(LogEventLevel.Debug, "SendCiscoCodecUiToWebViewMcUrl: {McPath}, webViewConfig null: {WebViewConfigNull}, _McTouchPanelController: {McTpControllerNull}, AppUrlFeedback null: {AppUrlFeedbackNull}, appUrl: {AppUrl}", this, mcPath, webViewConfig == null, mcTpController == null, mcTpController?.AppUrlFeedback == null, mcTpController?.AppUrlFeedback?.StringValue);
            // Parse the _appUrl into a Uri object
            var (url, printableUrl) = prependmcUrl ? GetMobileControlUrl(mcPath, webViewConfig) : (mcPath, mcPath);


            Debug.LogMessage(LogEventLevel.Debug, "[MobileControlClickedEvent] Sending Mobile Control URL: {Url}", this, printableUrl);

            extensionsHandler.UiWebViewDisplayAction?.Invoke(
                new WebViewDisplayActionArgs()
                {
                    Title =
                        webViewConfig.Title != null
                            ? webViewConfig.Title
                            : defaultUiWebViewDisplayConfig.Title,
                    Url = url,
                    Target =
                        webViewConfig.Target != null
                            ? webViewConfig.Target
                            : defaultUiWebViewDisplayConfig.Target,
                    Mode =
                        webViewConfig.Mode != null
                            ? webViewConfig.Mode
                            : defaultUiWebViewDisplayConfig.Mode
                }
            );
        }

        private (string, string) GetMobileControlUrl(string mcPath, WebViewDisplayConfig webViewConfig)
        {
            var appUrl = mcTpController.AppUrlFeedback.StringValue;
            if (appUrl == null)
            {
                Debug.LogMessage(LogEventLevel.Debug, "AppUrl is null, cannot send to WebView", this);
                return (string.Empty, string.Empty);
            }
            //var printableAppUrl = _mcTpController?.AppUrlFeedback?.StringValue?.MaskQParamTokenInUrl();
            //Debug.LogMessage(
            //    LogEventLevel.Debug,
            //    $"SendCiscoCodecUiToWebViewMcUrl: {printableAppUrl}",
            //    this
            //);

            var uriBuilder = new UriBuilder(appUrl);

            //check for qparams
            var qparams = webViewConfig.QueryParams;
            if (qparams != null)
            {
                var parameters = HttpUtility.ParseQueryString(uriBuilder.Query);
                foreach (var item in qparams)
                {
                    parameters.Add(item.Key, item.Value);
                }
                uriBuilder.Query = parameters.ToString();
            }

            // Append suffix (i.e: "/lockout") to the path
            uriBuilder.Path = uriBuilder.Path.TrimEnd('/') + mcPath;

            // Get the final URL
            return (uriBuilder.ToString(), uriBuilder.ToString().MaskQParamTokenInUrl());
        }

        /// <summary>
        /// Send the cisco ui to a webview with url
        /// </summary>
        /// <param name="mcPath"></param>
        /// <param name="webViewConfig"></param>
        public void SendCiscoCodecUiToWebViewUrl(string url, WebViewDisplayConfig webViewConfig)
        {
            var uriBuilder = new UriBuilder(url);
            var urlToUse = uriBuilder.ToString();

            Debug.LogMessage(LogEventLevel.Debug, "[MobileControlClickedEvent] Sending URL: {Url}", this, urlToUse);

            extensionsHandler.UiWebViewDisplayAction?.Invoke(
                new WebViewDisplayActionArgs()
                {
                    Title = webViewConfig.Title ?? defaultUiWebViewDisplayConfig.Title,
                    Url = urlToUse,
                    Target = webViewConfig.Target ?? defaultUiWebViewDisplayConfig.Target,
                    Mode = webViewConfig.Mode ?? defaultUiWebViewDisplayConfig.Mode
                }
            );
        }

        public void ClearCiscoCodecUiWebViewController()
        {
            Debug.LogMessage(LogEventLevel.Debug, "ClearCiscoCodecUiWebViewController", this);
            extensionsHandler?.UiWebViewClearAction?.Invoke(
                new WebViewDisplayClearActionArgs() { Target = "Controller" }
            );
        }

        public void ClearCiscoCodecUiWebViewOsd()
        {
            Debug.LogMessage(LogEventLevel.Debug, "ClearCiscoCodecUiWebViewOsd", this);
            extensionsHandler?.UiWebViewClearAction?.Invoke(
                new WebViewDisplayClearActionArgs() { Target = "OSD" }
            );
        }
    }
}
