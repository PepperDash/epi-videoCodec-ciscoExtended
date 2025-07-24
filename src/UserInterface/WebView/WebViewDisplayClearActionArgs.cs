namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.WebView
{

    public class WebViewDisplayClearActionArgs
    {
        /// <summary>
        /// OSD, Controller, PersistentWebApp Controller: Only for Cisco internal use.
        /// OSD: Close the web view that is displayed on the screen of the device.PersistentWebApp: Only for Cisco internal use.
        /// </summary>
        public string Target { get; set; }
    }
}
