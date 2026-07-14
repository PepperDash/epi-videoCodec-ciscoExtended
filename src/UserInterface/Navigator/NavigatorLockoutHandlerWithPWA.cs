using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Timers;
using Crestron.SimplSharp.Net;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.VideoCodec;
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
    internal class NavigatorLockoutHandlerWithPWA : IKeyed, INavigatorLockoutHandlerWithPwa
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

        private System.Timers.Timer exitPwaModeTimer;

        private bool inManualPwaMode;

        private bool inProgressWebViewActive;

        private EventHandler<EventArgs> _combinationOperationStatusChangedHandler;

        // How long to keep the in-progress webview open after a Failed/TimedOut result so the
        // React app can display its failure/timeout message. Matches the React app default
        // (combinationOperationFailureDisplayMs).
        private const int CombinationFailureDisplayHoldMs = 4000;

        private System.Timers.Timer inProgressFailureCloseTimer;

        private const int InProgressPollIntervalMs = 5000;
        private System.Timers.Timer inProgressPollTimer;

        private readonly Dictionary<string, (BoolFeedback Feedback, EventHandler<FeedbackEventArgs> Handler)> _lockoutFeedbackHandlers =
            new Dictionary<string, (BoolFeedback, EventHandler<FeedbackEventArgs>)>();

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

            exitPwaModeTimer = new System.Timers.Timer(500)
            {
                AutoReset = false
            };
            exitPwaModeTimer.Elapsed += (s, e) =>
            {
                inManualPwaMode = false;
                this.LogDebug("Exiting PWA mode and returning to default UI");
                SetPeripheralMode(ePeripheralMode.Controller);
                exitPwaModeTimer.Stop();
            };
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

            extensionsHandler = parent.Parent?.UiExtensionsHandler ?? parent.UiExtensionsHandler;

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

            extensionsHandler.UiExtensionsClickedEvent +=
                VideoCodecUiExtensionsClickedMcEventHandler;

            defaultRoomKey = mcTpController.DefaultRoomKey;

            if (combinerHandler.EssentialsRoomCombiner != null)
            {
                //subscribe to events for routing buttons from codec ui to mobile control
                combinerHandler.EssentialsRoomCombiner.RoomCombinationScenarioChanged += HandleRoomCombineScenarioChanged;
                TrySubscribeToCombinationOperationStatusChanged(combinerHandler.EssentialsRoomCombiner);
            }
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
                var handlerKey = $"{lockout.DeviceKey}:{lockout.FeedbackKey}";

                // Reliably unsubscribe any previously registered handler for this lockout using the tracked delegate
                if (_lockoutFeedbackHandlers.TryGetValue(handlerKey, out var existingSubscription))
                {
                    this.LogDebug("Unsubscribing from old feedback {feedbackKey} for lockout: {handlerKey}", lockout.FeedbackKey, handlerKey);
                    existingSubscription.Feedback.OutputChange -= existingSubscription.Handler;
                    _lockoutFeedbackHandlers.Remove(handlerKey);
                }

                if (deviceKey == defaultRoomKey && currentScenarioRoomKey != defaultRoomKey)
                {
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

                // Create a named handler delegate so it can be tracked and reliably unsubscribed later
                EventHandler<FeedbackEventArgs> handler = (s, a) => HandleLockout(lockout, a);
                feedback.OutputChange += handler;
                _lockoutFeedbackHandlers[handlerKey] = (feedback, handler);
            }
        }

        private void HandleLockout(Lockout lockout, FeedbackEventArgs a)
        {

            this.LogInformation("Handling lockout feedback change. DeviceKey: {DeviceKey}, FeedbackKey: {FeedbackKey}, Value: {Value}", lockout.DeviceKey, lockout.FeedbackKey, a.BoolValue);

            this.LogDebug("Custom lockout feedback changed. DeviceKey: {DeviceKey}, FeedbackKey: {FeedbackKey}, Value: {Value}", lockout.DeviceKey, lockout.FeedbackKey, a.BoolValue);
            // skip this lockout update if the current lockout is a combination lockout
            if (combinationLockout)
            {
                this.LogDebug("Skipping custom lockout update because currently in combination lockout mode");
                return;
            }

            if (currentLockout != null && (currentLockout.Priority > lockout.Priority))
            {
                this.LogDebug("Skipping custom lockout update because current lockout has higher priority. Current Lockout DeviceKey: {CurrentLockoutDeviceKey}, FeedbackKey: {CurrentLockoutFeedbackKey}, Priority: {CurrentLockoutPriority}. New Lockout DeviceKey: {NewLockoutDeviceKey}, FeedbackKey: {NewLockoutFeedbackKey}, Priority: {NewLockoutPriority}", currentLockout.DeviceKey, currentLockout.FeedbackKey, currentLockout.Priority, lockout.DeviceKey, lockout.FeedbackKey, lockout.Priority);
                return;
            }
            else
            {
                this.LogDebug("Updating current lockout to new lockout. New Lockout DeviceKey: {NewLockoutDeviceKey}, FeedbackKey: {NewLockoutFeedbackKey}, Priority: {NewLockoutPriority}", lockout.DeviceKey, lockout.FeedbackKey, lockout.Priority);
            }

            // if (currentLockout?.MobileControlPath != lockout.MobileControlPath && mcTpController.LockedOut)
            // {
            //     this.LogDebug("Skipping custom lockout update because currently in other lockout mode. Path: {path}", currentLockout?.MobileControlPath);
            //     return;
            // }

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

                // While a combination operation is in progress, do not change lockout state.
                // Instead (re)assert the in-progress webview so the React "combination in
                // progress" overlay stays visible. This event fires AFTER the combiner runs the
                // outgoing scenario's deactivation actions (e.g. CloseWebViewController) which
                // clear the Controller webview, so re-opening here keeps the overlay in place.
                if (IsCombinationOperationInProgress())
                {
                    this.LogDebug("Combination operation in progress; (re)asserting in-progress webview for {DefaultRoomKey}", defaultRoomKey);
                    OpenInProgressWebView(reassert: true);
                    return;
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

        private void HandleCombinationOperationStatusChanged(object sender, EventArgs e)
        {
            if (IsCombinationOperationInProgress())
            {
                // Combination change is underway: just open the webview so the React
                // "in progress" overlay is visible. Do NOT force lockout on this panel.
                CancelInProgressFailureCloseTimer();
                OpenInProgressWebView();
                return;
            }

            if (IsCombinationOperationFailedOrTimedOut())
            {
                // Keep the webview open long enough for the React app to display its
                // failure/timeout message for the full duration, then close and apply the
                // normal scenario-driven UI.
                this.LogDebug("Combination operation failed/timed out; holding in-progress webview {HoldMs}ms so the message displays for {DefaultRoomKey}", CombinationFailureDisplayHoldMs, defaultRoomKey);
                ScheduleInProgressFailureClose();
                return;
            }

            // Completed / Idle: close the in-progress webview if we opened it, then apply
            // the normal, scenario-driven lockout state (unchanged behavior).
            CancelInProgressFailureCloseTimer();
            CloseInProgressWebView();
            HandleRoomCombineScenarioChanged();
        }

        private void ScheduleInProgressFailureClose()
        {
            CancelInProgressFailureCloseTimer();

            inProgressFailureCloseTimer = new System.Timers.Timer(CombinationFailureDisplayHoldMs) { AutoReset = false };
            inProgressFailureCloseTimer.Elapsed += (s, a) =>
            {
                CancelInProgressFailureCloseTimer();
                CloseInProgressWebView();
                HandleRoomCombineScenarioChanged();
            };
            inProgressFailureCloseTimer.Start();
        }

        private void StartInProgressPollTimer()
        {
            StopInProgressPollTimer();
            inProgressPollTimer = new System.Timers.Timer(InProgressPollIntervalMs) { AutoReset = true };
            inProgressPollTimer.Elapsed += (s, a) =>
            {
                if (!IsCombinationOperationInProgress())
                {
                    StopInProgressPollTimer();
                    return;
                }
                this.LogVerbose("In-progress poll: re-asserting webview for {DefaultRoomKey}", defaultRoomKey);
                OpenInProgressWebView(reassert: true);
            };
            inProgressPollTimer.Start();
        }

        private void StopInProgressPollTimer()
        {
            if (inProgressPollTimer == null) return;
            inProgressPollTimer.Stop();
            inProgressPollTimer.Dispose();
            inProgressPollTimer = null;
        }

        private void CancelInProgressFailureCloseTimer()
        {
            if (inProgressFailureCloseTimer == null)
            {
                return;
            }

            inProgressFailureCloseTimer.Stop();
            inProgressFailureCloseTimer.Dispose();
            inProgressFailureCloseTimer = null;
        }

        private void TrySubscribeToCombinationOperationStatusChanged(object combiner)
        {
            try
            {
                var eventInfo = combiner?.GetType().GetEvent("CombinationOperationStatusChanged", BindingFlags.Instance | BindingFlags.Public);
                if (eventInfo == null || eventInfo.EventHandlerType != typeof(EventHandler<EventArgs>))
                {
                    this.LogDebug("CombinationOperationStatusChanged event not available for subscription");
                    return;
                }

                _combinationOperationStatusChangedHandler = HandleCombinationOperationStatusChanged;
                eventInfo.AddEventHandler(combiner, _combinationOperationStatusChangedHandler);
                this.LogDebug("Subscribed to CombinationOperationStatusChanged");
            }
            catch (Exception ex)
            {
                this.LogDebug("Failed to subscribe to CombinationOperationStatusChanged: {message}", ex.Message);
            }
        }

        private bool IsCombinationOperationInProgress()
        {
            var combiner = combinerHandler?.EssentialsRoomCombiner;
            if (combiner == null)
            {
                return false;
            }

            var operationStatus = combiner.GetType().GetProperty("CombinationOperation", BindingFlags.Instance | BindingFlags.Public)?.GetValue(combiner, null);
            if (operationStatus == null)
            {
                return false;
            }

            var stateValue = operationStatus.GetType().GetProperty("State", BindingFlags.Instance | BindingFlags.Public)?.GetValue(operationStatus, null);
            if (stateValue == null)
            {
                return false;
            }

            var stateText = stateValue.ToString();
            return string.Equals(stateText, "InProgress", StringComparison.OrdinalIgnoreCase) || string.Equals(stateText, "1", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsCombinationOperationFailedOrTimedOut()
        {
            var combiner = combinerHandler?.EssentialsRoomCombiner;
            if (combiner == null)
            {
                return false;
            }

            var operationStatus = combiner.GetType().GetProperty("CombinationOperation", BindingFlags.Instance | BindingFlags.Public)?.GetValue(combiner, null);
            if (operationStatus == null)
            {
                return false;
            }

            var stateValue = operationStatus.GetType().GetProperty("State", BindingFlags.Instance | BindingFlags.Public)?.GetValue(operationStatus, null);
            if (stateValue == null)
            {
                return false;
            }

            var stateText = stateValue.ToString();
            return string.Equals(stateText, "Failed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(stateText, "TimedOut", StringComparison.OrdinalIgnoreCase)
                || string.Equals(stateText, "3", StringComparison.OrdinalIgnoreCase)
                || string.Equals(stateText, "4", StringComparison.OrdinalIgnoreCase);
        }

        private void OpenInProgressWebView(bool reassert = false)
        {
            if (mcTpController.LockedOut)
            {
                // A real lockout already owns the webview; nothing to do.
                return;
            }

            if (inProgressWebViewActive && !reassert)
            {
                return;
            }

            var appUrl = mcTpController.AppUrlFeedback.StringValue;
            if (appUrl == null)
            {
                this.LogDebug("AppUrl is null, cannot open in-progress webview");
                return;
            }

            inProgressWebViewActive = true;
            this.LogDebug("Opening in-progress PWA (/lockout route) for {DefaultRoomKey}", defaultRoomKey);

            // Point the persistent web app at the /lockout route (NOT "/"). "/" redirects to
            // the Tech PIN gate, so a churn/flicker leaves the Tech PIN page showing. /lockout
            // is a defined, static route; the React "combination in progress" overlay still
            // renders on top of it while the combiner operation state is InProgress.
            var uriBuilder = new UriBuilder(appUrl)
            {
                Path = new UriBuilder(appUrl).Path.TrimEnd('/') + "/lockout"
            };

            SetPersistentWebAppUrl(uriBuilder.ToString());
            SetPeripheralMode(ePeripheralMode.PersistentWebApp);

            StartInProgressPollTimer();
        }

        private void CloseInProgressWebView()
        {
            if (!inProgressWebViewActive)
            {
                return;
            }

            inProgressWebViewActive = false;

            StopInProgressPollTimer();

            if (mcTpController.LockedOut || inManualPwaMode)
            {
                // Real lockout or manual PWA owns the panel now; leave it in place.
                return;
            }

            this.LogDebug("Closing in-progress PWA (app root) for {DefaultRoomKey}", defaultRoomKey);
            SetPeripheralMode(ePeripheralMode.Controller);
        }

        private void StartLockout(bool isCombinationLockout = true)
        {
            // clear manual mode
            inManualPwaMode = false;
            // Stop the timer if it's already running to prevent multiple rapid calls to ExitPwaMode
            exitPwaModeTimer.Stop();

            mcTpController.LockedOut = true;

            combinationLockout = isCombinationLockout;

            ClearWebView();

            SendLockout(defaultRoomKey, primaryRoomKey);
        }

        private void CancelLockout()
        {
            if (currentLockout != null)
            {
                currentLockout = null;
            }

            if (!mcTpController.LockedOut)
            {
                return;
            }

            this.LogDebug("UiMap default room key: {DefaultRoomKey} is exiting lockout state", defaultRoomKey);

            mcTpController.LockedOut = false;

            combinationLockout = false;

            inProgressWebViewActive = false;

            CancelInProgressFailureCloseTimer();

            StopInProgressPollTimer();

            if (inManualPwaMode)
            {
                this.LogDebug("Currently in manual PWA mode, not exiting to controller mode");
                return;
            }

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

                if (string.Equals(panelId, "catv", StringComparison.OrdinalIgnoreCase) && CodecIsInCall())
                {
                    this.LogInformation("Ignoring CATV panel click - codec is in a call");
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
                        var configParams = action.Params;

                        if (action.DeviceKey == defaultRoomKey && defaultRoomKey != currentScenarioRoomKey)
                        {
                            this.LogInformation("Sending action {ActionId} to primary room {PrimaryRoomId}", action.MethodName, currentScenarioRoomKey);
                            action.DeviceKey = currentScenarioRoomKey;
                            action.Params = GetScenarioAwareActionParams(action);
                        }

                        this.LogDebug("Running DeviceAction {MethodName} on device {key}", action.MethodName, action.DeviceKey);
                        await DeviceJsonApi.DoDeviceActionAsync(action);

                        this.LogInformation("Resetting action deviceKey to config value");
                        action.DeviceKey = configDeviceKey;
                        action.Params = configParams;
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

        private object[] GetScenarioAwareActionParams(DeviceActionWrapper action)
        {
            if (!string.Equals(action.MethodName, "RunRouteAction", StringComparison.OrdinalIgnoreCase)
                || action.Params == null
                || action.Params.Length < 2
                || currentScenarioRoomKey == LOCKOUT_SCENARIO_KEY
                || !(action.Params[1] is string sourceListKey)
                || !string.Equals(sourceListKey, defaultRoomKey, StringComparison.OrdinalIgnoreCase))
            {
                return action.Params;
            }

            var scenarioParams = (object[])action.Params.Clone();
            scenarioParams[1] = currentScenarioRoomKey;

            return scenarioParams;
        }

        private bool CodecIsInCall()
        {
            var ownCodec = mcTpController?.Parent;
            var inCall = ownCodec?.IsAnyCallActive ?? false;

            this.LogVerbose(
                "CodecIsInCall check: ownCodec={ownCodecKey} IsAnyCallActive={inCall}",
                ownCodec?.Key, inCall);

            return inCall;
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
            inManualPwaMode = true;

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
            this.LogDebug("********* ExitPwaMode called. Currently in manual PWA mode: {InManualPwaMode}", inManualPwaMode);

            if (!inManualPwaMode)
            {
                this.LogDebug("Not in manual PWA mode, ignoring ExitPwaMode call");
                return;
            }

            exitPwaModeTimer.Stop();
            exitPwaModeTimer.Start();
        }

        private void SetPersistentWebAppUrl(string url)
        {
            this.LogDebug("Setting Persistent Web App URL to: {url}", url);
            mcTpController.Parent.EnqueueCommand("xConfiguration UserInterface HomeScreen Peripherals WebApp URL: " + url + CiscoCodec.Delimiter);
        }

        private void SetPeripheralMode(ePeripheralMode mode)
        {
            if (mode == ePeripheralMode.Controller)
            {
                this.LogDebug("Setting peripheral mode to Controller");
            }
            else if (mode == ePeripheralMode.PersistentWebApp)
            {

                this.LogDebug("Setting peripheral mode to Persistent Web App");
            }

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
