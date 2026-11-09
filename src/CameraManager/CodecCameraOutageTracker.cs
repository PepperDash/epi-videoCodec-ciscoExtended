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
        Ended
    }

    /// <summary>
    /// Decides, per codec, whether the camera manager is looking at a codec or network outage
    /// rather than a single floating camera.
    /// <para>
    /// A codec is in outage when either:
    /// <list type="bullet">
    /// <item>its control connection is offline (camera state cannot be trusted), or</item>
    /// <item>it reports no online cameras at all, after it has been seen with at least one online
    /// "anchor" camera. An anchor is a camera the manager never moves (for example a Quad camera,
    /// or any camera assigned to the same codec in every scenario). An anchor can only disappear
    /// for a reason outside the manager, so losing every camera, anchors included, means the codec
    /// or its network segment is down, not that a camera is floating.</item>
    /// </list>
    /// A codec that has never shown an anchor online is not armed: it may legitimately end up with
    /// no cameras when all of its cameras move to another codec, so it is never declared in outage
    /// by camera count alone.
    /// </para>
    /// <para>
    /// The outage ends as soon as the codec is online and reports at least one online camera.
    /// Not thread-safe; the caller serializes access.
    /// </para>
    /// </summary>
    public sealed class CodecCameraOutageTracker
    {
        /// <summary>Reason reported while the codec control connection is offline.</summary>
        public const string ReasonCodecOffline = "codecOffline";

        /// <summary>Reason reported while an armed codec reports no online cameras.</summary>
        public const string ReasonAllCamerasLost = "allCamerasLost";

        private sealed class CodecState
        {
            public bool Armed;
            public bool InOutage;
            public string? Reason;
            public DateTime SinceUtc;
        }

        private readonly Dictionary<string, CodecState> states = new Dictionary<string, CodecState>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Feeds the latest view of one codec into the tracker.
        /// </summary>
        /// <param name="codecKey">Codec device key.</param>
        /// <param name="codecOnline">True when the codec control connection is online.</param>
        /// <param name="onlineCameraCount">Number of cameras the codec currently reports online.</param>
        /// <param name="onlineAnchorCameraCount">How many of those are anchors (never moved by the manager).</param>
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
                if (state.InOutage)
                {
                    state.Reason = reason;
                    return CodecOutageTransition.None;
                }

                state.InOutage = true;
                state.Reason = reason;
                state.SinceUtc = nowUtc;
                return CodecOutageTransition.Started;
            }

            if (!state.InOutage)
            {
                return CodecOutageTransition.None;
            }

            state.InOutage = false;
            state.Reason = null;
            return CodecOutageTransition.Ended;
        }

        /// <summary>True when the codec is currently in outage.</summary>
        public bool IsInOutage(string codecKey)
        {
            return !string.IsNullOrWhiteSpace(codecKey)
                && states.TryGetValue(codecKey, out var state)
                && state.InOutage;
        }

        /// <summary>True when the codec has been seen with an online anchor camera.</summary>
        public bool IsArmed(string codecKey)
        {
            return !string.IsNullOrWhiteSpace(codecKey)
                && states.TryGetValue(codecKey, out var state)
                && state.Armed;
        }

        /// <summary>Reason for the current outage, or null when not in outage.</summary>
        public string? GetReason(string codecKey)
        {
            return !string.IsNullOrWhiteSpace(codecKey) && states.TryGetValue(codecKey, out var state) && state.InOutage
                ? state.Reason
                : null;
        }

        /// <summary>UTC time the current outage started, or <see cref="DateTime.MinValue"/> when not in outage.</summary>
        public DateTime GetOutageSinceUtc(string codecKey)
        {
            return !string.IsNullOrWhiteSpace(codecKey) && states.TryGetValue(codecKey, out var state) && state.InOutage
                ? state.SinceUtc
                : DateTime.MinValue;
        }
    }
}
