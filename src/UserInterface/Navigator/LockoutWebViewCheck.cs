using System;
using System.Collections.Generic;
using System.Linq;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Navigator
{
    /// <summary>Whether a web view status shows a given page (the lockout or the in-progress overlay).</summary>
    public enum LockoutWebViewState
    {
        /// <summary>A visible web view on the expected target shows the page.</summary>
        Showing,

        /// <summary>A full status (every entry has its URL) and no visible web view on the expected target shows the page.</summary>
        NotShowing,

        /// <summary>
        /// The message can't tell: partial feedback (an entry without its URL). Never act on it; the next
        /// full status reply decides.
        /// </summary>
        Unknown,
    }

    /// <summary>
    /// Decides whether a navigator is actually showing a page (the lockout, or the combining overlay that
    /// uses the same path). The codec can list several web views at once (a page left open, or one it
    /// still reports as Visible after the navigator stopped showing it), so "a web view is visible" is
    /// not the same as "this page is visible".
    /// </summary>
    /// <remarks>Kept free of Crestron types so it can be unit tested.</remarks>
    public static class LockoutWebViewCheck
    {
        /// <param name="views">
        /// The web views one status message lists: status ("Visible", "NotVisible" or null), target
        /// ("Controller", "OSD", "PersistentWebApp", or null when the firmware doesn't report it) and URL
        /// (null in partial feedback)
        /// </param>
        /// <param name="path">The page's mobile-control path as configured, e.g. "/lockout"</param>
        /// <param name="target">The target the page is displayed on; null or empty means "Controller"</param>
        public static LockoutWebViewState Evaluate(IEnumerable<(string Status, string Target, string Url)> views, string path, string target = null)
        {
            if (views == null || string.IsNullOrWhiteSpace(path)) return LockoutWebViewState.Unknown;

            var list = views.ToList();

            // Partial feedback lists only what changed, without URLs: it can't show what's on screen.
            if (list.Any(v => v.Url == null)) return LockoutWebViewState.Unknown;

            // The configured path is appended to the app URL as is (escaped into the URL path), so it's
            // compared decoded, query included, without adding a slash.
            var wantedPath = Decode(path.Trim()).TrimEnd('/');
            if (wantedPath.Trim('/').Length == 0) return LockoutWebViewState.Unknown; // "/" would match any URL
            var wantedTarget = TargetOrDefault(target);

            // Shown = listed with a status other than NotVisible (a page still loading counts, so it
            // isn't pushed again mid-load).
            return list.Any(v => v.Status != null
                    && !string.Equals(v.Status, "NotVisible", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(TargetOrDefault(v.Target), wantedTarget, StringComparison.OrdinalIgnoreCase)
                    && EndsWithPath(UrlPath(v.Url), wantedPath))
                ? LockoutWebViewState.Showing
                : LockoutWebViewState.NotShowing;
        }

        private static string TargetOrDefault(string target) =>
            string.IsNullOrWhiteSpace(target) ? "Controller" : target.Trim();

        // ".../mc/app/lockout?token=..." -> ".../mc/app/lockout": the URL's own query cut first (a query
        // in the configured path was escaped into the path), then decoded, no trailing slash.
        private static string UrlPath(string url)
        {
            var s = url.Trim();
            var end = s.IndexOfAny(new[] { '?', '#' });
            if (end >= 0) s = s.Substring(0, end);
            return Decode(s).TrimEnd('/');
        }

        // A configured path starting with "/" carries its own segment boundary ("/lockout" doesn't match
        // ".../combinationlockout"); one without is appended to the app URL with no slash, so neither
        // does the URL.
        private static bool EndsWithPath(string urlPath, string wantedPath) =>
            urlPath.EndsWith(wantedPath, StringComparison.OrdinalIgnoreCase);

        private static string Decode(string s)
        {
            try { return Uri.UnescapeDataString(s); }
            catch (UriFormatException) { return s; }
        }
    }

    /// <summary>
    /// Limits lockout re-pushes made because the status shows no lockout: the first few go out as usual,
    /// then at most one per <see cref="BackoffInterval"/> until the lockout is seen. Guards against a
    /// reload loop if the codec reports the lockout under a URL the check doesn't match.
    /// </summary>
    /// <remarks>Kept free of Crestron types so it can be unit tested. Thread-safe.</remarks>
    public sealed class LockoutRepairThrottle
    {
        public const int FreeAttempts = 3;
        public static readonly TimeSpan BackoffInterval = TimeSpan.FromSeconds(60);

        private readonly object sync = new object();
        private int attempts;
        private DateTime lastAttemptUtc = DateTime.MinValue;

        /// <summary>The number of repair pushes since the lockout was last seen.</summary>
        public int Attempts { get { lock (sync) return attempts; } }

        /// <returns>true if a repair push may go out now. Doesn't count it: call <see cref="Record"/> once it has.</returns>
        public bool CanAttempt(DateTime nowUtc)
        {
            lock (sync) return attempts < FreeAttempts || nowUtc - lastAttemptUtc >= BackoffInterval;
        }

        /// <summary>Counts a repair push that went out.</summary>
        /// <returns>the number of repair pushes since the lockout was last seen, including this one</returns>
        public int Record(DateTime nowUtc)
        {
            lock (sync)
            {
                attempts++;
                lastAttemptUtc = nowUtc;
                return attempts;
            }
        }

        /// <summary>The lockout was seen, or the lockout ended: start counting again.</summary>
        public void Reset()
        {
            lock (sync)
            {
                attempts = 0;
                lastAttemptUtc = DateTime.MinValue;
            }
        }
    }
}
