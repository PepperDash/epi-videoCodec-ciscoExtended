using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Navigator
{
    /// <summary>
    /// Builds the Mobile Control app URL a Navigator loads.
    /// </summary>
    /// <remarks>
    /// Kept free of Crestron and Essentials dependencies so the test project can compile and run it
    /// directly. It must not use <c>System.Web.HttpUtility</c> either: the Essentials plugin loader
    /// rejects any plugin that references it.
    /// </remarks>
    public static class NavigatorAppUrl
    {
        /// <summary>
        /// Adds <paramref name="queryParams"/> to the query string of <paramref name="url"/>.
        /// Parameters the URL already has (such as the Mobile Control <c>token</c>) are never
        /// overwritten, and the existing query string is kept exactly as it was.
        /// </summary>
        /// <param name="url">An absolute http(s) URL. Anything else is returned unchanged.</param>
        /// <param name="queryParams">Parameters to add. Null or empty returns the URL unchanged.</param>
        /// <returns>The URL with the parameters added.</returns>
        public static string AddQueryParams(string url, IDictionary<string, string> queryParams)
        {
            if (string.IsNullOrEmpty(url) || queryParams == null || queryParams.Count == 0)
            {
                return url;
            }

            // Require an http(s) scheme: on Linux a bare "/path" parses as an absolute file:// URI.
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return url;
            }

            var builder = new UriBuilder(uri);
            var query = builder.Query.TrimStart('?');
            var existingKeys = new HashSet<string>(ParseKeys(query), StringComparer.Ordinal);

            var added = new StringBuilder();
            foreach (var param in queryParams)
            {
                if (string.IsNullOrEmpty(param.Key) || !existingKeys.Add(param.Key))
                {
                    continue;
                }

                if (added.Length > 0) added.Append('&');
                added.Append(Uri.EscapeDataString(param.Key))
                    .Append('=')
                    .Append(Uri.EscapeDataString(param.Value ?? string.Empty));
            }

            if (added.Length == 0)
            {
                return url;
            }

            builder.Query = query.Length > 0 ? query + "&" + added : added.ToString();
            return builder.Uri.AbsoluteUri;
        }

        /// <summary>The (decoded) parameter names in a query string, without its leading '?'.</summary>
        private static IEnumerable<string> ParseKeys(string query) =>
            query.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(pair =>
                {
                    var key = pair.Split(new[] { '=' }, 2)[0];
                    return Uri.UnescapeDataString(key.Replace('+', ' '));
                });
    }
}
