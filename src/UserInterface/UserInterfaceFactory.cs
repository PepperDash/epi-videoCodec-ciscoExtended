using System.Collections.Generic;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface;
using CiscoCodecUserInterfaceNS = PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.CiscoCodecUserInterface;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.CiscoCodecUserInterface.MobileControl;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;


namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface
{
    public class UserInterfaceFactory : EssentialsPluginDeviceFactory<CiscoCodecUserInterfaceNS.CiscoCodecUserInterface>
    {
        public UserInterfaceFactory()
        {
            MinimumEssentialsFrameworkVersion = "2.0.0";

            TypeNames = new List<string>() { "ciscoRoomOsMobileControl"};
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
			Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "Factory Attempting to create new Cisco RoomOs User Interface Device", null, null);

            var type = dc.Type;

			if (type.Equals("ciscoRoomOsMobileControl"))
			{
				return new McVideoCodecTouchpanelController(dc);
			}
			return new CiscoCodecUserInterfaceNS.CiscoCodecUserInterface(dc);
        }
    }
}