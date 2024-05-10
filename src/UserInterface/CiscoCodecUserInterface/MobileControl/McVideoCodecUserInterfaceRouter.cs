using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp.Net;
using epi_videoCodec_ciscoExtended.UserInterface.CiscoCodecUserInterface.RoomCombiner;
using epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceExtensions;
using epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceExtensions.Panels;
using epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceWebViewDisplay;
using Independentsoft.Json.Parser;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Devices.Common.VideoCodec.Interfaces;
using Serilog.Events;

namespace epi_videoCodec_ciscoExtended.UserInterface.CiscoCodecUserInterface.MobileControl
{
    internal class McVideoCodecUserInterfaceRouter : IKeyed
    {
        private McVideoCodecTouchpanelController _mcTpController;

        private IVideoCodecUiExtensionsHandler _extensionsHandler;

        private IRoomCombinerHandler _combinerHandler;
        private IMobileControlRoomMessenger _bridge;

        private McVideoCodecUserInterfaceConfig _props { get; }
        private McVideoCodecUserInterfaceConfig _props { get; }

        private Room _primaryRoom;

        public string Key { get; }

        private System.Timers.Timer lockoutTimer;

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
            _extensionsHandler = ui.VideoCodecUiExtensionsHandler;
            Debug.LogMessage(
                LogEventLevel.Debug,
                "VideoCodecUiExtensionsHandler null: {0}",
                this,
                ui.VideoCodecUiExtensionsHandler == null
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

            //subscribe to events for routing buttons from codec ui to mobile control
            _combinerHandler.EssentialsRoomCombiner.RoomCombinationScenarioChanged +=
                Combiner_RoomCombinationScenarioChanged_Lockout_EventHandler;
            _extensionsHandler.UiExtensionsClickedEvent +=
                VideoCodecUiExtensionsClickedMcEventHandler;

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
            try
            {
                Debug.LogMessage(
                    LogEventLevel.Debug,
                    "Combiner_RoomCombinationScenarioChanged_Lockout_EventHandler",
                    null,
                    null
                );

                var combiner = _combinerHandler?.EssentialsRoomCombiner;
                var curScenario = combiner?.CurrentScenario;
                var uimap = curScenario?.UiMap;
                if (uimap == null)
                {
                    Debug.LogMessage(LogEventLevel.Debug, "uimap is null", null, null);
                    return;
                }
                var thisUisDefaultRoomKey = _mcTpController?.DefaultRoomKey;
                Debug.LogMessage(
                    LogEventLevel.Debug,
                    $"thisUisDefaultRoomKey: {thisUisDefaultRoomKey}",
                    null,
                    null
                );
                var thisUisUiMapRoomKeyValue = (
                    uimap?.FirstOrDefault((kv) => kv.Key == thisUisDefaultRoomKey)
                )?.Value;
                Debug.LogMessage(
                    LogEventLevel.Debug,
                    $"thisUisUiMapRoomKeyValue: {thisUisUiMapRoomKeyValue}",
                    null,
                    null
                );
                var primaryRoomKey = "";
                var getPrimaryKeySuccess = uimap?.TryGetValue("primary", out primaryRoomKey);
                Debug.LogMessage(
                    LogEventLevel.Debug,
                    $"primaryRoomKey done: {primaryRoomKey}",
                    null,
                    null
                );

                if (!getPrimaryKeySuccess.GetValueOrDefault())
                {
                    Debug.LogMessage(
                        LogEventLevel.Debug,
                        $"Primary room key not found in UiMap for scenario: {curScenario.Key}",
                        null,
                        null
                    );
                }
                if (thisUisUiMapRoomKeyValue == null)
                {
                    Debug.LogMessage(
                        LogEventLevel.Debug,
                        $"[ERROR] UiMap default room key: {thisUisDefaultRoomKey} Error: UiMap must have an entry keyed to default room key with value of room connection for room state {curScenario.Key} or lockout",
                        null,
                        null
                    );
                    return;
                }
                if (thisUisUiMapRoomKeyValue == "lockout")
                {
                    Debug.LogMessage(
                        LogEventLevel.Debug,
                        $"UiMap default room key: {thisUisDefaultRoomKey} is in lockout state",
                        null,
                        null
                    );
                    var path = _props?.Lockout?.MobileControlPath;
                    Debug.LogMessage(LogEventLevel.Debug, $"Lockout path: {path}", null, null);
                    if (path == null || path.Length == 0)
                        path = "/lockout";
                    Debug.LogMessage(LogEventLevel.Debug, $"Lockout path: {path}", null, null);
                    var webViewConfig =
                        _props?.Lockout?.UiWebViewDisplay == null
                            ? _defaultUiWebViewDisplayConfig
                            : _props.Lockout.UiWebViewDisplay;
                    var room = DeviceManager.GetDeviceForKey(primaryRoomKey) as IKeyName;
                    Debug.LogMessage(LogEventLevel.Debug, $"room: {room?.Name}", null, null);
                    if (webViewConfig.QueryParams == null)
                    {
                        webViewConfig.QueryParams = new Dictionary<string, string>();
                    }
                    webViewConfig.QueryParams.Add(
                        "primaryRoomName",
                        room != null ? room.Name : primaryRoomKey
                    );
                    SendCiscoCodecUiToWebViewMcUrl(path, webViewConfig);
                    // Start the timer when lockout occurs
                    lockoutTimer = new System.Timers.Timer(
                        _props?.Lockout?.PollInterval > 0 ? _props.Lockout.PollInterval : 5000
                    );
                    lockoutTimer.Elapsed += OnLockoutTimerElapsed;
                    lockoutTimer.AutoReset = true;
                    lockoutTimer.Enabled = true;
                    return;
                }
                Debug.LogMessage(
                    LogEventLevel.Debug,
                    $"ui with default room key {thisUisDefaultRoomKey} is not locked out",
                    null,
                    null
                );
            }
            catch (Exception ex)
            {
                Debug.LogMessage(
                    ex,
                    "Error in Combiner_RoomCombinationScenarioChanged_Lockout_EventHandler",
                    null,
                    null
                );
                Debug.LogMessage(
                    LogEventLevel.Debug,
                    $"Error in Combiner_RoomCombinationScenarioChanged_Lockout_EventHandler: {ex.Message}, {ex.StackTrace}",
                    null,
                    null
                );
            }
        }

        private void OnLockoutTimerElapsed(Object source, System.Timers.ElapsedEventArgs e) { }

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
                    + $"appUrl null: {_mcTpController?.AppUrlFeedback?.StringValue == null}",
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

            // Append "/lockout" to the path
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
                new UiWEbViewDisplayClearActionArgs() { Target = "Controller" }
            );
        }

        public void ClearCiscoCodecUiWebViewOsd()
        {
            Debug.LogMessage(LogEventLevel.Debug, "ClearCiscoCodecUiWebViewOsd", this);
            _extensionsHandler?.UiWebViewClearAction?.Invoke(
                new UiWEbViewDisplayClearActionArgs() { Target = "OSD" }
            );
        }
    }
}
