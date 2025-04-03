using epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceWebViewDisplay;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using System.Collections.Generic;

namespace PepperDash.Essentials.Touchpanel
{
    public interface IMcCiscoCodecUserInterfaceAppControl : IKeyed
    {
        //BoolFeedback WebViewOpenFeedback { get; }

        void CloseWebViewController();
        void CloseWebViewOsd();
        void ShowWebViewOsd(string url);
        void ShowWebViewOsd(string url, UiWebViewDisplayConfig webviewConfig);
        void ShowWebViewOsd();

        }
}
