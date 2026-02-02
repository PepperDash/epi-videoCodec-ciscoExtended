using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.WebView;


namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Navigator
{
    internal interface INavigatorLockoutHandler
    {
        void Activate(NavigatorController parent);

        void SendWebViewMcUrl(
            string mcPath,
            WebViewDisplayConfig webViewConfig, bool prependmcUrl = true
        );

        void SendWebViewUrl(string url, WebViewDisplayConfig webViewConfig);

        void ClearWebView();

        void ClearWebViewOsd();
    }
}
    