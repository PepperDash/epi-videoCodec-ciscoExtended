#nullable enable
using System;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// Matches the reply to the sync's tagged full xStatus request, and keeps a reply from one session
    /// from counting in the next.
    /// </summary>
    /// <remarks>
    /// <para>The reply is matched on the receive thread and the sync is reset on the sync worker. Each
    /// runs under one lock here, so a reset either comes before the match (the reply then matches
    /// nothing, since its request was cancelled) or after it (and clears what the match set). Before, a
    /// reset between the check of the request and the setting of <see cref="Arriving"/> left it set for a
    /// cancelled request, and the next session didn't ask for its own full status.</para>
    /// <para><see cref="Arriving"/> is set by a match and cleared only by a reset, so it also tells the
    /// sync, when it applies a matched reply, whether that reply belongs to the current session.</para>
    /// <para>Thread-safe. Has no Crestron dependencies, so the test project compiles and tests it
    /// directly.</para>
    /// </remarks>
    public sealed class FullStatusReplyGate
    {
        private readonly XapiRequestTracker _requests;
        private readonly string _kind;
        private readonly object _lock = new object();
        private volatile bool _arriving;

        /// <param name="requests">The tracker the full xStatus requests are registered in.</param>
        /// <param name="kind">The request kind of the sync's full xStatus requests.</param>
        public FullStatusReplyGate(XapiRequestTracker requests, string kind)
        {
            _requests = requests ?? throw new ArgumentNullException(nameof(requests));
            _kind = kind ?? throw new ArgumentNullException(nameof(kind));
        }

        /// <summary>
        /// A reply to this session's full xStatus request has been matched. Set before the request leaves
        /// the tracker, so a reader checking pending first and then this never sees neither for a reply in
        /// flight. Cleared only by <see cref="Reset"/>.
        /// </summary>
        public bool Arriving => _arriving;

        /// <summary>Test seam: runs inside the match, after the request was found and before it is marked.</summary>
        internal Action? AfterMatchForTest;

        /// <summary>
        /// Completes the request with this id if it is a pending full xStatus request (receive thread).
        /// </summary>
        /// <param name="resultId">The reply's resultId.</param>
        /// <returns>True when the reply was this session's full status.</returns>
        public bool TryComplete(string? resultId)
        {
            lock (_lock)
            {
                if (!_requests.TryGetKind(resultId, out var kind) || kind != _kind)
                    return false;

                AfterMatchForTest?.Invoke();
                _arriving = true;
                _requests.TryComplete(resultId, out _);
                return true;
            }
        }

        /// <summary>
        /// Starts a new session: drops the pending full xStatus requests (their replies may never come,
        /// and while pending they would hold back the new session's own request) and clears
        /// <see cref="Arriving"/>.
        /// </summary>
        /// <returns>How many pending requests were dropped.</returns>
        public int Reset()
        {
            lock (_lock)
            {
                _arriving = false;
                return _requests.Cancel(_kind);
            }
        }
    }
}
