#nullable enable
using System;
using System.Collections.Generic;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras
{
    /// <summary>
    /// Where a camera serial is connected according to the codecs' own camera-status records.
    /// </summary>
    public readonly struct CameraPresenceMatch
    {
        /// <summary>Creates a match.</summary>
        public CameraPresenceMatch(string codecKey, uint cameraId)
        {
            CodecKey = codecKey;
            CameraId = cameraId;
        }

        /// <summary>Codec reporting the camera connected.</summary>
        public string CodecKey { get; }

        /// <summary>Slot the codec reports it in (0 when unknown).</summary>
        public uint CameraId { get; }
    }

    /// <summary>
    /// Finds a camera by serial in the codecs' camera-status records, in any slot. The camera
    /// manager's own view (control wrappers matched by configured slot) can miss a camera that is
    /// connected in a different slot than configured; before probing a "lost" camera the manager
    /// checks here, so a camera that is actually connected is never power-cycled.
    /// </summary>
    public static class CameraPresence
    {
        /// <summary>
        /// Looks for <paramref name="serialNumber"/> connected on any codec, preferring
        /// <paramref name="preferredCodecKey"/> (the camera's target) when several report it.
        /// </summary>
        /// <param name="codecs">Codec key to that codec's camera-status records.</param>
        /// <param name="serialNumber">Camera serial to look for.</param>
        /// <param name="preferredCodecKey">Codec to prefer when more than one reports the serial.</param>
        /// <param name="match">Where the camera was found.</param>
        /// <returns>True when a codec reports the serial connected.</returns>
        public static bool TryFindConnected(
            IEnumerable<KeyValuePair<string, IReadOnlyList<CodecCameraStatusSample>>> codecs,
            string serialNumber,
            string? preferredCodecKey,
            out CameraPresenceMatch match)
        {
            match = default;
            var serial = serialNumber?.Trim();
            if (string.IsNullOrEmpty(serial))
            {
                return false;
            }

            var found = false;
            foreach (var codec in codecs)
            {
                if (codec.Value == null)
                {
                    continue;
                }

                foreach (var camera in codec.Value)
                {
                    if (!camera.Connected
                        || !string.Equals(camera.SerialNumber?.Trim(), serial, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (string.Equals(codec.Key, preferredCodecKey, StringComparison.OrdinalIgnoreCase))
                    {
                        match = new CameraPresenceMatch(codec.Key, camera.CameraId);
                        return true;
                    }

                    if (!found)
                    {
                        match = new CameraPresenceMatch(codec.Key, camera.CameraId);
                        found = true;
                    }
                }
            }

            return found;
        }
    }
}
