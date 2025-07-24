using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Crestron.SimplSharpPro.DeviceSupport;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions.Panels
{
    /// <summary>
    /// Interface for Cisco Codec UI Extensions Panel.
    /// This interface defines the properties and events for a panel in the Cisco Codec UI Extensions.
    /// It includes properties for panel configuration such as order, ID, location, icon, name, and custom icon.
    /// It also includes an event for handling panel click actions.
    /// </summary>
    public interface ICiscoCodecUiExtensionsPanel
    {
        /// <summary>
        /// Click event for the panel button.
        /// This event is triggered when the panel button is clicked.
        /// It allows external handlers to respond to panel click actions.
        /// </summary>
        event EventHandler ClickedEvent;

        /// <summary>
        /// Numbered order of panels shown on Cisco UI.
        /// This property defines the order in which the panels are displayed.
        /// It must be greater than or equal to 1.
        /// </summary>
        ushort Order { get; }

        /// <summary>
        /// The unique identifier for the panel.
        /// This ID is used to reference the panel in commands and configurations.
        /// It should be a unique string that identifies the panel within the Cisco Codec UI Extensions.
        /// </summary>
        string PanelId { get; }

        /// <summary>
        /// Location of the panel on the Navigation Bar
        /// </summary>
        ///<remarks>
        /// Valid values for codecs in RoomOS mode are: <br />
        /// HomeScreen<br />CallControls<br />HomeScreenAndCallControls<br />ControlPanel<br />Hidden<br />
        /// <br />
        /// Valid values for codecs in MTR mode are: <br />
        /// CallControls<br />ControlPanel<br />Hidden
        /// </remarks>
        ECiscoPanelLocation Location { get; }

        /// <summary>
        /// Briefing, Camera, Concierge, Disc, Handset, Help, Helpdesk,
        /// Home, Hvac, Info, Input, Language, Laptop, Lightbulb, Media, 
        /// Microphone, Power, Proximity, Record, Spark, Tv, Webex, 
        /// General, Custom
        /// The icon on the button. Use one of the preinstalled icons from 
        /// the list or select Custom to use a custom icon that has been
        /// uploaded to the device.
        /// </summary>
        ECiscoPanelIcons Icon { get; }

        /// <summary>
        /// The unique identifier of the uploaded custom icon.
        /// </summary>
        string IconId { get; }

        /// <summary>
        /// Color in hex format (e.g., #FF0000 for red)
        /// <remarks>
        /// This property is not used for text color, but rather for the background color of the panel.
        /// It is applicable only for codecs in RoomOS mode.
        /// </summary>
        string Color { get; }

        /// <summary>
        /// The name of the panel.
        /// This name is used to identify the panel in the user interface.
        /// It should be a descriptive string that represents the function or purpose of the panel.
        /// </summary>
        string Name { get; }
    }
}
