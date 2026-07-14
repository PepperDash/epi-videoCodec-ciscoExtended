using System;
using System.Collections.Generic;

using System.Linq;
using System.Reflection;
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
    /// Handles Lockout for Navigator with Modal WebView.  Uses timer to poll for lockout state if enabled.
    /// </summary>
    internal class NavigatorLockoutHandlerWithModal : IKeyed, INavigatorLockoutHandler
    {
        public const string LOCKOUT_SCENARIO_KEY = "lockout";
        private NavigatorController mcTpController;

        private ExtensionsHandler extensionsHandler;

        private RoomCombinerHandler combinerHandler;

        private readonly NavigatorConfig props;

        public string Key { get; }

        private readonly Timer lockoutPollTimer;

        private string defaultRoomKey;

        private string primaryRoomKey;

        private string currentScenarioRoomKey;

        private Lockout currentLockout;

        private bool combinationLockout;

        private bool inProgressWebViewActive;

        private EventHandler<EventArgs> _combinationOperationStatusChangedHandler;

        // How long to keep the in-progress webview open after a Failed/TimedOut result so the
        // React app can display its failure/timeout message. Matches the React app default
        // (combinationOperationFailureDisplayMs).
        private const int CombinationFailureDisplayHoldMs = 4000;

        private Timer inProgressFailureCloseTimer;

        // Polls every 5 s while a combination operation is in progress to re-assert the
        // in-progress webview in case the user dismisses it (X button).
        private const int InProgressPollIntervalMs = 5000;
        private Timer inProgressPollTimer;

        private readonly WebViewDisplayConfig defaultUiWebViewDisplayConfig = new WebViewDisplayConfig()
        {
            Title = "Mobile Control",
            Target = "Controller",
            Mode = "Modal"
        };

        internal NavigatorLockoutHandlerWithModal(
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

            SetupCustomLockouts();

            mcTpController.Parent.IsReadyChange += (s, a) =>
            {
                if (!mcTpController.Parent.IsReady) return;

                //send lockout if in lockout state
                HandleRoomCombineScenarioChanged();
            };

            extensionsHandler.UiExtensionsClickedEvent +=
                VideoCodecUiExtensionsClickedMcEventHandler;

            defaultRoomKey = mcTpController?.DefaultRoomKey;

            if (combinerHandler.EssentialsRoomCombiner != null)
            {
                //subscribe to events for routing buttons from codec ui to mobile control
                combinerHandler.EssentialsRoomCombiner.RoomCombinationScenarioChanged += HandleRoomCombineScenarioChanged;
                TrySubscribeToCombinationOperationStatusChanged(combinerHandler.EssentialsRoomCombiner);
            }
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
                CancelLockoutTimer();
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
                    CancelLockoutTimer();
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

            inProgressFailureCloseTimer = new Timer(CombinationFailureDisplayHoldMs) { AutoReset = false };
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
            inProgressPollTimer = new Timer(InProgressPollIntervalMs) { AutoReset = true };
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

            inProgressWebViewActive = true;
            this.LogDebug("Opening in-progress webview (/lockout route) for {DefaultRoomKey}", defaultRoomKey);

            // Open the mobile control app at the /lockout route (NOT "/"). "/" redirects to
            // the Tech PIN gate, so a churn/flicker leaves the Tech PIN page showing. /lockout
            // is a defined, static route; the React "combination in progress" overlay still
            // renders on top of it while the combiner operation state is InProgress.
            var inProgressConfig = new WebViewDisplayConfig
            {
                Title = "Room Combining",
                Mode = "Fullscreen",
                Target = "Controller"
            };
            SendWebViewMcUrl("/lockout", inProgressConfig, true);

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

            if (mcTpController.LockedOut)
            {
                // Real lockout owns the webview now; leave it in place.
                return;
            }

            this.LogDebug("Closing in-progress webview (app root) for {DefaultRoomKey}", defaultRoomKey);
            ClearWebView();
        }

        private void StartLockout(bool isCombinationLockout = true)
        {
            mcTpController.LockedOut = true;

            combinationLockout = isCombinationLockout;

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

            inProgressWebViewActive = false;

            CancelInProgressFailureCloseTimer();

            StopInProgressPollTimer();

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

            SendWebViewMcUrl(path, webViewConfig, true);
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

                    if (mcPanel.UiWebViewDisplays == null || !mcPanel.UiWebViewDisplays.Any())
                    {
                        this.LogDebug("[Warning] UiWebViewDisplays not found for {PanelName}; using default display config", mcPanel.Name);
                        SendWebViewUrl(mcPanel.Url, defaultUiWebViewDisplayConfig);
                    }
                    else
                    {
                        foreach (WebViewDisplayConfig webView in mcPanel.UiWebViewDisplays)
                        {
                            SendWebViewUrl(mcPanel.Url, webView);
                        }
                    }

                    return;
                }

                if (mcPanel.MobileControlPath == null || mcPanel.MobileControlPath.Length == 0)
                {
                    this.LogDebug("MobileControlPath not found for {PanelName}", mcPanel.Name);
                    return;
                }
                if (mcPanel.UiWebViewDisplays == null || !mcPanel.UiWebViewDisplays.Any())
                {
                    this.LogDebug("[Warning] UiWebViewDisplays not found for {PanelName} using default Title: {Title}, Mode: {Mode}, Target: {Target}", mcPanel.Name, defaultUiWebViewDisplayConfig.Title, defaultUiWebViewDisplayConfig.Mode, defaultUiWebViewDisplayConfig.Target);
                    SendWebViewMcUrl(mcPanel.MobileControlPath, defaultUiWebViewDisplayConfig);
                    return;
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