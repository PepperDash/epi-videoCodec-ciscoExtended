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

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    public class CiscoWebsocketClient : IKeyed
    {
        public int IdTracker { get; private set; }
        private readonly WebSocketClient _client;
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



            _client =
                new WebSocketClient()
                {
                    URL = String.Format("wss://{0}:{1}@{2}/ws", _config.TcpSshProperties.Username,
                    _config.TcpSshProperties.Password, _config.TcpSshProperties.Address),
                    KeepAlive = true,
                    ConnectionCallBack =  WebsocketConnected,
                    DisconnectCallBack = WebsocketDisconnected,
                    ReceiveCallBack = WebsocketReceiveCallback
                };
            _client.ConnectAsync();
        }

        public int WebsocketConnected(WebSocketClient.WEBSOCKET_RESULT_CODES error)
        {
            Debug.Console(1, this, "Websocket Connected Result = {0}", error);
            return (int)error;
        }

        public int WebsocketDisconnected(WebSocketClient.WEBSOCKET_RESULT_CODES error, object item)
        {
            Debug.Console(1, this, "Websocket Disconnected Result = {0}", error);
            return (int)error;
        }

        public int WebsocketReceiveCallback(byte[] data, uint dataLen, 
            WebSocketClient.WEBSOCKET_PACKET_TYPES opcode, WebSocketClient.WEBSOCKET_RESULT_CODES error)
        {
            if (opcode != WebSocketClient.WEBSOCKET_PACKET_TYPES.LWS_WS_OPCODE_07__TEXT_FRAME) return (int) error;
            var strData = Encoding.ASCII.GetString(data, 0, data.Length);
            Debug.Console(0, this, "Incoming Data Packet From Websocket");
            Debug.Console(0, this, "{0}", strData);
            return (int) error;
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