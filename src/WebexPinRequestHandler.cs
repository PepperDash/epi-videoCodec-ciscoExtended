using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace epi_videoCodec_ciscoExtended
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

        public enum AuthRequestedTypeEnum
        {
            None,
            HostPinOrGuest,
            HostPinOrGuestPin,
            AnyHostPinOrGuestPin,
            PanelistPin,
            PanelistPinOrAttendeePin,
            PanelistPinOrAttendee,
            GuestPin
        }

        private readonly IKeyed _parent;
        private readonly IBasicCommunication _coms;

        private int _authRequestedCallInstance;
        private string _pin;
        private AuthRequestedTypeEnum _currentTypeEnum;

        public readonly BoolFeedbackPulse HostPinRequested;
        public readonly BoolFeedbackPulse PanelistPinRequested;
        public readonly BoolFeedbackPulse JoinedAsGuest;
        public readonly BoolFeedbackPulse JoinedAsHost;
        public readonly BoolFeedbackPulse JoinedAsPanelist;
        public readonly BoolFeedbackPulse PinIncorrect;
        public readonly IntFeedback AuthRequestedCallInstance;

        public WebexPinRequestHandler(CiscoCodec parent, IBasicCommunication coms)
        {
            _parent = parent;
            _coms = coms;
            HostPinRequested = new BoolFeedbackPulse(25, true);
            PanelistPinRequested = new BoolFeedbackPulse(25, true);

            JoinedAsGuest = new BoolFeedbackPulse(25, true);
            JoinedAsPanelist = new BoolFeedbackPulse(25, true);
            JoinedAsHost = new BoolFeedbackPulse(25, true);
            PinIncorrect = new BoolFeedbackPulse(25, true);
            AuthRequestedCallInstance = new IntFeedback(() => _authRequestedCallInstance);
        }

        public void ParseAuthenticationRequest(JToken token)
        {
            try
            {
                var request = token.ToObject<AuthenticationRequestObject>();
                if (request.Call == null)
                    return;

                var authRequest = request.Call.FirstOrDefault(x => x.AuthenticationRequest != null);
                if (authRequest != null)
                {
                    _currentTypeEnum = (AuthRequestedTypeEnum)Enum.Parse(typeof(AuthRequestedTypeEnum), authRequest.AuthenticationRequest.Value, true);

                    _authRequestedCallInstance = request.Call.IndexOf(authRequest);
                    Debug.Console(0, _parent, "Auth Requested Call Instance:{0} | {1}",
                        _authRequestedCallInstance,
                        _currentTypeEnum);

                    AuthRequestedCallInstance.FireUpdate();
                    switch (_currentTypeEnum)
                    {
                        case AuthRequestedTypeEnum.None:
                            break;
                        case AuthRequestedTypeEnum.HostPinOrGuest:
                            HostPinRequested.Start();
                            break;
                        case AuthRequestedTypeEnum.HostPinOrGuestPin:
                            HostPinRequested.Start();
                            break;
                        case AuthRequestedTypeEnum.AnyHostPinOrGuestPin:
                            HostPinRequested.Start();
                            break;
                        case AuthRequestedTypeEnum.PanelistPin:
                            PanelistPinRequested.Start();
                            break;
                        case AuthRequestedTypeEnum.PanelistPinOrAttendeePin:
                            PanelistPinRequested.Start();
                            break;
                        case AuthRequestedTypeEnum.PanelistPinOrAttendee:
                            PanelistPinRequested.Start();
                            break;
                        case AuthRequestedTypeEnum.GuestPin:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Console(1, "Caught an exception parsing an Authentication Request:{0}", ex);
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
                if (!String.IsNullOrEmpty(role) && role == "Guest")
                {
                    JoinedAsGuest.Start();
                    _pin = string.Empty;
                    Debug.Console(0, _parent, "Joined as {1}", _authRequestedCallInstance, role);
                    _currentTypeEnum = AuthRequestedTypeEnum.None;
                    return;
                }

                if (!String.IsNullOrEmpty(role) && role == "Host")
                {
                    JoinedAsHost.Start();
                    _pin = string.Empty;
                    Debug.Console(0, _parent, "Joined as {1}", _authRequestedCallInstance, role);
                    _currentTypeEnum = AuthRequestedTypeEnum.None;
                    return;
                }

                if (!String.IsNullOrEmpty(role) && role == "Panelist")
                {
                    JoinedAsHost.Start();
                    _pin = string.Empty;
                    Debug.Console(0, _parent, "Joined as {1}", _authRequestedCallInstance, role);
                    _currentTypeEnum = AuthRequestedTypeEnum.None;
                    return;
                }
            }

            var pinError = request.Call.AuthenticationResponse.PinError;
            if (pinError != null)
            {
                PinIncorrect.Start();
                _pin = string.Empty;
                Debug.Console(0, _parent, "Pin error", _authRequestedCallInstance);
            }
        }

        public void JoinAsGuest()
        {
            const string commandFormat = "xCommand Conference Call AuthenticationResponse CallId: {0} ParticipantRole: Guest{1}\x0D\x0A";
            var command = String.Format(commandFormat, _authRequestedCallInstance, String.IsNullOrEmpty(_pin) ? String.Empty : String.Format(" Pin: {0}#", _hostPin));
            _coms.SendText(command);
        }

        public void JoinAsHost()
        {
            const string commandFormat = "xCommand Conference Call AuthenticationResponse CallId: {0} ParticipantRole: Host Pin: {1}#\x0D\x0A";
            var command = String.Format(commandFormat, _authRequestedCallInstance, _pin);
            _coms.SendText(command);
        }

        public void JoinAsPanelist()
        {
            const string commandFormat = "xCommand Conference Call AuthenticationResponse CallId: {0} ParticipantRole: Panelist Pin: {1}#\x0D\x0A";
            var command = String.Format(commandFormat, _authRequestedCallInstance, _pin);
            _coms.SendText(command);
        }

        public void LinkToApi(BasicTriList trilist, CiscoCodecJoinMap joinMap)
        {
            trilist.SetStringSigAction(joinMap.SetWebexPin.JoinNumber, s => _pin = s);
            trilist.SetSigTrueAction(joinMap.WebexPinClear.JoinNumber, () => _pin = string.Empty);
            trilist.SetSigTrueAction(joinMap.WebexJoinAsHost.JoinNumber, JoinAsHost);
            trilist.SetSigTrueAction(joinMap.WebexJoinAsGuest.JoinNumber, JoinAsGuest);
            trilist.SetSigTrueAction(joinMap.WebexJoinAsPanelist.JoinNumber, JoinAsPanelist);

            HostPinRequested.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.WebexPinRequested.JoinNumber]);
            PanelistPinRequested.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.WebexWebinarPinRequested.JoinNumber]);
            JoinedAsGuest.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.WebexJoinedAsGuest.JoinNumber]);
            JoinedAsHost.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.WebexJoinedAsHost.JoinNumber]);
            PinIncorrect.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.WebexPinError.JoinNumber]);
        }
    }
}