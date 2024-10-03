using Crestron.SimplSharpPro.DeviceSupport;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceExtensions.Panels
{
    public interface ICiscoCodecUiExtensionsPanel
    {
        /// <summary>
        /// click event for panel button
        /// </summary>
        event EventHandler ClickedEvent;

        /// <summary>
        /// numbered order of panels shown on cisco ui
        /// </summary>
        ushort Order { get; }

        /// <summary>
        /// Panel id string. Must be unique per config
        /// </summary>
        string PanelId { get; }

        /// <summary>
        ///  For a Panel Button CallControls, ControlPanel, Hidden
        /// </summary>
        string Location { get; }

        /// <summary>
        /// Briefing, Camera, Concierge, Disc, Handset, Help, Helpdesk,
        /// Home, Hvac, Info, Input, Language, Laptop, Lightbulb, Media, 
        /// Microphone, Power, Proximity, Record, Spark, Tv, Webex, 
        /// General, Custom
        /// The icon on the button.Use one of the preinstalled icons from 
        /// the list or select Custom to use a custom icon that has been
        /// uploaded to the device.
        /// </summary>
        eCiscoPanelIcons Icon { get; }

        /// <summary>
        /// The unique identifier of the uploaded custom icon.
        /// </summary>
        string IconId { get; }

        /// <summary>
        /// The new name of the custom panel, action button, or web app.
        /// </summary>
        string Name { get; }
    }
}
