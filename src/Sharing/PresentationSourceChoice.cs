using System;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// Which codec connector a sharingStart presents. No Crestron dependencies, so it is tested directly.
    /// </summary>
    public static class PresentationSourceChoice
    {
        /// <summary>
        /// The selected source when there is one (a route to a codec input, the SIMPL presentation source join);
        /// otherwise the configured default (sharing.defaultPresentationSource), unless that is a camera connector.
        /// 0 means there is nothing to present, and sharingStart does nothing (as before).
        /// </summary>
        /// <param name="selected">The selected presentation source, or 0 (at boot and after every stop).</param>
        /// <param name="configuredDefault">sharing.defaultPresentationSource, or 0 when not configured.</param>
        /// <param name="isCameraConnector">True for a connector a camera is wired to; presenting it would put the
        /// camera image on the room display.</param>
        public static int Resolve(int selected, int configuredDefault, Func<int, bool> isCameraConnector)
        {
            if (selected > 0)
                return selected;

            if (configuredDefault <= 0)
                return 0;

            return isCameraConnector != null && isCameraConnector(configuredDefault) ? 0 : configuredDefault;
        }
    }
}
