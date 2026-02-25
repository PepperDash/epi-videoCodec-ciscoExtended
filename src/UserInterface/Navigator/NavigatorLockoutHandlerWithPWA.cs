using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
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
    /// <summary>
    /// Peripheral Modes for Touch Panels
    /// </summary>
    public enum ePeripheralMode
    {
        Controller,
        PersistentWebApp
    }

    /// <summary>
    /// Handles Lockout Functionality with Persistent Web App for Navigator Touch Panels
    /// </summary>
    internal class NavigatorLockoutHandlerWithPWA : IKeyed, INavigatorLockoutHanderWithPwa
    {
        public const string LOCKOUT_SCENARIO_KEY = "lockout";
        private NavigatorController mcTpController;

        private ExtensionsHandler extensionsHandler;

        private RoomCombinerHandler combinerHandler;

        private readonly NavigatorConfig props;

        public string Key { get; }

        private string defaultRoomKey;

        private string primaryRoomKey;

        private string currentScenarioRoomKey;

        private Lockout currentLockout;

        private bool combinationLockout;

        private readonly WebViewDisplayConfig defaultUiWebViewDisplayConfig = new WebViewDisplayConfig()
        {
            Title = "Mobile Control",
            Target = "Controller",
            Mode = "Modal"
        };

        internal NavigatorLockoutHandlerWithPWA(
            NavigatorController ui,
            NavigatorConfig props
        )
        {
            this.props = props;
            mcTpController = ui;
            // Initialize defaultRoomKey from props, fallback to null or throw if not available
            defaultRoomKey = props?.DefaultRoomKey ?? null;
            currentScenarioRoomKey = defaultRoomKey;

            Key = ui.Key + "-NavigatorLockout";
        }

        public void Activate(NavigatorController parent)
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

            if (mcTpController.Parent.IsReady)
            {
                SetUpCodecCommands();
            }

            SetupCustomLockouts();

            mcTpController.Parent.IsReadyChange += (s, a) =>
            {
                if (!mcTpController.Parent.IsReady) return;

                SetUpCodecCommands();

                Thread.Sleep(1000);

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

            defaultRoomKey = mcTpController.DefaultRoomKey;
        }

        private void SetUpCodecCommands()
        {
            // Ensure touch panel is in controller mode on activation
            SetPeripheralMode(ePeripheralMode.Controller);


            // Possibly make this configurable later
            SetLedControlMode(true);

            SetPeripheralsProfileForTouchpanels();
        }

        private void SetLedControlMode(bool mode)
        {
            this.LogDebug("Setting Touch Panel LED Control Mode to: {mode}", mode);
            mcTpController.Parent.EnqueueCommand($"xConfiguration UserInterface LedControl Mode: {(mode ? "on" : "off")}{CiscoCodec.Delimiter}");
        }

        private void SetPeripheralsProfileForTouchpanels()
        {
            this.LogDebug("Setting Touch Panel Peripherals Profile to: NotSet");
            mcTpController.Parent.EnqueueCommand($"xConfiguration Peripherals Profile TouchPanels: NotSet{CiscoCodec.Delimiter}");
        }

        private void SetupCustomLockouts()
        {
            if (props.CustomLockouts == null)
            {
                return;
            }

            foreach (var lockout in props.CustomLockouts)
            {
                this.LogDebug("Setting up custom lockout for device key: {DeviceKey}, default room key: {defaultRoomKey} current scenario room key: {currentScenarioRoomKey}", lockout.DeviceKey, defaultRoomKey, currentScenarioRoomKey);

                var deviceKey = lockout.DeviceKey;

                if (deviceKey == defaultRoomKey && currentScenarioRoomKey != defaultRoomKey)
                {
                    if (DeviceManager.GetDeviceForKey(deviceKey) is IHasFeedback oldFeedbackProvider)
                    {
                        if (oldFeedbackProvider.Feedbacks[lockout.FeedbackKey] is BoolFeedback oldFeedback)
                        {
                            this.LogDebug("Unsubscribing from old feedback {feedbackKey} for roomKey: {roomKey}", lockout.FeedbackKey, deviceKey);

                            oldFeedback.OutputChange -= HandleLockoutFeedbackChange;
                        }
                        else
                        {
                            this.LogDebug("No BoolFeedback found for key: {FeedbackKey} on device: {DeviceKey}", lockout.FeedbackKey, deviceKey);
                        }
                    }
                    else
                    {
                        this.LogDebug("No feedback found for key: {FeedbackKey} on device: {DeviceKey}", lockout.FeedbackKey, deviceKey);
                    }

                    if (currentScenarioRoomKey == LOCKOUT_SCENARIO_KEY)
                    {
                        continue;
                    }
                    this.LogDebug("Using current scenario room key for custom lockout: {RoomKey}", currentScenarioRoomKey);
                    deviceKey = currentScenarioRoomKey;
                }

                this.LogDebug("Subscribing to feedback changes for device key: {DeviceKey}, feedback key: {FeedbackKey}", deviceKey, lockout.FeedbackKey);

                if (!(DeviceManager.GetDeviceForKey(deviceKey) is IHasFeedback feedbackProvider))
                {
                    this.LogDebug("No feedback provider found for device key: {DeviceKey}", deviceKey);
                    continue;
                }

                // Setup lockout for feedback provider
                if (!(feedbackProvider.Feedbacks[lockout.FeedbackKey] is BoolFeedback feedback))
                {
                    this.LogDebug("No BoolFeedback found for key: {FeedbackKey} on device: {DeviceKey}", lockout.FeedbackKey, deviceKey);
                    continue;
                }

                // Check initial feedback value
                if (feedback.BoolValue)
                {
                    this.LogDebug("Initial feedback value is true for device key: {DeviceKey}, feedback key: {FeedbackKey}", deviceKey, lockout.FeedbackKey);
                    HandleLockout(lockout, new FeedbackEventArgs(true));
                }

                void HandleLockoutFeedbackChange(object s, FeedbackEventArgs a)
                {
                    HandleLockout(lockout, a);
                }

                // Setup lockout for feedback
                feedback.OutputChange += HandleLockoutFeedbackChange;
            }
        }

        private void HandleLockout(Lockout lockout, FeedbackEventArgs a)
        {
            this.LogDebug("Custom lockout feedback changed. DeviceKey: {DeviceKey}, FeedbackKey: {FeedbackKey}, Value: {Value}", lockout.DeviceKey, lockout.FeedbackKey, a.BoolValue);
            // skip this lockout update if the current lockout is a combination lockout
            if (combinationLockout)
            {
                this.LogDebug("Skipping custom lockout update because currently in combination lockout or in other lockout mode");
                return;
            }

            if (currentLockout?.MobileControlPath != lockout.MobileControlPath && mcTpController.LockedOut)
            {
                this.LogDebug("Skipping custom lockout update because currently in other lockout mode. Path: {path}", currentLockout?.MobileControlPath);
                return;
            }

            if ((a.BoolValue && !lockout.LockOnFalse) || (!a.BoolValue && lockout.LockOnFalse))
            {
                this.LogDebug("Custom lockout activated. DeviceKey: {DeviceKey}, FeedbackKey: {FeedbackKey}, Value: {Value}", lockout.DeviceKey, lockout.FeedbackKey, a.BoolValue);
                currentLockout = lockout;

                StartLockout(false);
            }
            else
            {
                this.LogDebug("Custom lockout deactivated. DeviceKey: {DeviceKey}, FeedbackKey: {FeedbackKey}, Value: {Value}", lockout.DeviceKey, lockout.FeedbackKey, a.BoolValue);
                CancelLockout();
            }
        }

        private void HandleRoomCombineScenarioChanged(object sender = null, EventArgs e = null)
        {
            try
            {

                var combiner = combinerHandler.EssentialsRoomCombiner;
                if (combiner == null)
                {
                    this.LogDebug("EssentialsRoomCombiner is null in HandleRoomCombineScenarioChanged");
                    return;
                }
                var currentScenario = combiner.CurrentScenario;
                if (currentScenario == null)
                {
                    this.LogDebug("CurrentScenario is null in HandleRoomCombineScenarioChanged");
                    return;
                }

                var uiMap = currentScenario.UiMap;

                if (uiMap == null)
                {
                    this.LogDebug("uiMap is null");
                    return;
                }

                if (!uiMap.TryGetValue(defaultRoomKey, out currentScenarioRoomKey))
                {
                    this.LogDebug("[ERROR] UiMap default room key: {DefaultRoomKey} Error: UiMap must have an entry keyed to default room key with value of room connection for room state {ScenarioKey} or lockout", defaultRoomKey, currentScenario.Key);
                    return;
                }

                if (!uiMap.TryGetValue("primary", out primaryRoomKey))
                {
                    this.LogDebug("Primary room key not found in UiMap for scenario: {ScenarioKey}", currentScenario.Key);
                }

                if (currentScenarioRoomKey != LOCKOUT_SCENARIO_KEY)
                {
                    CancelLockout();
                    this.LogDebug("ui with default room key {DefaultRoomKey} is not locked out", defaultRoomKey);

                    SetupCustomLockouts();
                    return;
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

        private void StartLockout(bool isCombinationLockout = true)
        {
            mcTpController.LockedOut = true;

            combinationLockout = isCombinationLockout;

            ClearWebView();

            SendLockout(defaultRoomKey, primaryRoomKey);
        }

        private void CancelLockout()
        {
            if (!mcTpController.LockedOut)
            {
                return;
            }

            this.LogDebug("UiMap default room key: {DefaultRoomKey} is exiting lockout state", defaultRoomKey);

            mcTpController.LockedOut = false;

            combinationLockout = false;

            SetPeripheralMode(ePeripheralMode.Controller);
        }


        private void SendLockout(string thisUisDefaultRoomKey, string primRoomKey)
        {
            this.LogDebug("UiMap default room key: {DefaultRoomKey} is in lockout state", thisUisDefaultRoomKey);

            var path = currentLockout?.MobileControlPath;

            if (path == null || path.Length == 0)
                path = "/lockout";

            var webViewConfig =
                currentLockout?.UiWebViewDisplay == null
                    ? defaultUiWebViewDisplayConfig
                    : currentLockout.UiWebViewDisplay;

            if (!string.IsNullOrEmpty(primRoomKey))
            {
                if (webViewConfig.QueryParams == null)
                {
                    webViewConfig.QueryParams = new Dictionary<string, string>();
                }

                webViewConfig.QueryParams["primaryRoomName"] =
                            DeviceManager.GetDeviceForKey(primRoomKey) is IKeyName room ? room.Name : primRoomKey;
            }

            var appUrl = mcTpController.AppUrlFeedback.StringValue;

            if (appUrl == null)
            {
                this.LogDebug("AppUrl is null, cannot send to WebView", this);
                return;
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
            uriBuilder.Path = uriBuilder.Path.TrimEnd('/') + path;

            SetPersistentWebAppUrl(uriBuilder.ToString());

            SetPeripheralMode(ePeripheralMode.PersistentWebApp);
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

                        var configDeviceKey = action.DeviceKey;

                        if (action.DeviceKey == defaultRoomKey && defaultRoomKey != currentScenarioRoomKey)
                        {
                            this.LogInformation("Sending action {ActionId} to primary room {PrimaryRoomId}", action.MethodName, currentScenarioRoomKey);
                            action.DeviceKey = currentScenarioRoomKey;
                        }

                        this.LogDebug("Running DeviceAction {MethodName} on device {key}", action.MethodName, action.DeviceKey);
                        await DeviceJsonApi.DoDeviceActionAsync(action);

                        this.LogInformation("Resetting action deviceKey to config value");
                        action.DeviceKey = configDeviceKey;
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

        ///<inheritdoc />
        public void EnterPwaMode(string url, bool prependmcUrl = true)
        {
            this.LogDebug("Entering PWA mode with URL: {url}", url);
            var (finalUrl, printableUrl) = prependmcUrl ? GetMobileControlUrl(url, defaultUiWebViewDisplayConfig) : (url, url);
            this.LogDebug("Final URL for PWA mode: {finalUrl}", printableUrl);
            SetPersistentWebAppUrl(finalUrl);
            this.LogDebug("Entering PWA mode with URL: {url}", finalUrl);
            SetPeripheralMode(ePeripheralMode.PersistentWebApp);
        }

        ///<inheritdoc />
        public void ExitPwaMode()
        {
            this.LogDebug("Exiting PWA mode and returning to default UI");
            SetPeripheralMode(ePeripheralMode.Controller);
        }

        private void SetPersistentWebAppUrl(string url)
        {
            this.LogDebug("Setting Persistent Web App URL to: {url}", url);
            mcTpController.Parent.EnqueueCommand("xConfiguration UserInterface HomeScreen Peripherals WebApp URL: " + url + CiscoCodec.Delimiter);
        }

        private void SetPeripheralMode(ePeripheralMode mode)
        {
            var macAddress = props?.MacAddress;
            if (string.IsNullOrWhiteSpace(macAddress))
            {
                this.LogError("Cannot set peripheral mode {mode} because MacAddress is not configured or is empty.", mode);
                return;
            }
            this.LogDebug("Setting Touch Panel with MAC Address: {macAddress} to Mode: {mode}", macAddress, mode);
            mcTpController.Parent.EnqueueCommand($"xCommand Peripherals TouchPanel Configure ID: \"{macAddress}\" Mode: {mode}{CiscoCodec.Delimiter}");
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
