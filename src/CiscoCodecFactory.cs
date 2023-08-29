using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;


namespace epi_videoCodec_ciscoExtended
{
    public class CiscoCodecFactory : EssentialsPluginDeviceFactory<CiscoCodec>
    {
        public CiscoCodecFactory()
        {
            MinimumEssentialsFrameworkVersion = "1.15.2";

            TypeNames = new List<string>() { "ciscoRoomOS" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            Debug.Console(1, "Factory Attempting to create new Cisco RoomOs Device");

            var comm = CommFactory.CreateCommForDevice(dc);
            return new CiscoCodec(dc, comm);
        }
    }
}