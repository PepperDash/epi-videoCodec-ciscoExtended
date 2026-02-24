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

    internal interface INavigatorLockoutHanderWithPwa : INavigatorLockoutHandler
    {
        /// <summary>
        /// Enters PWA mode on the navigator with the specified URL
        /// </summary>
        /// <param name="url"></param>
        /// <param name="prependmcUrl"></param>
        void EnterPwaMode(string url, bool prependmcUrl = true);

        /// <summary>
        /// Exits PWA mode on the navigator and returns to the default UI
        /// </summary>
        void ExitPwaMode();
    }
}