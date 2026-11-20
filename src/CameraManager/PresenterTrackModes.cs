#nullable enable
using System;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// The PresenterTrack modes the camera manager can put a codec in, as written in config
    /// (<c>presenterTrackMode</c>) and as sent to the codec
    /// (<c>xCommand Cameras PresenterTrack Set Mode: &lt;mode&gt;</c>).
    /// </summary>
    /// <remarks>Has no Crestron dependencies, so the test project compiles and tests it directly.</remarks>
    public static class PresenterTrackModes
    {
        private static readonly string[] Modes = { "Follow", "Persistent", "Background", "Off" };

        /// <summary>
        /// Returns the codec's spelling of <paramref name="value"/> ("Follow", "Persistent",
        /// "Background" or "Off"), matched without regard to case or surrounding spaces, or null when
        /// it is empty or not one of those modes.
        /// </summary>
        public static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value!.Trim();
            foreach (var mode in Modes)
            {
                if (string.Equals(mode, trimmed, StringComparison.OrdinalIgnoreCase))
                    return mode;
            }

            return null;
        }
    }
}
