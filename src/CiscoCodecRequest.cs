using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace epi_videoCodec_ciscoExtended
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