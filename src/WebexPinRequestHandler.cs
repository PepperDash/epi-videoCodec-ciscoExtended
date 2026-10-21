using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Queues;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    public class WebexPinRequestHandler
    {
        public class AuthenticationRequestObject
        {
            [JsonProperty("Call")]
            public List<AuthenticationRequestResponseCall> Call { get; set; }
        }

        public class AuthenticationResponseObject
        {
            [JsonProperty("Call")]
            public AuthenticationRequestResponseCall Call { get; set; }
        }

        public class AuthenticationRequestResponseCall
        {
            [JsonProperty("AuthenticationRequest")]
            public AuthenticationRequest AuthenticationRequest { get; set; }

            [JsonProperty("AuthenticationResponse")]
            public AuthenticationResponse AuthenticationResponse { get; set; }

            [JsonProperty("id")]
            public long Id { get; set; }
        }

        public class AuthenticationRequest
        {
            [JsonProperty("Value")]
            public string Value { get; set; }
        }

        public class AuthenticationResponse
        {
            [JsonProperty("PinEntered")]
            public PinEntered PinEntered { get; set; }

            [JsonProperty("PinError")]
            public PinError PinError { get; set; }

            [JsonProperty("id")]
            public long Id { get; set; }
        }

        public class PinEntered
        {
            [JsonProperty("AuthenticatingPin")]
            public AuthenticatingPin AuthenticatingPin { get; set; }

            [JsonProperty("CallId")]
            public AuthenticatingPin CallId { get; set; }

            [JsonProperty("NumDigitsEntered")]
            public AuthenticatingPin NumDigitsEntered { get; set; }

            [JsonProperty("ParticipantRole")]
            public AuthenticatingPin ParticipantRole { get; set; }

            [JsonProperty("id")]
            public long Id { get; set; }
        }

        public class AuthenticatingPin
        {
            [JsonProperty("Value")]
            public string Value { get; set; }

            [JsonProperty("id")]
            public long Id { get; set; }
        }


        public class PinError
        {
            [JsonProperty("CallId")]
            public CallId CallId { get; set; }

            [JsonProperty("id")]
            public long Id { get; set; }
        }

        public class CallId
        {
            [JsonProperty("Value")]
            public long Value { get; set; }

            [JsonProperty("id")]
            public long Id { get; set; }
        }

        private readonly CiscoCodec _parent;
        private readonly IBasicCommunication _coms;
        private readonly GenericQueue _handler;

        // The codec call id of the call asking for a PIN (#111: was its list index, so every reply
        // targeted CallId 0 and the codec never admitted the call, which is also why #42's "joined as"
        // feedback never fired).
        private string _authRequestedCallId = string.Empty;
        private string _authRequestType = WebexAuthentication.None;
        private bool _authRequested;
        private string _hostPin;
        public readonly BoolFeedback AuthRequested;
        public readonly BoolFeedback WebinarPinRequested;
        public readonly StringFeedback AuthRequestType;
        public readonly BoolFeedbackPulse JoinedAsGuest;
        public readonly BoolFeedbackPulse JoinedAsHost;
        public readonly BoolFeedbackPulse JoinedAsPanelist;
        public readonly BoolFeedbackPulse JoinedAsAttendee;
        public readonly BoolFeedbackPulse PinIncorrect;
        public readonly IntFeedback AuthRequestedCallInstance;

        public WebexPinRequestHandler(CiscoCodec parent, IBasicCommunication coms, GenericQueue handler)
        {
            _parent = parent;
            _coms = coms;
            _handler = handler;
            AuthRequested = new BoolFeedback(() => _authRequested);
            WebinarPinRequested = new BoolFeedback(() => _authRequested && WebexAuthentication.IsWebinar(_authRequestType));
            AuthRequestType = new StringFeedback(() => _authRequested ? _authRequestType : string.Empty);
            JoinedAsGuest = new BoolFeedbackPulse(25, true);
            JoinedAsHost = new BoolFeedbackPulse(25, true);
            JoinedAsPanelist = new BoolFeedbackPulse(25, true);
            JoinedAsAttendee = new BoolFeedbackPulse(25, true);
            PinIncorrect = new BoolFeedbackPulse(25, true);
            AuthRequestedCallInstance = new IntFeedback(() => int.TryParse(_authRequestedCallId, out var id) ? id : 0);
        }

        class ProcessActionMethod : IQueueMessage
        {
            private readonly Action _action;

            public ProcessActionMethod(Action action)
            {
                _action = action;
            }

            public void Dispatch()
            {
                _action();
            }
        }

        public void ParseAuthenticationRequest(JToken token)
        {
            var request = WebexAuthentication.ReadRequest(token);
            if (request == null)
                return;

            _authRequestedCallId = request.Value.CallId;
            _authRequestType = request.Value.RequestType;
            _authRequested = WebexAuthentication.IsRequested(_authRequestType);
            _parent.LogInformation("Webex PIN request: call {callId} | {type}", _authRequestedCallId, _authRequestType);
            AuthRequestedCallInstance.FireUpdate();
            AuthRequestType.FireUpdate();
            AuthRequested.FireUpdate();
            WebinarPinRequested.FireUpdate();
        }

        public void ParseAuthenticationResponse(JToken token)
        {
            var result = WebexAuthentication.ReadResponse(token);
            if (result == null)
                return;

            _hostPin = string.Empty;
            switch (result)
            {
                case "PinError":
                    PinIncorrect.Start();
                    _parent.LogError("Webex PIN error | call {callId}", _authRequestedCallId);
                    return;
                case "Host": JoinedAsHost.Start(); break;
                case "Guest": JoinedAsGuest.Start(); break;
                case "Panelist": JoinedAsPanelist.Start(); break;
                case "Attendee": JoinedAsAttendee.Start(); break;
            }
            _parent.LogInformation("Joined as {role} | call {callId}", result, _authRequestedCallId);
        }

        private void Respond(string role, bool withPin)
        {
            if (string.IsNullOrEmpty(_authRequestedCallId))
            {
                _parent.LogWarning("Webex PIN: no call is asking for a PIN; not sending {role}", role);
                return;
            }
            _parent.SendText(WebexAuthentication.ResponseCommand(_authRequestedCallId, role, withPin ? _hostPin : null));
        }

        public void JoinAsGuest() => Respond("Guest", true);

        public void JoinAsHost() => Respond("Host", true);

        /// <summary>Webinar: joins as a panelist with the PIN set on WebexSendPin (#111).</summary>
        public void JoinAsPanelist() => Respond("Panelist", true);

        /// <summary>Webinar: joins as an attendee, with the PIN when one was set (#111).</summary>
        public void JoinAsAttendee() => Respond("Attendee", true);

        public void LinkToApi(BasicTriList trilist, CiscoCodecJoinMap joinMap)
        {
            trilist.SetStringSigAction(joinMap.WebexSendPin.JoinNumber, s => _hostPin = s);
            trilist.SetSigTrueAction(joinMap.WebexPinClear.JoinNumber, () => _hostPin = string.Empty);
            trilist.SetSigTrueAction(joinMap.WebexSendPin.JoinNumber, JoinAsHost);
            trilist.SetSigTrueAction(joinMap.WebexJoinAsGuest.JoinNumber, JoinAsGuest);
            trilist.SetSigTrueAction(joinMap.WebexJoinAsPanelist.JoinNumber, JoinAsPanelist);
            trilist.SetSigTrueAction(joinMap.WebexJoinAsAttendee.JoinNumber, JoinAsAttendee);

            AuthRequested.LinkInputSig(trilist.BooleanInput[joinMap.WebexPinRequested.JoinNumber]);
            WebinarPinRequested.LinkInputSig(trilist.BooleanInput[joinMap.WebexWebinarPinRequested.JoinNumber]);
            AuthRequestType.LinkInputSig(trilist.StringInput[joinMap.WebexPinRequestType.JoinNumber]);
            JoinedAsGuest.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.WebexJoinedAsGuest.JoinNumber]);
            JoinedAsHost.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.WebexJoinedAsHost.JoinNumber]);
            JoinedAsPanelist.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.WebexJoinedAsPanelist.JoinNumber]);
            JoinedAsAttendee.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.WebexJoinedAsAttendee.JoinNumber]);
            PinIncorrect.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.WebexPinError.JoinNumber]);
        }
    }
}