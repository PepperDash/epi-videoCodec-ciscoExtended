using Newtonsoft.Json;

namespace epi_videoCodec_ciscoExtended
{
    /// <summary>
    /// This class exists to capture serialized data sent back by a Cisco codec in JSON output mode
    /// </summary>
    public class CiscoCodecEvents
    {
        public class CauseValue
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class CauseType
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class CauseString
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class OrigCallDirection
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class RemoteUri
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class DisplayName
        {
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class CallId
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class CauseCode
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class CauseOrigin
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class Protocol
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class Duration
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class CallType
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class CallRate
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class Encryption
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class RequestedUri
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class PeopleCountAverage
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class CallDisconnect
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public CauseValue CauseValue { get; set; }
            public CauseType CauseType { get; set; }
            public CauseString CauseString { get; set; }
            public OrigCallDirection OrigCallDirection { get; set; }
            public RemoteUri RemoteUri { get; set; }
            public DisplayName DisplayName { get; set; }
            public CallId CallId { get; set; }
            public CauseCode CauseCode { get; set; }
            public CauseOrigin CauseOrigin { get; set; }
            public Protocol Protocol { get; set; }
            public Duration Duration { get; set; }
            public CallType CallType { get; set; }
            public CallRate CallRate { get; set; }
            public Encryption Encryption { get; set; }
            public RequestedUri RequestedUri { get; set; }
            public PeopleCountAverage PeopleCountAverage { get; set; }
        }
        public class UserInterface
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public Presentation Presentation { get; set; }
        }
        public class Presentation
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public ExternalSource ExternalSource { get; set; }
        }
        public class ExternalSource
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public Selected Selected { get; set; }
        }
        public class Selected
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public SourceIdentifier SourceIdentifier { get; set; }
        }
        public class SourceIdentifier
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }
        public class Event
        {
            public CallDisconnect CallDisconnect { get; set; }
            public UserInterface UserInterface { get; set; }
        }

        public class RootObject
        {
            public Event Event { get; set; }
        }
    }
}