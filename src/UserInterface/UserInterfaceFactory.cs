using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Navigator;


namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface
{
    public class UserInterfaceFactory : EssentialsPluginDeviceFactory<CiscoCodecUserInterface>
    {
        public UserInterfaceFactory()
        {
            MinimumEssentialsFrameworkVersion = "2.0.0";

            TypeNames = new List<string>() { "ciscoRoomOsMobileControl" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "Factory Attempting to create new Cisco RoomOs User Interface Device", null, null);

            var type = dc.Type;

            if (type.Equals("ciscoRoomOsMobileControl"))
            {
                return new NavigatorController(dc);
            }
            return new CiscoCodecUserInterface(dc);
        }
    }
}