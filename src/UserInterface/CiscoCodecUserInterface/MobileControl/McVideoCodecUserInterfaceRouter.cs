using Crestron.SimplSharp.Net;
using epi_videoCodec_ciscoExtended.UserInterface.CiscoCodecUserInterface.RoomCombiner;
using epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceExtensions;
using epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceWebViewDisplay;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace epi_videoCodec_ciscoExtended.UserInterface.CiscoCodecUserInterface.MobileControl
{
    internal class McVideoCodecUserInterfaceRouter : IKeyed
    {
        private McVideoCodecTouchpanelController _mcTpController;

        private ICiscoCodecUiExtensionsHandler _extensionsHandler;

        private IRoomCombinerHandler _combinerHandler;
        private IMobileControlRoomMessenger _bridge;

        private McVideoCodecUserInterfaceConfig _props { get; }

        public string Key { get; }

        private System.Timers.Timer _lockoutPollTimer;

        private string _thisUisDefaultRoomKey;
        private string _primaryRoomKey;

        private UiWebViewDisplayConfig _defaultUiWebViewDisplayConfig = new UiWebViewDisplayConfig()
        {
            Title = "Mobile Control",
            Target = "Controller",
            Mode = "Modal"
        };

        internal McVideoCodecUserInterfaceRouter(
            McVideoCodecTouchpanelController ui,
            IMobileControlRoomMessenger bridge,
            McVideoCodecUserInterfaceConfig props
        )
        {
            _props = props;
            _mcTpController = ui;
            _bridge = bridge;
            Key = ui.Key + "-McVcUiRouter";

            _lockoutPollTimer = new System.Timers.Timer(
                                _props?.Lockout?.PollIntervalMs > 0 ? _props.Lockout.PollIntervalMs : 5000
                            )
            {
                Enabled = false,
                AutoReset = true
            };

            _lockoutPollTimer.Elapsed += (s, a) =>
            {
                Debug.LogMessage(LogEventLevel.Verbose, "Lockout Poll Timer Elapsed", this);
                if (!_mcTpController.LockedOut)
                {
                    Debug.LogMessage(LogEventLevel.Verbose, $"_mcTpController.LockedOut: {_mcTpController.LockedOut}", this);
                    //if not in lockout state and was previously locked out
                    CancelLockoutTimer();
                    return;
                }
                _mcTpController.UisCiscoCodec.EnqueueCommand(UiWebViewDisplay.xCommandStatus());
            };
        }

        internal void Activate(McVideoCodecTouchpanelController ui)
        {
            Debug.LogMessage(
                LogEventLevel.Debug,
                $"Activating VideoCodecMobileControlRouter for {ui.Key}",
                this
            );
            //set private props after activation so everything is instantiated
            if (ui == null)
            {
                Debug.LogMessage(LogEventLevel.Debug, $"Error: {ui.Key} is null", this);
                return;
            }
            _mcTpController = ui;
            _extensionsHandler = ui.CiscoCodecUiExtensionsHandler;
            Debug.LogMessage(
                LogEventLevel.Debug,
                "VideoCodecUiExtensionsHandler null: {0}",
                this,
                ui.CiscoCodecUiExtensionsHandler == null
            );

            _combinerHandler = ui.RoomCombinerHandler;
            Debug.LogMessage(
                LogEventLevel.Debug,
                "EssentialsRoomCombiner null: {0}",
                this,
                ui.RoomCombinerHandler.EssentialsRoomCombiner == null
            );
            Debug.LogMessage(
                LogEventLevel.Debug,
                "MobileControlRoomBridge null: {0}",
                this,
                _bridge == null
            );

            if (_extensionsHandler == null)
            {
                Debug.LogMessage(
                    LogEventLevel.Debug,
                    "[Warning]: VideoCodecUiExtensionsHandler is null. Skipping VideoCodecMobileControlRouter Subscriptions",
                    this
                );
                return;
            }
            Debug.LogMessage(
                LogEventLevel.Debug,
                "VideoCodecMobileControlRouter Registering for VideoCodecUiExtensionsHandler.UiExtensionsClickedEvent",
                this
            );


            _mcTpController.UisCiscoCodec.IsReadyChange += (s, a) =>
            {
                if (!_mcTpController.UisCiscoCodec.IsReady) return;

                //send lockout if in lockout state
                HandleRoomCombineScenarioChanged();
            };

            if (_combinerHandler.EssentialsRoomCombiner != null)
            {
                //subscribe to events for routing buttons from codec ui to mobile control
                _combinerHandler.EssentialsRoomCombiner.RoomCombinationScenarioChanged +=
                    Combiner_RoomCombinationScenarioChanged_Lockout_EventHandler;
            }

            _extensionsHandler.UiExtensionsClickedEvent +=
                VideoCodecUiExtensionsClickedMcEventHandler;

            _thisUisDefaultRoomKey = _mcTpController?.DefaultRoomKey;

            Debug.LogMessage(
                LogEventLevel.Debug,
                "VideoCodecMobileControlRouter Registering for MobileControlRoomBridge.AppUrlChanged",
                this
            );

            Debug.LogMessage(
                LogEventLevel.Debug,
                "VideoCodecMobileControlRouter initialized for {0}",
                this,
                ui.Key
            );
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
                Debug.LogMessage(
                    LogEventLevel.Debug,
                    "Combiner_RoomCombinationScenarioChanged_Lockout_EventHandler",
                    this
                );

                var combiner = _combinerHandler?.EssentialsRoomCombiner;
                var curScenario = combiner?.CurrentScenario;
                var uimap = curScenario?.UiMap;
                if (uimap == null)
                {
                    Debug.LogMessage(LogEventLevel.Debug, "uimap is null", null, null);
                    return;
                }
                Debug.LogMessage(
                    LogEventLevel.Debug,
                    $"thisUisDefaultRoomKey: {_thisUisDefaultRoomKey}",
                    this
                );
                var thisUisUiMapRoomKeyValue = (
                    uimap?.FirstOrDefault((kv) => kv.Key == _thisUisDefaultRoomKey)
                )?.Value;
                Debug.LogMessage(
                    LogEventLevel.Debug,
                    $"thisUisUiMapRoomKeyValue: {thisUisUiMapRoomKeyValue}",
                    this
                );
                var getPrimaryKeySuccess = uimap?.TryGetValue("primary", out _primaryRoomKey);
                Debug.LogMessage(
                    LogEventLevel.Debug,
                    $"primaryRoomKey done: {_primaryRoomKey}",
                    this
                );

                if (!getPrimaryKeySuccess.GetValueOrDefault())
                {
                    Debug.LogMessage(
                        LogEventLevel.Debug,
                        $"Primary room key not found in UiMap for scenario: {curScenario.Key}",
                        this
                    );
                }
                if (thisUisUiMapRoomKeyValue == null)
                {
                    Debug.LogMessage(
                        LogEventLevel.Debug,
                        $"[ERROR] UiMap default room key: {_thisUisDefaultRoomKey} Error: UiMap must have an entry keyed to default room key with value of room connection for room state {curScenario.Key} or lockout",
                        this
                    );
                    return;
                }
                if (thisUisUiMapRoomKeyValue == "lockout")
                {
                    Debug.LogMessage(LogEventLevel.Debug, $"UiMap default room key {_thisUisDefaultRoomKey} is in lockout state", this);
                    _mcTpController.LockedOut = true;
                    //SendLockout(_thisUisDefaultRoomKey, _primaryRoomKey);
                    _extensionsHandler.UiWebViewChanagedEvent += LockoutUiWebViewChanagedEventHandler;
                    _mcTpController.UisCiscoCodec.EnqueueCommand(UiWebViewDisplay.xCommandStatus());
                    if (_mcTpController.EnableLockoutPoll)
                    {
                        // Start the timer when lockout occurs                      
                        _lockoutPollTimer.Start();
                        return;
                    }
                    return;
                }

                CancelLockoutTimer();
                Debug.LogMessage(
                    LogEventLevel.Debug,
                    $"ui with default room key {_thisUisDefaultRoomKey} is not locked out",
                    this
                );
            }
            catch (Exception ex)
            {
                Debug.LogMessage(
                    ex,
                    "Error in Combiner_RoomCombinationScenarioChanged_Lockout_EventHandler",
                    this
                );
            }
        }

        private void CancelLockoutTimer()
        {
            Debug.LogMessage(LogEventLevel.Verbose, $"Canceling Lockout Poll Timer for: {_mcTpController.Key}", this, _mcTpController.Key);
            _extensionsHandler.UiWebViewChanagedEvent -= LockoutUiWebViewChanagedEventHandler;
            _mcTpController.LockedOut = false;
            ClearCiscoCodecUiWebViewController();
            _lockoutPollTimer.Stop();
        }

        public void LockoutUiWebViewChanagedEventHandler(object sender, UiWebViewChanagedEventArgs args)
        {
            bool isError = args?.UiWebViewStatus?.IsError ?? false;
            if (isError) //isError means no webview open
            {
                Debug.LogMessage(
                    LogEventLevel.Debug,
                        $"Error in UiWebViewChanagedEventHandler.  XPath: {args?.UiWebViewStatus?.ErrorStatus?.XPath?.Value}" +
                        $"Reason: {args?.UiWebViewStatus?.ErrorStatus?.Reason?.Value}", this);

                //if web view not open and in lockout send lockout to web view
                if (_mcTpController.LockedOut == true)
                {
                    SendLockout(_thisUisDefaultRoomKey, _primaryRoomKey);
                }
                return;
            }

            if (_mcTpController.LockedOut == false)
            {
                UiWebView uiWebView = args?.UiWebViewStatus?.UiWebView;
                _extensionsHandler.UiWebViewClearAction?.Invoke(
                    new UiWebViewDisplayClearActionArgs() { Target = "Controller" }
                );
            }
        }

        private void SendLockout(string thisUisDefaultRoomKey, string primaryRoomKey)
        {
            Debug.LogMessage(
                        LogEventLevel.Debug,
                        $"UiMap default room key: {thisUisDefaultRoomKey} is in lockout state",
                        this
                    );
            var path = _props?.Lockout?.MobileControlPath;
            Debug.LogMessage(LogEventLevel.Debug, $"Lockout path: {path}", this);
            if (path == null || path.Length == 0)
                path = "/lockout";
            Debug.LogMessage(LogEventLevel.Debug, $"Lockout path: {path}", this);
            var webViewConfig =
                _props?.Lockout?.UiWebViewDisplay == null
                    ? _defaultUiWebViewDisplayConfig
                    : _props.Lockout.UiWebViewDisplay;
            if (!string.IsNullOrEmpty(primaryRoomKey))
            {
                var room = DeviceManager.GetDeviceForKey(primaryRoomKey) as IKeyName;
                Debug.LogMessage(LogEventLevel.Debug, $"room: {room?.Name}", this);
                if (webViewConfig.QueryParams == null)
                {
                    webViewConfig.QueryParams = new Dictionary<string, string>();
                }

                webViewConfig.QueryParams["primaryRoomName"] =
                    room != null ? room.Name : primaryRoomKey;
            }
            SendCiscoCodecUiToWebViewMcUrl(path, webViewConfig);
        }

        private void VideoCodecUiExtensionsClickedMcEventHandler(
            object sender,
            UiExtensionsClickedEventArgs e
        )
        {
            Debug.LogMessage(
                LogEventLevel.Debug,
                $"VideoCodecUiExtensionsClickedMcEventHandler: {e.Id}",
                this
            );
            try
            {
                //navigator button click build url and use VideoCodecUiExtensionsHandler action to send to mobile control
                var panelId = e.Id;
                var extensions = _props.Extensions;
                if (extensions == null || !extensions.Panels.Any())
                {
                    Debug.LogMessage(
                        LogEventLevel.Debug,
                        "No extensions found for VideoCodecMobileControlRouter",
                        _mcTpController
                    );
                    return;
                }
                var panels = extensions.Panels;
                var mcPanel = panels.Find((pp) => pp.PanelId == panelId);
                if (mcPanel == null)
                {
                    Debug.LogMessage(
                        LogEventLevel.Debug,
                        "Panel not found for VideoCodecMobileControlRouter",
                        this
                    );
                    return;
                }
                if (mcPanel.MobileControlPath == null || mcPanel.MobileControlPath.Length == 0)
                {
                    Debug.LogMessage(
                        LogEventLevel.Debug,
                        $"MobileControlPath not found for {mcPanel.Name}",
                        this
                    );
                    return;
                }
                if (mcPanel.UiWebViewDisplay == null)
                {
                    Debug.LogMessage(
                        LogEventLevel.Debug,
                        $"[Warning] UiWebViewDisplay not found for {mcPanel.Name} using default Title: ${_defaultUiWebViewDisplayConfig.Title}, Mode: {_defaultUiWebViewDisplayConfig.Mode}, Target: {_defaultUiWebViewDisplayConfig}",
                        this
                    );
                }
                SendCiscoCodecUiToWebViewMcUrl(mcPanel.MobileControlPath, mcPanel.UiWebViewDisplay);
            }
            catch (Exception ex)
            {
                Debug.LogMessage(ex, "Error Sending Mc URL to Cisco Ui", this);
                Debug.LogMessage(
                    LogEventLevel.Debug,
                    $"Error Sending Mc URL to Cisco Ui: {ex.Message}",
                    this
                );
            }
        }

        /// <summary>
        /// Send the cisco ui to a webview with mc app url + path using the webViewConfig
        /// </summary>
        /// <param name="mcPath"></param>
        /// <param name="webViewConfig"></param>
        public void SendCiscoCodecUiToWebViewMcUrl(
            string mcPath,
            UiWebViewDisplayConfig webViewConfig
        )
        {
            Debug.LogMessage(
                LogEventLevel.Debug,
                $"SendCiscoCodecUiToWebViewMcUrl: {mcPath}, webViewConfig null: {webViewConfig == null}, "
                    + $"_McTouchPanelController: {_mcTpController == null}, "
                    + $"AppUrlFeedback null: {_mcTpController?.AppUrlFeedback == null}, "
                    + $"appUrl: {_mcTpController?.AppUrlFeedback?.StringValue}",
                this
            );
            // Parse the _appUrl into a Uri object
            var appUrl = _mcTpController.AppUrlFeedback.StringValue;
            if (appUrl == null)
            {
                Debug.LogMessage(
                    LogEventLevel.Debug,
                    "AppUrl is null, cannot send to WebView",
                    this
                );
                return;
            }
            //var printableAppUrl = _mcTpController?.AppUrlFeedback?.StringValue?.MaskQParamTokenInUrl();
            //Debug.LogMessage(
            //    LogEventLevel.Debug,
            //    $"SendCiscoCodecUiToWebViewMcUrl: {printableAppUrl}",
            //    this
            //);

            UriBuilder uriBuilder = new UriBuilder(appUrl);

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
            var url = uriBuilder.ToString();

            var printableUrl = uriBuilder.ToString().MaskQParamTokenInUrl();

            Debug.LogMessage(
                LogEventLevel.Debug,
                "[MobileControlClickedEvent] Sending Mobile Control URL: {0}",
                this,
                printableUrl
            );

            _extensionsHandler.UiWebViewDisplayAction?.Invoke(
                new UiWebViewDisplayActionArgs()
                {
                    Title =
                        webViewConfig.Title != null
                            ? webViewConfig.Title
                            : _defaultUiWebViewDisplayConfig.Title,
                    Url = url,
                    Target =
                        webViewConfig.Target != null
                            ? webViewConfig.Target
                            : _defaultUiWebViewDisplayConfig.Target,
                    Mode =
                        webViewConfig.Mode != null
                            ? webViewConfig.Mode
                            : _defaultUiWebViewDisplayConfig.Mode
                }
            );
        }

        public void ClearCiscoCodecUiWebViewController()
        {
            Debug.LogMessage(LogEventLevel.Debug, "ClearCiscoCodecUiWebViewController", this);
            _extensionsHandler?.UiWebViewClearAction?.Invoke(
                new UiWebViewDisplayClearActionArgs() { Target = "Controller" }
            );
        }

        public void ClearCiscoCodecUiWebViewOsd()
        {
            Debug.LogMessage(LogEventLevel.Debug, "ClearCiscoCodecUiWebViewOsd", this);
            _extensionsHandler?.UiWebViewClearAction?.Invoke(
                new UiWebViewDisplayClearActionArgs() { Target = "OSD" }
            );
        }
    }
}
