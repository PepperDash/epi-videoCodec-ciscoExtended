#nullable enable
using System;
using System.Collections.Generic;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// Matches codec replies to the requests that asked for them, by <c>| resultId="…"</c> (#129, stage 4).
    /// </summary>
    /// <remarks>
    /// <para>Replaces the phonebook's queue of ids, which dequeued and dropped pending searches whenever
    /// a reply without the expected id arrived (the start-up queries carry none; #109). A reply now
    /// completes only its own request, and a reply without an id completes nothing.</para>
    /// <para>The codec answers in any order (the xAPI is asynchronous), so requests are kept by id, not
    /// in a queue. A request that gets no reply is removed by <see cref="Expire"/> after its timeout.</para>
    /// <para>Thread-safe. Has no Crestron dependencies, so the test project compiles and tests it directly.</para>
    /// </remarks>
    public sealed class XapiRequestTracker
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, Pending> _pending = new Dictionary<string, Pending>(StringComparer.Ordinal);
        private readonly Func<DateTime> _utcNow;

        public XapiRequestTracker(TimeSpan timeout, Func<DateTime>? utcNow = null)
        {
            if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
            Timeout = timeout;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public TimeSpan Timeout { get; }

        /// <summary>
        /// Registers a new request of the given kind and returns its id, for <c>| resultId="…"</c>.
        /// <paramref name="timeout"/> overrides <see cref="Timeout"/> for this request (a large reply
        /// over a serial link, for example).
        /// </summary>
        public string Begin(string kind, TimeSpan? timeout = null)
        {
            if (timeout.HasValue && timeout.Value <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            var id = Guid.NewGuid().ToString();
            lock (_lock)
                _pending[id] = new Pending(kind, _utcNow(), timeout ?? Timeout);
            return id;
        }

        /// <summary>
        /// Completes the request with this id. Returns false (and changes nothing) when the id is
        /// missing, empty, the all-zero GUID, unknown, or already completed or expired.
        /// </summary>
        public bool TryComplete(string? id, out string kind)
        {
            kind = string.Empty;
            if (string.IsNullOrEmpty(id) || id == Guid.Empty.ToString())
                return false;

            lock (_lock)
            {
                if (!_pending.TryGetValue(id!, out var pending))
                    return false;
                _pending.Remove(id!);
                kind = pending.Kind;
                return true;
            }
        }

        /// <summary>All requests still waiting for a reply.</summary>
        public int PendingCount()
        {
            lock (_lock)
                return _pending.Count;
        }

        /// <summary>Requests of this kind still waiting for a reply.</summary>
        public int PendingCount(string kind)
        {
            lock (_lock)
            {
                var n = 0;
                foreach (var p in _pending.Values)
                    if (p.Kind == kind)
                        n++;
                return n;
            }
        }

        /// <summary>Removes and returns requests older than their timeout: (id, kind).</summary>
        public IReadOnlyList<KeyValuePair<string, string>> Expire()
        {
            var now = _utcNow();
            var expired = new List<KeyValuePair<string, string>>();
            lock (_lock)
            {
                foreach (var entry in _pending)
                    if (now - entry.Value.StartedUtc >= entry.Value.Timeout)
                        expired.Add(new KeyValuePair<string, string>(entry.Key, entry.Value.Kind));
                foreach (var e in expired)
                    _pending.Remove(e.Key);
            }

            return expired;
        }

        /// <summary>Drops every pending request, e.g. when the connection changes.</summary>
        public void Clear()
        {
            lock (_lock)
                _pending.Clear();
        }

        private sealed class Pending
        {
            public Pending(string kind, DateTime startedUtc, TimeSpan timeout)
            {
                Kind = kind;
                StartedUtc = startedUtc;
                Timeout = timeout;
            }

            public string Kind { get; }
            public DateTime StartedUtc { get; }
            public TimeSpan Timeout { get; }
        }
    }
}
