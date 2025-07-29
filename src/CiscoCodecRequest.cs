using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{

    public class CiscoCodecJsonRpc
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; private set; }

        [JsonProperty("id")]
        public int Id { get; private set; }

        [JsonProperty("method")]
        public string Method { get; private set; }

        [JsonProperty("params")]
        public JToken Params { get; private set; }

        public CiscoCodecJsonRpc(string method, JToken parameters)
        {
            JsonRpc = "2.0";
            Method = method;
            Params = parameters;
        }

        public void SetId(int id)
        {
            Id = id;
        }
    }
}