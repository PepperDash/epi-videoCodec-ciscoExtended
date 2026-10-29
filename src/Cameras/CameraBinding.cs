using System;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras
{
    /// <summary>
    /// Whether a camera the codec lists is bound to a video input connector.
    /// </summary>
    public enum CameraBindingState
    {
        /// <summary>The codec hasn't reported enough to tell (no Connected or DetectedConnector yet).</summary>
        Unknown,

        /// <summary>The codec reports the camera as not connected.</summary>
        Disconnected,

        /// <summary>Connected, with a DetectedConnector: its video reaches a video input.</summary>
        Bound,

        /// <summary>
        /// Connected but DetectedConnector 0: an Ethernet camera in a slot no video input connector
        /// is configured for (InputSourceType camera, CameraControl CameraId equal to the slot).
        /// Its video never comes up.
        /// </summary>
        Unbound,
    }

    /// <summary>
    /// Works out a camera's <see cref="CameraBindingState"/> from its xStatus Cameras Camera values.
    /// </summary>
    /// <remarks>Has no Crestron dependencies, so the test project compiles and tests it directly.</remarks>
    public static class CameraBinding
    {
        /// <summary>
        /// The binding state from the camera's <c>Connected</c> and <c>DetectedConnector</c> values,
        /// as merged into the state model.
        /// </summary>
        /// <remarks>
        /// Only 0 versus a positive connector counts. The connector number itself isn't a reliable
        /// map (network PTZ cameras can all report the same one), and HardwareID or SoftwareID depend
        /// on firmware. A value that is missing or not a number is <see cref="CameraBindingState.Unknown"/>,
        /// never unbound: partial feedback often leaves DetectedConnector out.
        /// </remarks>
        public static CameraBindingState Evaluate(string connected, string detectedConnector)
        {
            if (string.Equals(connected, "false", StringComparison.OrdinalIgnoreCase))
                return CameraBindingState.Disconnected;

            if (!string.Equals(connected, "true", StringComparison.OrdinalIgnoreCase))
                return CameraBindingState.Unknown;

            if (!int.TryParse(detectedConnector, out var connector) || connector < 0)
                return CameraBindingState.Unknown;

            return connector == 0 ? CameraBindingState.Unbound : CameraBindingState.Bound;
        }
    }
}
