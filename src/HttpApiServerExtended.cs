using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;

using PepperDash.Core;


namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    public class HttpApiServerExtended
    {
        public static Dictionary<string, string> ExtensionContentTypes;

        public event EventHandler<OnHttpRequestArgs> ApiRequest;
        public HttpServer HttpServer { get; private set; }

        public string HtmlRoot { get; set; }


        /// <summary>
        /// SIMPL+ can only execute the default constructor. If you have variables that require initialization, please
        /// use an Initialize method
        /// </summary>
        public HttpApiServerExtended()
        {
            ExtensionContentTypes = new Dictionary<string, string>
			{
				{ ".css", "text/css" },
				{ ".htm", "text/html" },
				{ ".html", "text/html" },
				{ ".jpg", "image/jpeg" },
				{ ".jpeg", "image/jpeg" },
				{ ".js", "application/javascript" },
				{ ".json", "application/json" },
                { ".xml", "text/xml" },
				{ ".map", "application/x-navimap" },
				{ ".pdf", "application.pdf" },
				{ ".png", "image/png" },
				{ ".txt", "text/plain" },
			};
            HtmlRoot = @"\HTML";
        }


        public void Start(int port)
        {
            // TEMP - this should be inserted by configuring class

            HttpServer = new HttpServer {ServerName = "Cisco API Servers", KeepAlive = true, Port = port};
            HttpServer.OnHttpRequest += Server_Request;
            HttpServer.Open();

            CrestronEnvironment.ProgramStatusEventHandler += a =>
            {
                if (a != eProgramStatusEventType.Stopping) return;
                HttpServer.Close();
                Debug.Console(1, "Shutting down HTTP Servers on port {0}", HttpServer.Port);
            };
        }

        void Server_Request(object sender, OnHttpRequestArgs args)
        {
            if (args.Request.Header.RequestType == "OPTIONS")
            {
                Debug.Console(2, "Asking for OPTIONS");
                args.Response.Header.SetHeaderValue("Access-Control-Allow-Origin", "*");
                args.Response.Header.SetHeaderValue("Access-Control-Allow-Methods", "GET, POST, PATCH, PUT, DELETE, OPTIONS");
                return;
            }

            var path = Uri.UnescapeDataString(args.Request.Path);
            var host = args.Request.DataConnection.RemoteEndPointAddress;
            //string authToken;

            Debug.Console(2, "HTTP Request: {2}: Path='{0}' ?'{1}'", path, args.Request.QueryString, host);

            // ----------------------------------- ADD AUTH HERE
            if (!path.StartsWith("/cisco/api")) return;
            var handler = ApiRequest;
            if (handler != null)
                handler(this, args);
        }

        public static string GetContentType(string extension)
        {
            var type = ExtensionContentTypes.ContainsKey(extension) ? ExtensionContentTypes[extension] : "text/plain";
            return type;
        }
    }

}