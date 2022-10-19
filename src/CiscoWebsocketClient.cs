using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronWebSocketClient;
using Crestron.SimplSharp.Net;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharp.Net.Https;
using Crestron.SimplSharpPro.DM.Endpoints;
using PepperDash.Core;
using RequestType = Crestron.SimplSharp.Net.Https.RequestType;

namespace epi_videoCodec_ciscoExtended
{
    public class CiscoWebsocketClient : IKeyed
    {
        public int IdTracker { get; private set; }
        private readonly HttpsClient _client;
        public string Key { get; private set; }
        private readonly CrestronQueue<Action> _requestQueue = new CrestronQueue<Action>(20);
        private readonly ControlPropertiesConfig _config;

        public CiscoWebsocketClient(string key, ControlPropertiesConfig controlConfig) 
        {
            if (string.IsNullOrEmpty(key) || controlConfig == null)
            {
                Debug.Console(0, "WebSocketClient key or Host is null or empty - failed to instantiate websocket client" );
                return;
            }
            _config = controlConfig;

            if (string.IsNullOrEmpty(controlConfig.TcpSshProperties.Username) ||
                string.IsNullOrEmpty(controlConfig.TcpSshProperties.Password))
            {
                Debug.Console(0, "WebsocketClient has no login information - failed to instantiate websocked client");
                return;
            }

            Key = string.Format("{0}-{1}-websocket", key, _config.Method).ToLower();

            _client = new HttpsClient
            {
                IncludeHeaders = true,
            };
        }

        /*
        public void SendRequest(CiscoCodecJsonRpc data)
        {
            var request = new HttpsClientRequest
            {
                RequestType = RequestType.Get,
                Url = new UrlParser(string.Format("wss://{0}/ws", _config.TcpSshProperties.Address)),
                
            };
            request.KeepAlive = true;
            request.Header.SetHeaderValue("Authorization", String.Format("Basic {0}", 
                String.Format("{0}:{1}", _config.TcpSshProperties.Username, _config.TcpSshProperties.Password)
                .EncodeBase64()));
            request.Header.SetHeaderValue("Connection", "Upgrader");
            request.Header.SetHeaderValue("Upgrade", "websocket");
            request.Header.
        }
         * */
    }





}