namespace epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceWebViewDisplay
{
    /// <summary>
    /// xCommand UserInterface WebView Display Header: value Mode: value Options: value Target: value Title: value Url: value
    /// </summary>
    public class UiWebViewDisplay
    {
        /// <summary>
        /// <0 - 8192> An HTTP header field.You can add up 15 Header parameters in one command, each holding one HTTP header field.
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// Fullscreen, Modal Full screen: Display the web page on the entire screen.Modal: Display the web page in a window.
        /// </summary>
        public string Mode { get; set; }

        /// <summary>
        /// <0 - 255> This parameter is intended for internal use by the UI Extensions Editor.
        /// </summary>
        public string Options { get; set; }

        /// <summary>
        /// OSD, Controller, PersistentWebApp Controller: 
        /// Only for Cisco internal use.OSD: Close the web view that is displayed on the screen of the device.
        /// PersistentWebApp: Only for Cisco internal use.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// <0 - 255> The title of the web page.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Required <0 - 2000>	The URL of the web page.
        /// </summary>
        public string Url { get; set; }


        /// <summary>
        /// return xcommand string based on props
        /// </summary>
        /// <returns></returns>
        public string xCommand()
        {
            var command = "xCommand UserInterface WebView Display";

            if (!string.IsNullOrEmpty(Header))
                command += $" Header: \"{Header}\"";

            if (!string.IsNullOrEmpty(Mode))
                command += $" Mode: \"{Mode}\"";

            if (!string.IsNullOrEmpty(Options))
                command += $" Options: \"{Options}\"";

            if (!string.IsNullOrEmpty(Target))
                command += $" Target: \"{Target}\"";

            if (!string.IsNullOrEmpty(Title))
                command += $" Title: \"{Title}\"";

            if (!string.IsNullOrEmpty(Url))
                command += $" Url: \"{Url}\"";

            command += CiscoCodec.Delimiter;

            return command;
        }

    }
}
