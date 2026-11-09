#nullable enable
using System;
using System.Collections.Generic;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras
{
    /// <summary>
    /// Result of feeding one sample into <see cref="CodecCameraOutageTracker"/>.
    /// </summary>
    public enum CodecOutageTransition
    {
        /// <summary>No change in outage state.</summary>
        None,

        /// <summary>The codec just entered an outage.</summary>
        Started,

        /// <summary>The codec just left an outage.</summary>
        Ended,

        /// <summary>
        /// An "all cameras lost" outage has lasted longer than the hold ceiling: the codec is still
        /// counted as in outage, but recovery is no longer held for it.
        /// </summary>
        HoldExpired
    }

    /// <summary>
    /// One camera-status record as the codec reports it: whether it is connected and its serial.
    /// </summary>
    public readonly struct CodecCameraStatusSample
    {
        /// <summary>Creates a sample.</summary>
        public CodecCameraStatusSample(bool connected, string? serialNumber)
        {
            Connected = connected;
            SerialNumber = serialNumber;
        }

        /// <summary>True when the codec reports the camera connected.</summary>
        public bool Connected { get; }

        /// <summary>Serial the codec reports for the camera; may be empty while it is not yet known.</summary>
        public string? SerialNumber { get; }
    }

    /// <summary>
    /// Decides, per codec, whether the camera manager is looking at a codec or network outage
    /// rather than a single floating camera.
    /// <para>
    /// A codec is in outage when either:
    /// <list type="bullet">
    /// <item>its control connection is offline (camera state cannot be trusted), or</item>
    /// <item>it reports no connected camera at all, after it has been seen with at least one
    /// connected "anchor" camera. An anchor is a camera the manager never moves (for example a
    /// Quad camera, or any camera assigned to the same codec in every scenario), identified by its
    /// codec-reported serial. An anchor can only disappear for a reason outside the manager, so
    /// losing every camera, anchors included, means the codec or its network segment is down, not
    /// that a camera is floating.</item>
    /// </list>
    /// A codec that has never shown an identified anchor is not armed: it may legitimately end up
    /// with no cameras when all of its cameras move to another codec, so it is never declared in
    /// outage by camera count alone. A camera with no serial yet is not an anchor (it could be a
    /// moving camera).
    /// </para>
    /// <para>
    /// The outage ends as soon as the codec is online and reports at least one connected camera.
    /// An "all cameras lost" outage is an inference, so recovery is only held for it up to a
    /// ceiling (<c>allCamerasLostMaxHold</c>); a codec-offline outage is held for as long as the
    /// codec is offline. Not thread-safe; the caller serializes access.
    /// </para>
    /// </summary>
    public sealed class CodecCameraOutageTracker
    {
        /// <summary>Reason reported while the codec control connection is offline.</summary>
        public const string ReasonCodecOffline = "codecOffline";

        /// <summary>Reason reported while an armed codec reports no connected cameras.</summary>
        public const string ReasonAllCamerasLost = "allCamerasLost";

        private sealed class CodecState
        {
            public bool Armed;
            public bool InOutage;
            public string? Reason;
            public DateTime SinceUtc;
            public bool HoldExpired;
        }

        private readonly Dictionary<string, CodecState> states = new Dictionary<string, CodecState>(StringComparer.OrdinalIgnoreCase);
        private readonly TimeSpan allCamerasLostMaxHold;

        /// <summary>
        /// Creates the tracker.
        /// </summary>
        /// <param name="allCamerasLostMaxHold">How long an "all cameras lost" outage may hold recovery.</param>
        public CodecCameraOutageTracker(TimeSpan allCamerasLostMaxHold)
        {
            this.allCamerasLostMaxHold = allCamerasLostMaxHold;
        }

        /// <summary>
        /// Counts the connected cameras in one codec's status records, and how many of them are
        /// anchors for that codec. A camera is an anchor when its serial is known and either it is
        /// not a managed camera (e.g. a Quad), or it is a managed camera whose only configured
        /// owner is this codec. A moving camera, or a fixed camera seen on another codec (during a
        /// probe or wrong-codec recovery), is never an anchor, so it cannot arm that codec.
        /// </summary>
        /// <param name="cameras">The codec's camera-status records.</param>
        /// <param name="codecKey">The codec the records belong to.</param>
        /// <param name="managedCameraOwnerCodecs">Managed camera serial to the codecs it is assigned to in any scenario.</param>
        /// <param name="connectedCount">Connected cameras.</param>
        /// <param name="connectedAnchorCount">Connected anchors for this codec.</param>
        public static void CountCameras(
            IEnumerable<CodecCameraStatusSample> cameras,
            string codecKey,
            IReadOnlyDictionary<string, HashSet<string>> managedCameraOwnerCodecs,
            out int connectedCount,
            out int connectedAnchorCount)
        {
            connectedCount = 0;
            connectedAnchorCount = 0;
            foreach (var camera in cameras)
            {
                if (!camera.Connected)
                {
                    continue;
                }

                connectedCount++;

                var serial = camera.SerialNumber?.Trim();
                if (string.IsNullOrEmpty(serial)
                    || string.Equals(serial, "Unknown", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!managedCameraOwnerCodecs.TryGetValue(serial!, out var owners))
                {
                    // Unmanaged: the manager never moves it.
                    connectedAnchorCount++;
                }
                else if (owners.Count == 1 && owners.Contains(codecKey))
                {
                    // Fixed managed camera on its own codec.
                    connectedAnchorCount++;
                }
            }
        }

        /// <summary>
        /// Feeds the latest view of one codec into the tracker.
        /// </summary>
        /// <param name="codecKey">Codec device key.</param>
        /// <param name="codecOnline">True when the codec control connection is online.</param>
        /// <param name="onlineCameraCount">Number of cameras the codec currently reports connected.</param>
        /// <param name="onlineAnchorCameraCount">How many of those are identified anchors.</param>
        /// <param name="nowUtc">Current UTC time.</param>
        /// <returns>The transition caused by this sample.</returns>
        public CodecOutageTransition Update(string codecKey, bool codecOnline, int onlineCameraCount, int onlineAnchorCameraCount, DateTime nowUtc)
        {
            if (string.IsNullOrWhiteSpace(codecKey))
            {
                return CodecOutageTransition.None;
            }

            if (!states.TryGetValue(codecKey, out var state))
            {
                state = new CodecState();
                states[codecKey] = state;
            }

            if (codecOnline && onlineAnchorCameraCount > 0)
            {
                state.Armed = true;
            }

            string? reason = null;
            if (!codecOnline)
            {
                reason = ReasonCodecOffline;
            }
            else if (state.Armed && onlineCameraCount <= 0)
            {
                reason = ReasonAllCamerasLost;
            }

            if (reason != null)
            {
                if (!state.InOutage)
                {
                    state.InOutage = true;
                    state.Reason = reason;
                    state.SinceUtc = nowUtc;
                    state.HoldExpired = false;
                    return CodecOutageTransition.Started;
                }

                state.Reason = reason;
                if (!state.HoldExpired
                    && reason == ReasonAllCamerasLost
                    && nowUtc - state.SinceUtc >= allCamerasLostMaxHold)
                {
                    state.HoldExpired = true;
                    return CodecOutageTransition.HoldExpired;
                }

                return CodecOutageTransition.None;
            }

            if (!state.InOutage)
            {
                return CodecOutageTransition.None;
            }

            state.InOutage = false;
            state.Reason = null;
            state.HoldExpired = false;
            return CodecOutageTransition.Ended;
        }

        /// <summary>True when the codec is currently in outage (held or not).</summary>
        public bool IsInOutage(string codecKey)
        {
            return TryGetOutage(codecKey, out _);
        }

        /// <summary>
        /// True when recovery should be held for the codec: it is in outage, and either the codec
        /// is offline or the "all cameras lost" hold ceiling has not been reached.
        /// </summary>
        public bool IsHolding(string codecKey)
        {
            return TryGetOutage(codecKey, out var state)
                && (state.Reason == ReasonCodecOffline || !state.HoldExpired);
        }

        /// <summary>True when the codec has been seen with a connected, identified anchor camera.</summary>
        public bool IsArmed(string codecKey)
        {
            return !string.IsNullOrWhiteSpace(codecKey)
                && states.TryGetValue(codecKey, out var state)
                && state.Armed;
        }

        /// <summary>Reason for the current outage, or null when not in outage.</summary>
        public string? GetReason(string codecKey)
        {
            return TryGetOutage(codecKey, out var state) ? state.Reason : null;
        }

        /// <summary>UTC time the current outage started, or <see cref="DateTime.MinValue"/> when not in outage.</summary>
        public DateTime GetOutageSinceUtc(string codecKey)
        {
            return TryGetOutage(codecKey, out var state) ? state.SinceUtc : DateTime.MinValue;
        }

        private bool TryGetOutage(string codecKey, out CodecState state)
        {
            if (!string.IsNullOrWhiteSpace(codecKey)
                && states.TryGetValue(codecKey, out var found)
                && found.InOutage)
            {
                state = found;
                return true;
            }

            state = null!;
            return false;
        }
    }
}
