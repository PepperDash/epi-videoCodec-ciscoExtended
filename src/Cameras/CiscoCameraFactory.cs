using PepperDash.Essentials.Core;
using System.Collections.Generic;
using PepperDash.Essentials.Core.Config;
using PepperDash.Core;

namespace epi_videoCodec_ciscoExtended
{
    public class CiscoCameraFactory : EssentialsPluginDeviceFactory<CiscoCamera>
    {
        public CiscoCameraFactory()
        {
            MinimumEssentialsFrameworkVersion = "2.0.0";

            TypeNames = new List<string>() { "ciscocamera" };
        }

        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            Debug.Console(1, "Factory Attempting to create new Cisco Camera Device");

            var props = Newtonsoft.Json.JsonConvert.DeserializeObject<CiscoCodecCameraPropertiesConfig>(dc.Properties.ToString());
            return new CiscoCamera(dc.Key, dc.Name, props);
        }
    }
}
