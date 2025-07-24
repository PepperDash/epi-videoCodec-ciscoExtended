using Crestron.SimplSharp.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.CiscoCodecUserInterface.MobileControl
{
	public static class StringExtensions
	{
		public static string MaskQParamTokenInUrl(this string url)
		{
			if(string.IsNullOrEmpty(url) || !url.Contains("token=")) return url;
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
