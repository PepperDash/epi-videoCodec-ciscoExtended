using System.Collections.Generic;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace epi_videoCodec_ciscoExtended.V2
{
    public class CiscoRoomOsPluginFactory : EssentialsPluginDeviceFactory<CiscoRoomOsDevice>
    {
        public CiscoRoomOsPluginFactory()
        {
            TypeNames = new List<string> {"ciscoRoomOs"};
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            return new CiscoRoomOsDevice(dc);
        }
    }
}