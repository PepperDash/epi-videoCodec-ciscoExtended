using System;
using Crestron.SimplSharp.Net;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Navigator
{
	public static class StringExtensions
	{
		public static string MaskQParamTokenInUrl(this string url)
		{
			if (string.IsNullOrEmpty(url) || !url.Contains("token=")) return url;
			var uriBuilder = new UriBuilder(url);
			var query = HttpUtility.ParseQueryString(uriBuilder.Query);
			string token = query["token"];
			if (token != null)
			{
				query["token"] = new string('*', token.Length);
			}
			uriBuilder.Query = query.ToString();
			return uriBuilder.ToString();
		}
	}
}
