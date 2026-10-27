#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// Records how long named operations take (initial sync, phonebook search, dial to connected,
    /// JSON parsing, ...), so builds can be compared on the same codec (#129). Measuring only: nothing
    /// here changes how the plugin behaves. Thread-safe.
    /// </summary>
    /// <remarks>
    /// Has no Crestron dependencies, so the test project compiles and tests it directly.
    /// </remarks>
    public sealed class CodecTimings
    {
        /// <summary>Samples kept per name. Percentiles are computed over these.</summary>
        public const int MaxSamples = 500;

        /// <summary>A started operation that has not stopped after this long is discarded.</summary>
        public static readonly TimeSpan PendingTimeout = TimeSpan.FromMinutes(2);

        private const int MaxPending = 100;

        private readonly object _lock = new object();
        private readonly Dictionary<string, Series> _series = new Dictionary<string, Series>(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _pending = new Dictionary<string, long>(StringComparer.Ordinal);
        private DateTime _sinceUtc = DateTime.UtcNow;

        /// <summary>Current timestamp, for <see cref="RecordSince"/>.</summary>
        public static long Now => Stopwatch.GetTimestamp();

        public static double ElapsedMs(long startTimestamp, long endTimestamp) =>
            (endTimestamp - startTimestamp) * 1000.0 / Stopwatch.Frequency;

        /// <summary>When the samples were last reset.</summary>
        public DateTime SinceUtc
        {
            get { lock (_lock) return _sinceUtc; }
        }

        public void Record(string name, double milliseconds)
        {
            if (string.IsNullOrEmpty(name) || double.IsNaN(milliseconds) || milliseconds < 0)
                return;

            lock (_lock)
            {
                if (!_series.TryGetValue(name, out var series))
                {
                    series = new Series();
                    _series[name] = series;
                }

                series.Add(milliseconds);
            }
        }

        public void RecordSince(string name, long startTimestamp) =>
            Record(name, ElapsedMs(startTimestamp, Now));

        /// <summary>
        /// Starts timing an operation. <paramref name="id"/> tells apart operations of the same name
        /// that overlap (for example a phonebook search's resultId). Starting again replaces the start.
        /// </summary>
        public void Start(string name, string? id = null)
        {
            var now = Now;
            lock (_lock)
            {
                DropStalePending(now);
                _pending[PendingKey(name, id)] = now;
            }
        }

        /// <summary>
        /// Stops a started operation and records it, under <paramref name="recordAs"/> if given (for
        /// example a failed search). Returns false if nothing with that name and id was started.
        /// </summary>
        public bool Stop(string name, string? id = null, string? recordAs = null)
        {
            var now = Now;
            long start;
            lock (_lock)
            {
                var key = PendingKey(name, id);
                if (!_pending.TryGetValue(key, out start))
                    return false;

                _pending.Remove(key);
            }

            Record(recordAs ?? name, ElapsedMs(start, now));
            return true;
        }

        public bool IsPending(string name, string? id = null)
        {
            lock (_lock)
                return _pending.ContainsKey(PendingKey(name, id));
        }

        public void Reset()
        {
            lock (_lock)
            {
                _series.Clear();
                _pending.Clear();
                _sinceUtc = DateTime.UtcNow;
            }
        }

        public IReadOnlyList<TimingSummary> Summaries()
        {
            lock (_lock)
            {
                return _series
                    .OrderBy(s => s.Key, StringComparer.Ordinal)
                    .Select(s => s.Value.Summarize(s.Key))
                    .ToList();
            }
        }

        public TimingSummary? Summary(string name)
        {
            lock (_lock)
                return _series.TryGetValue(name, out var series) ? series.Summarize(name) : null;
        }

        /// <summary>A fixed-width table of every summary, for the console.</summary>
        public string FormatTable(string newLine = "\r\n")
        {
            var summaries = Summaries();
            if (summaries.Count == 0)
                return "No timings recorded." + newLine;

            var width = Math.Max(10, summaries.Max(s => s.Name.Length));
            var sb = new StringBuilder();
            sb.Append("name".PadRight(width))
                .Append("     count      last       min    median       p95       max  (ms)")
                .Append(newLine);

            foreach (var s in summaries)
            {
                sb.Append(s.Name.PadRight(width))
                    .Append(s.Count.ToString(CultureInfo.InvariantCulture).PadLeft(10))
                    .Append(Ms(s.LastMs)).Append(Ms(s.MinMs)).Append(Ms(s.MedianMs))
                    .Append(Ms(s.P95Ms)).Append(Ms(s.MaxMs))
                    .Append(newLine);
            }

            return sb.ToString();
        }

        private static string Ms(double value) =>
            value.ToString("0.0", CultureInfo.InvariantCulture).PadLeft(10);

        private static string PendingKey(string name, string? id) =>
            string.IsNullOrEmpty(id) ? name : name + "|" + id;

        private void DropStalePending(long now)
        {
            if (_pending.Count == 0)
                return;

            var maxAgeMs = PendingTimeout.TotalMilliseconds;
            foreach (var key in _pending.Where(p => ElapsedMs(p.Value, now) > maxAgeMs).Select(p => p.Key).ToList())
                _pending.Remove(key);

            // Ids that never complete (a search the codec never answers) must not pile up.
            while (_pending.Count >= MaxPending)
                _pending.Remove(_pending.OrderBy(p => p.Value).First().Key);
        }

        private sealed class Series
        {
            private readonly double[] _samples = new double[MaxSamples];
            private int _next;
            private int _retained;
            private long _count;
            private double _last;
            private double _min = double.MaxValue;
            private double _max;

            public void Add(double ms)
            {
                _samples[_next] = ms;
                _next = (_next + 1) % MaxSamples;
                if (_retained < MaxSamples)
                    _retained++;

                _count++;
                _last = ms;
                if (ms < _min) _min = ms;
                if (ms > _max) _max = ms;
            }

            public TimingSummary Summarize(string name)
            {
                var sorted = new double[_retained];
                Array.Copy(_samples, sorted, _retained);
                Array.Sort(sorted);

                return new TimingSummary(name, _count, _last, _min, Percentile(sorted, 0.5),
                    Percentile(sorted, 0.95), _max);
            }

            // Nearest-rank percentile over the retained samples.
            private static double Percentile(double[] sorted, double p)
            {
                if (sorted.Length == 0)
                    return 0;

                var rank = (int)Math.Ceiling(p * sorted.Length);
                return sorted[Math.Min(Math.Max(rank, 1), sorted.Length) - 1];
            }
        }
    }

    public sealed class TimingSummary
    {
        public TimingSummary(string name, long count, double lastMs, double minMs, double medianMs,
            double p95Ms, double maxMs)
        {
            Name = name;
            Count = count;
            LastMs = lastMs;
            MinMs = minMs;
            MedianMs = medianMs;
            P95Ms = p95Ms;
            MaxMs = maxMs;
        }

        public string Name { get; }

        /// <summary>All samples since the last reset (percentiles use the most recent <see cref="CodecTimings.MaxSamples"/>).</summary>
        public long Count { get; }

        public double LastMs { get; }
        public double MinMs { get; }
        public double MedianMs { get; }
        public double P95Ms { get; }
        public double MaxMs { get; }
    }
}
