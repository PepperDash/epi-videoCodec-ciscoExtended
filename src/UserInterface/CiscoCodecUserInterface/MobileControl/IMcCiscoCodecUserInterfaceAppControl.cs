using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Touchpanel
{
    public interface IMcCiscoCodecUserInterfaceAppControl : IKeyed
    {
        //BoolFeedback WebViewOpenFeedback { get; }

        void CloseWebViewController();
        void CloseWebViewOsd();
	}
}
