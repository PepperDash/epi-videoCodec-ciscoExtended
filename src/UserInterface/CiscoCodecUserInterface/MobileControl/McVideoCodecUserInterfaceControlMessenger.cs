using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Interfaces;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.CiscoCodecUserInterface.MobileControl;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceWebViewDisplay;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Essentials.AppServer.Messengers;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using static PepperDash.Essentials.Plugin.CiscoRoomOsCodec.CiscoCodecStatus;

namespace PepperDash.Essentials.Touchpanel
{
    public class McVideoCodecUserInterfaceControlMessenger : MessengerBase
        {
        private readonly IMcCiscoCodecUserInterfaceAppControl _appControl;

        public McVideoCodecUserInterfaceControlMessenger(string key, string messagePath, Device device) : base(key, messagePath, device)
            {
            _appControl = device as IMcCiscoCodecUserInterfaceAppControl;
            }

        protected override void RegisterActions()
            {
            if (_appControl == null)
                {
                Debug.Console(0, this, $"{_device.Key} does not implement ITswAppControl");
                return;
                }

            AddAction($"/closeWebViewController", (id, context) => _appControl.CloseWebViewController());

            AddAction($"/closeWebViewOSD", (id, context) => _appControl.CloseWebViewOsd());

            AddAction($"/showWebViewOSD", (id, context) =>
            {
                var url = context["url"]?.ToString();
                var webviewConfig = context["webviewConfig"]?.ToObject<UiWebViewDisplayConfig>();
                if (webviewConfig != null && !string.IsNullOrEmpty(url))
                    {
                    _appControl.ShowWebViewOsd(url, webviewConfig);
                    }
            });
            AddAction("/fullStatus", (id, content) => SendFullStatus());

            }
       
        private void SendFullStatus()
            {
            var webViewConfigs = UiWebViewDisplayConfig.GetWebViewConfigs();
            var message = new WebViewConfigStateMessage
                {
                WebViewConfigs = webViewConfigs
                };

            PostStatusMessage(message);
            }

        public class WebViewConfigStateMessage : DeviceStateMessageBase
        {
        public List<UiWebViewDisplayConfig> WebViewConfigs { get; set; }
        }

    //public class VideoCodecUserInterfaceAppStateMessage : DeviceStateMessageBase
    //{
    //    [JsonProperty("appOpen", NullValueHandling = NullValueHandling.Ignore)]
    //    public bool? AppOpen { get; set; }
    //}
        }
    }
        
    
