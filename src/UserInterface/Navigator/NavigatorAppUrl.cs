using System;
using System.Collections.Generic;
using System.Web;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Navigator
{
    /// <summary>
    /// Builds the Mobile Control app URL a Navigator loads.
    /// </summary>
    /// <remarks>
    /// Kept free of Crestron and Essentials dependencies so the test project can compile and run it
    /// directly.
    /// </remarks>
    public static class NavigatorAppUrl
    {
        /// <summary>
        /// Adds <paramref name="queryParams"/> to the query string of <paramref name="url"/>.
        /// Parameters the URL already has (such as the Mobile Control <c>token</c>) are never
        /// overwritten.
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
            var query = HttpUtility.ParseQueryString(builder.Query);
            var changed = false;

            foreach (var param in queryParams)
            {
                if (string.IsNullOrEmpty(param.Key) || query[param.Key] != null)
                {
                    continue;
                }

                query[param.Key] = param.Value ?? string.Empty;
                changed = true;
            }

            if (!changed)
            {
                return url;
            }

            builder.Query = query.ToString();
            return builder.Uri.AbsoluteUri;
        }
    }
}
