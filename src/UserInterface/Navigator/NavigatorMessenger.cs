using System.Collections.Generic;
using PepperDash.Essentials.AppServer.Messengers;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.WebView;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Navigator
{
    public class NavigatorMessenger : MessengerBase
    {
        private readonly NavigatorController navigator;

        public NavigatorMessenger(string key, string messagePath, NavigatorController navigator) : base(key, messagePath, navigator)
        {
            this.navigator = navigator;
        }

        protected override void RegisterActions()
        {
            if (navigator == null)
            {

                return;
            }

            AddAction($"/closeWebViewController", (id, context) => navigator.CloseWebViewController());

            AddAction($"/closeWebViewOSD", (id, context) => navigator.CloseWebViewOsd());

            AddAction($"/showWebViewOSD", (id, context) =>
            {
                var url = context["url"]?.ToString();
                var webviewConfig = context["webviewConfig"]?.ToObject<WebViewDisplayConfig>();
                if (webviewConfig != null && !string.IsNullOrEmpty(url))
                {
                    navigator.ShowWebViewOsd(url, webviewConfig);
                }
            });
            AddAction("/fullStatus", (id, content) => SendFullStatus());

        }

        private void SendFullStatus()
        {
            var webViewConfigs = WebViewDisplayConfig.GetWebViewConfigs();
            var message = new WebViewConfigStateMessage
            {
                WebViewConfigs = webViewConfigs
            };

            PostStatusMessage(message);
        }

        public class WebViewConfigStateMessage : DeviceStateMessageBase
        {
            public List<WebViewDisplayConfig> WebViewConfigs { get; set; }
        }
    }
}


