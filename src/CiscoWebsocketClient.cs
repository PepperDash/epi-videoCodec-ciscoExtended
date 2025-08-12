using System;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronWebSocketClient;
using PepperDash.Core;
using PepperDash.Core.Logging;

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
                this.LogWarning("WebSocketClient key or Host is null or empty - failed to instantiate websocket client");
                return;
            }
            _config = controlConfig;

            if (string.IsNullOrEmpty(controlConfig.TcpSshProperties.Username) ||
                string.IsNullOrEmpty(controlConfig.TcpSshProperties.Password))
            {
                this.LogWarning("WebsocketClient has no login information - failed to instantiate websocked client");
                return;
            }

            Key = string.Format("{0}-{1}-websocket", key, _config.Method).ToLower();



            _client =
                new WebSocketClient()
                {
                    URL = string.Format("wss://{0}:{1}@{2}/ws", _config.TcpSshProperties.Username,
                    _config.TcpSshProperties.Password, _config.TcpSshProperties.Address),
                    KeepAlive = true,
                    ConnectionCallBack = WebsocketConnected,
                    DisconnectCallBack = WebsocketDisconnected,
                    ReceiveCallBack = WebsocketReceiveCallback
                };
            _client.ConnectAsync();
        }

        public int WebsocketConnected(WebSocketClient.WEBSOCKET_RESULT_CODES error)
        {
            this.LogError("Websocket Connected Result = {error}", error);
            return (int)error;
        }

        public int WebsocketDisconnected(WebSocketClient.WEBSOCKET_RESULT_CODES error, object item)
        {
            this.LogError("Websocket Disconnected Result = {error}", error);
            return (int)error;
        }

        public int WebsocketReceiveCallback(byte[] data, uint dataLen,
            WebSocketClient.WEBSOCKET_PACKET_TYPES opcode, WebSocketClient.WEBSOCKET_RESULT_CODES error)
        {
            if (opcode != WebSocketClient.WEBSOCKET_PACKET_TYPES.LWS_WS_OPCODE_07__TEXT_FRAME) return (int)error;
            var strData = Encoding.ASCII.GetString(data, 0, data.Length);
            this.LogDebug("Incoming Data Packet From Websocket: {data}", strData);
            return (int)error;
        }
    }





}