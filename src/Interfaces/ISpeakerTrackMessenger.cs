using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.AppServer.Messengers;
using PepperDash.Essentials.Devices.Common.Codec.Cisco;


namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Interfaces
{
    public class ISpeakerTrackMessenger : MessengerBase
    {
        private readonly ISpeakerTrack _speakerTrack;

        public ISpeakerTrackMessenger(string key, string messagePath, ISpeakerTrack device)
            : base(key, messagePath, device as Device)
        {
            _speakerTrack = device;
        }

        protected override void RegisterActions()
        {
            if (_speakerTrack == null)
            {

                this.LogError("{Key} does not implement ISpeakerTrack", _speakerTrack.Key);
                return;
            }

            AddAction("/fullStatus", (id, content) => SendFullStatus());

            AddAction($"/speakerTrackOn", (id, context) => _speakerTrack.SpeakerTrackOn());

            AddAction($"/speakerTrackOff", (id, context) => _speakerTrack.SpeakerTrackOff());

            _speakerTrack.SpeakerTrackAvailableFeedback.OutputChange += (o, a) => SendFullStatus();
        }

        private void SendFullStatus()
        {
            var message = new ISpeakerTrackStateMessage
            {
                SpeakerTrackAvailability = _speakerTrack.SpeakerTrackAvailability,
                SpeakerTrackStatus = _speakerTrack.SpeakerTrackStatus
            };

            PostStatusMessage(message);
        }
    }

    public class ISpeakerTrackStateMessage : DeviceStateMessageBase
    {
        [JsonProperty("speakerTrackAvailability", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool? SpeakerTrackAvailability { get; set; }

        [JsonProperty("speakerTrackStatus", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool? SpeakerTrackStatus { get; set; }

    }
}
