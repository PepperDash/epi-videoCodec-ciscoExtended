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
        public enum AuthRequestedTypeEnum
        {
            None,
            HostPinOrGuest,
            HostPinOrGuestPin,
            AnyHostPinOrGuestPin,
            PanelistPin,
            PanelistPinOrAttendeePin,
            PanelistPinOrAttendee,
            GuestPin,
        }

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
        private int _authRequestedCallInstance;
        private string _pin;
        private AuthRequestedTypeEnum _currentRequestType = AuthRequestedTypeEnum.None;

        public readonly BoolFeedback AuthRequested;
        public readonly BoolFeedbackPulse JoinedAsGuest;
        public readonly BoolFeedbackPulse JoinedAsHost;
        public readonly BoolFeedbackPulse JoinedAsPanelist;
        public readonly BoolFeedbackPulse JoinedAsAttendee;
        public readonly BoolFeedbackPulse HostPinOrGuestRequested;
        public readonly BoolFeedbackPulse PanelistPinOrAttendeeRequested;
        public readonly BoolFeedbackPulse PinIncorrect;
        public readonly BoolFeedbackPulse AuthNonePulse;
        public readonly IntFeedback AuthRequestedCallInstance;

        public WebexPinRequestHandler(IKeyed parent, IBasicCommunication coms)
        {
            _parent = parent;
            _coms = coms;

            AuthRequested = new BoolFeedback(() => _currentRequestType != AuthRequestedTypeEnum.None);

            JoinedAsGuest = new BoolFeedbackPulse(25, true);
            JoinedAsHost = new BoolFeedbackPulse(25, true);
            PinIncorrect = new BoolFeedbackPulse(25, true);
            JoinedAsPanelist = new BoolFeedbackPulse(25, true);
            JoinedAsAttendee = new BoolFeedbackPulse(25, true);
            AuthNonePulse = new BoolFeedbackPulse(25, true);
            HostPinOrGuestRequested = new BoolFeedbackPulse(25, true);
            PanelistPinOrAttendeeRequested = new BoolFeedbackPulse(25, true);
            AuthRequestedCallInstance = new IntFeedback(() => _authRequestedCallInstance);

            AuthRequested.OutputChange += (sender, args) =>
                                          {
                                              if (sender is BoolFeedback 
                                                  && _currentRequestType == AuthRequestedTypeEnum.None)
                                              {
                                                  AuthNonePulse.Start();
                                              }
                                          };
        }

        public void ParseAuthenticationRequest(JToken token)
        {
            var request = token.ToObject<AuthenticationRequestObject>();
            if (request.Call == null)
                return;

            var authRequest = request.Call.FirstOrDefault(x => x.AuthenticationRequest != null);
            if (authRequest != null)
            {
                ParseAuthenticationType(authRequest);

                _authRequestedCallInstance = request.Call.IndexOf(authRequest);

                Debug.Console(1,
                _parent,
                "Auth Requested Call Instance:{0} | {1}",
                _authRequestedCallInstance,
                authRequest.AuthenticationRequest.Value);

                AuthRequestedCallInstance.FireUpdate();
                AuthRequested.FireUpdate();
            }
        }

        private void ParseAuthenticationType(AuthenticationRequestResponseCall authRequest)
        {
            try
            {
                if (authRequest.AuthenticationRequest.Value == null)
                    throw new NullReferenceException("AuthenticationRequest.Value");

                Debug.Console(1, _parent, "Parsing Auth Request object: {0}", JsonConvert.SerializeObject(authRequest));

                _currentRequestType =
                    (AuthRequestedTypeEnum)
                    Enum.Parse(typeof (AuthRequestedTypeEnum), authRequest.AuthenticationRequest.Value, true);

                switch (_currentRequestType)
                {
                    case AuthRequestedTypeEnum.None:
                        break;
                    case AuthRequestedTypeEnum.HostPinOrGuest:
                        HostPinOrGuestRequested.Start();
                        break;
                    case AuthRequestedTypeEnum.PanelistPinOrAttendee:
                        PanelistPinOrAttendeeRequested.Start();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(_currentRequestType.ToString());
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Debug.Console(1, Debug.ErrorLogLevel.Notice, "Not sure how to deal with this auth request type:{0}", ex);
            }
            catch (Exception ex)
            {
                Debug.Console(1, Debug.ErrorLogLevel.Notice, "Caught an error parsing this auth request type:{0}", ex);
            } 
        }

        public void ParseAuthenticationResponse(JToken token)
        {
            var request = token.ToObject<AuthenticationResponseObject>();
            if (request.Call == null || request.Call.AuthenticationResponse == null)
                return;

            Debug.Console(1, _parent, "Parsing Auth Response object: {0}", JsonConvert.SerializeObject(request));

            var pinError = request.Call.AuthenticationResponse.PinError;

            if (pinError != null)
            {
                PinIncorrect.Start();
                _pin = string.Empty;
                Debug.Console(1, _parent, "Pin error on call instance:{0}", _authRequestedCallInstance);
                return;
            }

            var pinEntered = request.Call.AuthenticationResponse.PinEntered;

            if (pinEntered != null && pinEntered.AuthenticatingPin.Value == "False")
            {
                var role = request.Call.AuthenticationResponse.PinEntered.ParticipantRole.Value;

                if (!String.IsNullOrEmpty(role) && role == "Guest")
                {
                    JoinedAsGuest.Start();
                    JoinedAsAttendee.Start();
                    _pin = string.Empty;
                    Debug.Console(1, _parent, "Joined as {0}", role);
                    return;
                }

                if (!String.IsNullOrEmpty(role) && role == "Host")
                {
                    JoinedAsHost.Start();
                    _pin = string.Empty;
                    Debug.Console(1, _parent, "Joined as {0}", role);
                    return;
                }

                if (!String.IsNullOrEmpty(role) && role == "Panelist")
                {
                    JoinedAsPanelist.Start();
                    _pin = string.Empty;
                    Debug.Console(1, _parent, "Joined as {0}", role);
                    return;
                }
            }
        }

        public void JoinAsGuest()
        {
            const string commandFormat = "xCommand Conference Call AuthenticationResponse CallId: {0} ParticipantRole: Guest{1}\x0D\x0A";
            var command = String.Format(commandFormat, _authRequestedCallInstance, String.IsNullOrEmpty(_pin) ? String.Empty : String.Format(" Pin: {0}#", _pin));
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
            trilist.SetStringSigAction(joinMap.WebexPinInput.JoinNumber, s => _pin = s);
            trilist.SetSigTrueAction(joinMap.WebexPinClear.JoinNumber, () => _pin = string.Empty);
            trilist.SetSigTrueAction(joinMap.WebexJoinAsHost.JoinNumber, JoinAsHost);
            trilist.SetSigTrueAction(joinMap.WebexJoinAsGuest.JoinNumber, JoinAsGuest);
            trilist.SetSigTrueAction(joinMap.WebexJoinAsPanelist.JoinNumber, JoinAsPanelist);
            trilist.SetSigTrueAction(joinMap.WebexJoinAsAttendee.JoinNumber, JoinAsGuest);

            JoinedAsGuest.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.WebexJoinedAsGuest.JoinNumber]);
            JoinedAsHost.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.WebexJoinedAsHost.JoinNumber]);
            JoinedAsAttendee.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.WebexJoinedAsAttendee.JoinNumber]);
            JoinedAsPanelist.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.WebexJoinedAsPanelist.JoinNumber]);
            AuthNonePulse.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.WebexPinNotRequested.JoinNumber]);
            HostPinOrGuestRequested.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.WebexHostPinOrGuestRequested.JoinNumber]);
            PanelistPinOrAttendeeRequested.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.WebexPanelistPinOrAttendeeRequested.JoinNumber]);

            PinIncorrect.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.WebexPinError.JoinNumber]);
        }
    }
}