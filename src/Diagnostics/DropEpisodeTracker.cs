#nullable enable
using System;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// Decides when to log that something meant for the codec was dropped, without logging every
    /// drop (#194). A run of drops with no success in between is one episode:
    /// <list type="bullet">
    /// <item>the first drop of an episode is logged;</item>
    /// <item>while it lasts, one more drop is logged per <c>reminderInterval</c>, with the count since
    /// the last logged one;</item>
    /// <item>the first success after it ends the episode and reports how many were dropped, so the
    /// caller can log the recovery once.</item>
    /// </list>
    /// Time is passed in, so it can be tested without a clock. Thread-safe.
    /// </summary>
    /// <remarks>
    /// Has no Crestron dependencies, so the test project compiles and tests it directly.
    /// </remarks>
    public sealed class DropEpisodeTracker
    {
        private readonly object _sync = new object();
        private readonly TimeSpan _reminderInterval;

        private int _droppedInEpisode;
        private int _droppedSinceLog;
        private DateTime _lastLoggedUtc;

        /// <param name="reminderInterval">How often a long episode is logged again.</param>
        public DropEpisodeTracker(TimeSpan reminderInterval)
        {
            if (reminderInterval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(reminderInterval), "must be positive");
            _reminderInterval = reminderInterval;
        }

        /// <summary>Drops in the episode in progress (0 when there is none).</summary>
        public int DroppedInEpisode
        {
            get { lock (_sync) return _droppedInEpisode; }
        }

        /// <summary>
        /// Records a drop. Returns true when the caller should log it: the first drop of an episode, or
        /// the first after <c>reminderInterval</c> since the last logged one.
        /// </summary>
        /// <param name="nowUtc">The current time.</param>
        /// <param name="droppedSinceLastLog">Drops since the last logged one, this one included.</param>
        public bool OnDropped(DateTime nowUtc, out int droppedSinceLastLog)
        {
            lock (_sync)
            {
                _droppedInEpisode++;
                _droppedSinceLog++;

                var log = _droppedInEpisode == 1 || nowUtc - _lastLoggedUtc >= _reminderInterval;
                droppedSinceLastLog = _droppedSinceLog;
                if (log)
                {
                    _lastLoggedUtc = nowUtc;
                    _droppedSinceLog = 0;
                }

                return log;
            }
        }

        /// <summary>
        /// Records a success. Returns how many were dropped in the episode it ends, or 0 when no episode
        /// was in progress (the usual case: log nothing).
        /// </summary>
        public int OnSucceeded()
        {
            lock (_sync)
            {
                var dropped = _droppedInEpisode;
                _droppedInEpisode = 0;
                _droppedSinceLog = 0;
                return dropped;
            }
        }

        /// <summary>
        /// A short, safe description of a codec command for the log: its first few words (for example
        /// "xCommand Audio Microphones"), never its arguments, which can carry numbers, URIs or PINs.
        /// </summary>
        public static string Describe(string? command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return "(empty)";

            const int maxWords = 3;
            const int maxLength = 60;

            var words = command!.Split(new[] { ' ', '\t', '\r', '\n' }, maxWords + 1, StringSplitOptions.RemoveEmptyEntries);
            var head = string.Join(" ", words, 0, Math.Min(words.Length, maxWords));
            if (head.Length > maxLength)
                head = head.Substring(0, maxLength);

            return words.Length > maxWords || head.Length < command.Trim().Length ? head + " ..." : head;
        }
    }
}
