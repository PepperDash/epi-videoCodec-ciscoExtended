using System.Collections.Generic;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace epi_videoCodec_ciscoExtended.V2
{
    public class CiscoRoomOsPluginFactory : EssentialsPluginDeviceFactory<CiscoRoomOsDevice>
    {
        public CiscoRoomOsPluginFactory()
        {
            TypeNames = new List<string> {"ciscoRoomOsV2"};
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            var props = dc.Properties.ToObject<CiscoCodecConfig>();
            var communications = CommFactory.CreateCommForDevice(dc);
            return new CiscoRoomOsDevice(dc.Key, dc.Name, props, communications);
        }
    }
}