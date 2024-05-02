using System.Collections.Generic;
using epi_videoCodec_ciscoExtended.UserInterface;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;


namespace epi_videoCodec_ciscoExtended
{
    public class UserInterfaceFactory : EssentialsPluginDeviceFactory<CiscoCodecUserInterface>
    {
        public UserInterfaceFactory()
        {
            MinimumEssentialsFrameworkVersion = "2.0.0";

            TypeNames = new List<string>() { "ciscoRoomOsMobileControl", "ciscoRoomOs" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
			Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "Factory Attempting to create new Cisco RoomOs User Interface Device", null, null);

            var type = dc.Type;

			if (type.Equals("ciscoRoomOsMobileControl"))
			{
				return new CiscoUiMobileControlTouchpanelController(dc);
			}
			return new CiscoCodecUserInterface(dc);
        }
    }
}