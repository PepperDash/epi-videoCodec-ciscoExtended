using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras
{
    public class CiscoNetworkCameraManagerFactory
        : EssentialsPluginDeviceFactory<CameraManager>
    {
        /// <summary>
        /// Initialises the factory with the supported type names and the minimum
        /// Essentials framework version required.
        /// </summary>
        public CiscoNetworkCameraManagerFactory()
        {
            MinimumEssentialsFrameworkVersion = "2.29.0";
            TypeNames = new List<string> { "ciscocameramanager", "cameramanager" };
        }

        /// <summary>
        /// Builds and returns a <see cref="CameraManager"/> from
        /// the supplied device configuration, or null on failure.
        /// </summary>
        /// <param name="dc">Essentials device configuration entry</param>
        /// <returns>Constructed plugin device, or null</returns>
        public override EssentialsDevice BuildDevice(
            DeviceConfig dc)
        {
            Debug.LogVerbose(
                "[{key}] CiscoNetworkCameraManagerFactory: creating device of type '{type}'",
                dc.Key, dc.Type);

            var config = dc.Properties.ToObject<CameraManagerPropertiesConfig>();
            if (config == null)
            {
                Debug.LogError(
                    "[{key}] CiscoNetworkCameraManagerFactory: failed to deserialise properties config",
                    dc.Key);
                return null;
            }

            return new CameraManager(dc.Key, dc.Name, config);
        }
    }

}