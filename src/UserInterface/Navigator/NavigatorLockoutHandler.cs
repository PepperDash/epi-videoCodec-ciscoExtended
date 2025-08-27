using System;
using System.Collections.Generic;

using System.Linq;
using System.Timers;
using Crestron.SimplSharp.Net;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Config;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.RoomCombiner;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.WebView;
using Serilog.Events;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Navigator
{
    internal class NavigatorLockoutHandler : IKeyed
    {
        private NavigatorController mcTpController;

        private ExtensionsHandler extensionsHandler;

        private RoomCombinerHandler combinerHandler;

        private readonly NavigatorConfig props;

        public string Key { get; }

        private readonly Timer lockoutPollTimer;

        private string defaultRoomKey;
        private string primaryRoomKey;

        private Lockout currentLockout;

        private bool combinationLockout;

        private readonly WebViewDisplayConfig defaultUiWebViewDisplayConfig = new WebViewDisplayConfig()
        {
            Title = "Mobile Control",
            Target = "Controller",
            Mode = "Modal"
        };

        internal NavigatorLockoutHandler(
            NavigatorController ui,
            NavigatorConfig props
        )
        {
            this.props = props;
            mcTpController = ui;

            Key = ui.Key + "-NavigatorLockout";

            lockoutPollTimer = new Timer(
                                      this.props?.Lockout?.PollIntervalMs > 0 ? this.props.Lockout.PollIntervalMs : 5000
                                  )
            {
                Enabled = false,
                AutoReset = true
            };

            lockoutPollTimer.Elapsed += (s, a) =>
            {
                this.LogVerbose("Lockout Poll Timer Elapsed");
                if (!mcTpController.LockedOut)
                {
                    this.LogVerbose("_mcTpController.LockedOut: {LockedOut}", mcTpController.LockedOut);
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
                this.LogDebug("Error: parent navigator controller is null");
                return;
            }

            mcTpController = parent;

            extensionsHandler = parent.UiExtensionsHandler;

            combinerHandler = parent.RoomCombinerHandler;

            if (extensionsHandler == null)
            {
                this.LogDebug("[Warning]: VideoCodecUiExtensionsHandler is null. Skipping VideoCodecMobileControlRouter Subscriptions");
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
                combinerHandler.EssentialsRoomCombiner.RoomCombinationScenarioChanged += HandleRoomCombineScenarioChanged;
            }

            extensionsHandler.UiExtensionsClickedEvent +=
                VideoCodecUiExtensionsClickedMcEventHandler;

            defaultRoomKey = mcTpController?.DefaultRoomKey;

            SetupCustomLockouts();
        }

        private void SetupCustomLockouts()
        {
            if (props.CustomLockouts == null)
            {
                return;
            }

            foreach (var lockout in props.CustomLockouts)
            {
                if (!(DeviceManager.GetDeviceForKey(lockout.DeviceKey) is IHasFeedback feedbackProvider))
                {
                    this.LogDebug("No feedback provider found for device key: {DeviceKey}", lockout.DeviceKey);
                    continue;
                }

                // Setup lockout for feedback provider
                if (!(feedbackProvider.Feedbacks[lockout.FeedbackKey] is BoolFeedback feedback))
                {
                    this.LogDebug("No BoolFeedback found for key: {FeedbackKey} on device: {DeviceKey}", lockout.FeedbackKey, lockout.DeviceKey);
                    continue;
                }

                // Setup lockout for feedback
                feedback.OutputChange += (s, a) =>
                        {
                            // skip this lockout update if the current lockout is a combination lockout
                            if (combinationLockout)
                            {
                                return;
                            }

                            if ((a.BoolValue && !lockout.LockOnFalse) || (!a.BoolValue && lockout.LockOnFalse))
                            {
                                currentLockout = lockout;

                                StartLockout();
                            }
                            else
                            {
                                CancelLockoutTimer();
                            }
                        };
            }
        }

        private void HandleRoomCombineScenarioChanged(object sender = null, EventArgs e = null)
        {
            try
            {
                var combiner = combinerHandler.EssentialsRoomCombiner;
                var currentScenario = combiner.CurrentScenario;
                var uiMap = currentScenario.UiMap;

                if (uiMap == null)
                {
                    this.LogDebug("uiMap is null");
                    return;
                }

                if (!uiMap.TryGetValue(defaultRoomKey, out var currentScenarioRoomKey))
                {
                    this.LogDebug("[ERROR] UiMap default room key: {DefaultRoomKey} Error: UiMap must have an entry keyed to default room key with value of room connection for room state {ScenarioKey} or lockout", defaultRoomKey, currentScenario.Key);
                    return;
                }

                if (!uiMap.TryGetValue("primary", out primaryRoomKey))
                {
                    this.LogDebug("Primary room key not found in UiMap for scenario: {ScenarioKey}", currentScenario.Key);
                }

                if (currentScenarioRoomKey != "lockout")
                {
                    CancelLockoutTimer();
                    this.LogDebug("ui with default room key {DefaultRoomKey} is not locked out", defaultRoomKey);
                }

                this.LogDebug("UiMap default room key {DefaultRoomKey} is in lockout state", defaultRoomKey);

                currentLockout = props?.Lockout;

                StartLockout();
            }
            catch (Exception ex)
            {
                this.LogDebug("Error in Combiner_RoomCombinationScenarioChanged_Lockout_EventHandler", ex);
            }
        }

        private void StartLockout()
        {
            mcTpController.LockedOut = true;

            combinationLockout = true;

            ClearWebView();

            extensionsHandler.UiWebViewChangedEvent += LockoutWebViewChanged;

            mcTpController.Parent.EnqueueCommand(WebViewDisplay.xCommandStatus());

            if (!mcTpController.EnableLockoutPoll)
            {
                return;
            }

            // Start the timer when lockout occurs                      
            lockoutPollTimer.Start();
        }

        private void CancelLockoutTimer()
        {
            this.LogVerbose("Canceling Lockout Poll Timer for: {Key}", mcTpController.Key);

            extensionsHandler.UiWebViewChangedEvent -= LockoutWebViewChanged;

            mcTpController.LockedOut = false;

            combinationLockout = false;

            ClearWebView();

            lockoutPollTimer.Stop();
        }

        public void LockoutWebViewChanged(object sender, WebViewChangedEventArgs args)
        {
            bool isError = args.UiWebViewStatus.IsError;

            // Case 1: No error AND not locked out → Clear web view
            if (!isError && !mcTpController.LockedOut)
            {
                WebView.WebView uiWebView = args.UiWebViewStatus.UiWebView;

                extensionsHandler.UiWebViewClearAction?.Invoke(
                    new WebViewDisplayClearActionArgs() { Target = "Controller" }
                );

                return;
            }

            // Case 2: No error AND locked out → Do nothing
            if (!isError && mcTpController.LockedOut)
            {
                return;
            }

            // Case 3: Error (regardless of lockout state) → Log error
            this.LogDebug("Error in UiWebViewChangedEventHandler.  XPath: {XPath}Reason: {Reason}", args.UiWebViewStatus.ErrorStatus.XPath.Value, args.UiWebViewStatus.ErrorStatus.Reason.Value);

            // Case 4: Error AND not locked out → Do nothing (just logged)
            if (!mcTpController.LockedOut)
            {
                return;
            }

            // Case 5: Error AND locked out → Send lockout
            SendLockout(defaultRoomKey, primaryRoomKey);

            return;
        }


        private void SendLockout(string thisUisDefaultRoomKey, string primaryRoomKey)
        {
            this.LogDebug("UiMap default room key: {DefaultRoomKey} is in lockout state", thisUisDefaultRoomKey);

            var path = currentLockout?.MobileControlPath;

            if (path == null || path.Length == 0)
                path = "/lockout";

            var webViewConfig =
                currentLockout?.UiWebViewDisplay == null
                    ? defaultUiWebViewDisplayConfig
                    : currentLockout.UiWebViewDisplay;

            if (!string.IsNullOrEmpty(primaryRoomKey))
            {
                if (webViewConfig.QueryParams == null)
                {
                    webViewConfig.QueryParams = new Dictionary<string, string>();
                }

                webViewConfig.QueryParams["primaryRoomName"] =
                            DeviceManager.GetDeviceForKey(primaryRoomKey) is IKeyName room ? room.Name : primaryRoomKey;
            }

            SendWebViewMcUrl(path, webViewConfig);
        }

        private async void VideoCodecUiExtensionsClickedMcEventHandler(
            object sender,
            UiExtensionsClickedEventArgs e
        )
        {
            this.LogDebug("VideoCodecUiExtensionsClickedMcEventHandler: {Id}", e.Id);
            try
            {
                //navigator button click build url and use VideoCodecUiExtensionsHandler action to send to mobile control
                var panelId = e.Id;
                var extensions = props.Extensions;
                if (extensions == null || !extensions.Panels.Any())
                {
                    this.LogDebug("No extensions found for VideoCodecMobileControlRouter");
                    return;
                }
                var panels = extensions.Panels;
                var mcPanel = panels.Find((pp) => pp.PanelId == panelId);
                if (mcPanel == null)
                {
                    this.LogDebug("Panel not found for VideoCodecMobileControlRouter");
                    return;
                }
                if (mcPanel.DeviceActions != null && mcPanel.DeviceActions.Count > 0)
                {
                    foreach (DeviceActionWrapper action in mcPanel.DeviceActions)
                    {
                        if (action == null)
                        {
                            this.LogDebug("DeviceAction is null");
                            continue;
                        }
                        this.LogDebug("Running DeviceAction {MethodName}", action.MethodName);
                        await DeviceJsonApi.DoDeviceActionAsync(action);
                    }
                }

                if (!string.IsNullOrEmpty(mcPanel.Url))
                {
                    this.LogDebug("Sending URL to WebView: {Url}", mcPanel.Url);

                    foreach (WebViewDisplayConfig webView in mcPanel.UiWebViewDisplays)
                    {
                        SendWebViewUrl(mcPanel.Url, webView);
                    }

                    return;
                }

                if (mcPanel.MobileControlPath == null || mcPanel.MobileControlPath.Length == 0)
                {
                    this.LogDebug("MobileControlPath not found for {PanelName}", mcPanel.Name);
                    return;
                }
                if (mcPanel.UiWebViewDisplays == null)
                {
                    this.LogDebug("[Warning] UiWebViewDisplay not found for {PanelName} using default Title: {Title}, Mode: {Mode}, Target: {Target}", mcPanel.Name, defaultUiWebViewDisplayConfig.Title, defaultUiWebViewDisplayConfig.Mode, defaultUiWebViewDisplayConfig.Target);
                }

                foreach (WebViewDisplayConfig webView in mcPanel.UiWebViewDisplays)
                {
                    SendWebViewMcUrl(mcPanel.MobileControlPath, webView);
                }
            }
            catch (Exception ex)
            {
                this.LogDebug("Error Sending Mc URL to Cisco Ui: {Message}", ex.Message);
                this.LogVerbose(ex, "Error Sending Mc URL to Cisco Ui");
            }
        }

        /// <summary>
        /// Send the cisco ui to a webview with mc app url + path using the webViewConfig
        /// </summary>
        /// <param name="mcPath"></param>
        /// <param name="webViewConfig"></param>
        public void SendWebViewMcUrl(
            string mcPath,
            WebViewDisplayConfig webViewConfig, bool prependmcUrl = true

        )
        {
            this.LogDebug("SendCiscoCodecUiToWebViewMcUrl: {McPath}, webViewConfig null: {WebViewConfigNull}, _McTouchPanelController: {McTpControllerNull}, AppUrlFeedback null: {AppUrlFeedbackNull}, appUrl: {AppUrl}", mcPath, webViewConfig == null, mcTpController == null, mcTpController?.AppUrlFeedback == null, mcTpController?.AppUrlFeedback?.StringValue);
            // Parse the _appUrl into a Uri object
            var (url, printableUrl) = prependmcUrl ? GetMobileControlUrl(mcPath, webViewConfig) : (mcPath, mcPath);


            this.LogDebug("[MobileControlClickedEvent] Sending Mobile Control URL: {Url}", printableUrl);

            extensionsHandler.UiWebViewDisplayAction?.Invoke(
                new WebViewDisplayActionArgs()
                {
                    Title =
                        webViewConfig.Title ?? defaultUiWebViewDisplayConfig.Title,
                    Url = url,
                    Target =
                        webViewConfig.Target ?? defaultUiWebViewDisplayConfig.Target,
                    Mode =
                        webViewConfig.Mode ?? defaultUiWebViewDisplayConfig.Mode
                }
            );
        }

        private (string, string) GetMobileControlUrl(string mcPath, WebViewDisplayConfig webViewConfig)
        {
            var appUrl = mcTpController.AppUrlFeedback.StringValue;
            if (appUrl == null)
            {
                this.LogDebug("AppUrl is null, cannot send to WebView", this);
                return (string.Empty, string.Empty);
            }

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
        public void SendWebViewUrl(string url, WebViewDisplayConfig webViewConfig)
        {
            var uriBuilder = new UriBuilder(url);
            var urlToUse = uriBuilder.ToString();

            this.LogDebug("[MobileControlClickedEvent] Sending URL: {Url}", urlToUse);

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

        public void ClearWebView()
        {
            extensionsHandler?.UiWebViewClearAction?.Invoke(
                new WebViewDisplayClearActionArgs() { Target = "Controller" }
            );
        }

        public void ClearWebViewOsd()
        {
            extensionsHandler?.UiWebViewClearAction?.Invoke(
                new WebViewDisplayClearActionArgs() { Target = "OSD" }
            );
        }
    }
}
