using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;


namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// Factory class for creating Cisco codec devices.
    /// Supports various Cisco Room OS codec models including Room Bar, Codec EQ, and Codec Pro.
    /// </summary>
    public class CiscoCodecFactory : EssentialsPluginDeviceFactory<CiscoCodec>
    {
        /// <summary>
        /// Initializes a new instance of the CiscoCodecFactory class.
        /// Sets up supported type names and minimum framework version requirements.
        /// </summary>
        public CiscoCodecFactory()
        {
            MinimumEssentialsFrameworkVersion = "1.15.2";

            TypeNames = new List<string>() { "ciscoRoomOS", "ciscoRoomBar", "ciscoRoomBarPro", "ciscoCodecEq", "ciscoCodecPro" };
        }

        /// <inheritdoc />
        public override EssentialsDevice BuildDevice(DeviceConfig dc)
        {
            Debug.Console(1, "Factory Attempting to create new Cisco RoomOs Device");

            var comm = CommFactory.CreateCommForDevice(dc);
            return new CiscoCodec(dc, comm);
        }
    }
}