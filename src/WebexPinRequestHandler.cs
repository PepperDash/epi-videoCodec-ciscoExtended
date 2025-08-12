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

        private readonly IKeyed _parent;
        private readonly IBasicCommunication _coms;
        private readonly GenericQueue _handler;

        private int _authRequestedCallInstance;
        private bool _authRequested;
        private string _hostPin;
        public readonly BoolFeedback AuthRequested;
        public readonly BoolFeedbackPulse JoinedAsGuest;
        public readonly BoolFeedbackPulse JoinedAsHost;
        public readonly BoolFeedbackPulse PinIncorrect;
        public readonly IntFeedback AuthRequestedCallInstance;

        public WebexPinRequestHandler(IKeyed parent, IBasicCommunication coms, GenericQueue handler)
        {
            _parent = parent;
            _coms = coms;
            _handler = handler;
            AuthRequested = new BoolFeedback(() => _authRequested);
            JoinedAsGuest = new BoolFeedbackPulse(25, true);
            JoinedAsHost = new BoolFeedbackPulse(25, true);
            PinIncorrect = new BoolFeedbackPulse(25, true);
            AuthRequestedCallInstance = new IntFeedback(() => _authRequestedCallInstance);
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
            var request = token.ToObject<AuthenticationRequestObject>();
            if (request.Call == null)
                return;

            var authRequest = request.Call.FirstOrDefault(x => x.AuthenticationRequest != null);
            if (authRequest != null)
            {
                _authRequested = authRequest.AuthenticationRequest.Value != "None";
                _authRequestedCallInstance = request.Call.IndexOf(authRequest);
                _parent.LogInformation("Auth Requested Call Instance: {instance} | {value}", _authRequestedCallInstance,
                    authRequest.AuthenticationRequest.Value);
                AuthRequestedCallInstance.FireUpdate();
                AuthRequested.FireUpdate();
            }
        }

        public void ParseAuthenticationResponse(JToken token)
        {
            var request = token.ToObject<AuthenticationResponseObject>();
            if (request.Call == null || request.Call.AuthenticationResponse == null)
                return;

            var pinEntered = request.Call.AuthenticationResponse.PinEntered;

            if (pinEntered != null)
            {
                var role = request.Call.AuthenticationResponse.PinEntered.ParticipantRole.Value;
                if (!string.IsNullOrEmpty(role) && role == "Guest")
                {
                    JoinedAsGuest.Start();
                    _hostPin = string.Empty;
                    _parent.LogInformation("Joined as {instance} | {role}", _authRequestedCallInstance, role);
                    return;
                }

                if (!string.IsNullOrEmpty(role) && role == "Host")
                {
                    JoinedAsHost.Start();
                    _hostPin = string.Empty;
                    _parent.LogInformation("Joined as {instance} | {role}", _authRequestedCallInstance, role);
                    return;
                }
            }

            var pinError = request.Call.AuthenticationResponse.PinError;
            if (pinError != null)
            {
                PinIncorrect.Start();
                _hostPin = string.Empty;
                _parent.LogError("Pin error | {instance}", _authRequestedCallInstance);
            }
        }

        public void JoinAsGuest()
        {
            const string commandFormat = "xCommand Conference Call AuthenticationResponse CallId: {0} ParticipantRole: Guest{1}\x0D\x0A";
            var command = string.Format(commandFormat, _authRequestedCallInstance, string.IsNullOrEmpty(_hostPin) ? string.Empty : string.Format(" Pin: {0}#", _hostPin));
            _coms.SendText(command);
        }

        public void JoinAsHost()
        {
            const string commandFormat = "xCommand Conference Call AuthenticationResponse CallId: {0} ParticipantRole: Host Pin: {1}#\x0D\x0A";
            var command = string.Format(commandFormat, _authRequestedCallInstance, _hostPin);
            _coms.SendText(command);
        }

        public void LinkToApi(BasicTriList trilist, CiscoCodecJoinMap joinMap)
        {
            trilist.SetStringSigAction(joinMap.WebexSendPin.JoinNumber, s => _hostPin = s);
            trilist.SetSigTrueAction(joinMap.WebexPinClear.JoinNumber, () => _hostPin = string.Empty);
            trilist.SetSigTrueAction(joinMap.WebexSendPin.JoinNumber, JoinAsHost);
            trilist.SetSigTrueAction(joinMap.WebexJoinAsGuest.JoinNumber, JoinAsGuest);

            AuthRequested.LinkInputSig(trilist.BooleanInput[joinMap.WebexPinRequested.JoinNumber]);
            JoinedAsGuest.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.WebexJoinedAsGuest.JoinNumber]);
            JoinedAsHost.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.WebexJoinedAsHost.JoinNumber]);
            PinIncorrect.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.WebexPinError.JoinNumber]);
        }
    }
}