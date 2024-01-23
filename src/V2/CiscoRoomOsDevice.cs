using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Core.Intersystem;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Devices.Common.Cameras;
using PepperDash.Essentials.Devices.Common.Codec;

namespace epi_videoCodec_ciscoExtended.V2
{
    public class CiscoRoomOsDevice : EssentialsDevice, IHasCodecCameras, IOnline, ICommunicationMonitor, IBridgeAdvanced, IBasicVolumeWithFeedback, IPrivacy, IHasDoNotDisturbMode
    {
        public const string Delimiter = "\r\n";

        internal readonly CiscoCameras CodecCameras;
        internal readonly CiscoStandby Standby;
        internal readonly CiscoCallStatus CallStatus;
        internal readonly CiscoPresentation Presentation;
        internal readonly CiscoAudio Audio;
        internal readonly CiscoDirectory Directory;
        internal readonly CiscoRecents Recents;
        internal readonly CiscoDoNotDisturb DoNotDisturb;
        internal readonly CiscoSelfView SelfView;

        private readonly IBasicCommunication communications;
        private readonly IList<CiscoRoomOsFeature> features = new List<CiscoRoomOsFeature>();

        private readonly string username;
        private readonly string password;

        private bool isLoggedIn;
        private StringBuilder buffer = new StringBuilder();

        public CiscoRoomOsDevice(DeviceConfig config) : base(config.Key, config.Name)
        {
            var props = config.Properties.ToObject<CiscoCodecConfig>();
            username = props.Username ?? string.Empty;
            password = props.Password ?? string.Empty;

            communications = CommFactory.CreateCommForDevice(config);

            var gather = new CommunicationGather(communications, Delimiter);

            gather.LineReceived += GatherOnLineReceived;

            if (props.CommunicationMonitorProperties != null)
            {
                CommunicationMonitor = new GenericCommunicationMonitor(
                    this,
                    communications,
                    props.CommunicationMonitorProperties);
            }
            else
            {
                const string pollString = "xStatus SystemUnit Hardware\r";

                CommunicationMonitor = new GenericCommunicationMonitor(
                    this,
                    communications,
                    10000,
                    600000,
                    1200000,
                    pollString);
            }

            CodecCameras = new CiscoCameras(this);
            Standby = new CiscoStandby(this);
            CallStatus = new CiscoCallStatus(this);
            Presentation = new CiscoPresentation(this);
            Audio = new CiscoAudio(this);
            Directory = new CiscoDirectory(this);
            Recents = new CiscoRecents(this);
            DoNotDisturb = new CiscoDoNotDisturb(this);
            SelfView = new CiscoSelfView(this);

            features.Add(CodecCameras);
            features.Add(Standby);
            features.Add(CallStatus);
            features.Add(Presentation);
            features.Add(Audio);
            features.Add(Directory);
            features.Add(DoNotDisturb);
            features.Add(SelfView);
            features.Add(Recents);

            CodecCameras.CameraSelected += CameraSelected;
        }

        private void GatherOnLineReceived(object sender, GenericCommMethodReceiveTextArgs args)
        {
            var data = args.Text.Trim();

            if (string.IsNullOrEmpty(data) 
                || data == "Command not recognized."
                || data == "OK"
                || data == "ERROR")
            {
                return;
            }

            if (
                (data.StartsWith("*s SystemUnit") || data.StartsWith("*r Login successful"))
                && !isLoggedIn
                && (communications as ISocketStatus) == null) // RS232 Login Sucessful
            {
                isLoggedIn = true;

                var handler = Rs232LoggedIn;
                if (handler != null)
                    handler(this, EventArgs.Empty);
            }

            if (data == "** end")
            {
                var dataToProcess = buffer.ToString();
                buffer = new StringBuilder();

                if (!string.IsNullOrEmpty(dataToProcess))
                {
                    ProcessResponse(dataToProcess);
                }
            }
            else if (data.Contains("login:") || data.Contains("Password:"))
            {
                // handles login for non-delimited
            }
            else if (data.StartsWith("{") || data.EndsWith("}"))
            {
                SendText("xPreferences outputmode terminal");
            }
            else if (data.StartsWith("tshell:"))
            {
                // generic tshell notification
            }
            else if (data.StartsWith("xStatus"))
            {
                // echo needs to be disabled
                SendText("echo off");
            }
            else if (data.Contains("Login incorrect"))
            {
                Debug.Console(0, this, "Incorrect Login");
            }
            else
            {
                buffer.Append(data);
                buffer.Append("|");
            }
        }

        private void ProcessResponse(string response)
        {
            const string pattern = @"resultId:\s*\""(?<id>[a-fA-F0-9]+)\""";

            try
            {
                var match = Regex.Match(response, pattern);
                if (match.Success)
                {
                    var result = match.Groups["id"].Value;
                    var resultId = result.Trim();

                    Debug.Console(2, this, "Found a result id:{0}", resultId);
                    HandleResponse(resultId, response);
                }
                else
                {
                    foreach (var handler in features.OfType<IHandlesResponses>().Where(feature => feature.HandlesResponse(response)))
                    {
                        handler.HandleResponse(response);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Console(0, this, Debug.ErrorLogLevel.Notice, "Caught an exception handling a response{0} {1}", response, ex);
            }
        }

        public void SelectCamera(string key)
        {
            CodecCameras.SelectCamera(key);
        }

        public List<CameraBase> Cameras
        {
            get { return CodecCameras.Cameras; }
        }

        public CameraBase SelectedCamera
        {
            get { return CodecCameras.SelectedCamera; }
        }

        public StringFeedback SelectedCameraFeedback
        {
            get { return CodecCameras.SelectedCameraFeedback; }
        }

        public event EventHandler<CameraSelectedEventArgs> CameraSelected;

        public CameraBase FarEndCamera
        {
            get { return CodecCameras.FarEndCamera; }
        }

        public BoolFeedback ControllingFarEndCameraFeedback
        {
            get { return CodecCameras.ControllingFarEndCameraFeedback; }
        }

        private event EventHandler Rs232LoggedIn;

        public override void Initialize()
        {
            var pollTimer = new CTimer(
                _ =>
                {
                    PollFeatures();
                    PollForFeedback();
                },
                Timeout.Infinite);

            if (communications is ISocketStatus)
            {
                (communications as ISocketStatus)
                    .ConnectionChange += (sender, args) =>
                    {
                        if (args.Client.ClientStatus ==
                            SocketStatus.SOCKET_STATUS_CONNECTED)
                        {
                            pollTimer.Reset(250, 30000);
                        }
                        else
                        {
                            pollTimer.Stop();
                        }
                    };

                communications.Connect();
                isLoggedIn = true;
                CommunicationMonitor.Start();
            }
            else
            {
                communications.TextReceived += CommunicationsOnTextReceived;
                Rs232LoggedIn += (sender, args) => pollTimer.Reset(250, 30000);
                CommunicationMonitor.Start();
            }

            CrestronEnvironment
                .ProgramStatusEventHandler += type =>
                {
                    if (type == eProgramStatusEventType.Stopping)
                    {
                        pollTimer.Stop();
                    }
                };
        }

        private void CommunicationsOnTextReceived(object sender, GenericCommMethodReceiveTextArgs args)
        {
            var data = args.Text.Trim();

            if (data.Contains("login:"))
            {
                isLoggedIn = false;
                if (string.IsNullOrEmpty(username))
                {
                    Debug.Console(0, Debug.ErrorLogLevel.Notice, "Prompted for a username but none is configured");
                }
                else
                {
                    SendText(username);
                }
            }
            else if (data.Contains("Password:"))
            {
                isLoggedIn = false;
                if (string.IsNullOrEmpty(password))
                {
                    Debug.Console(0, this, Debug.ErrorLogLevel.Notice, "Prompted for a password but none is configured");
                }
                else
                {
                    SendText(password);
                }
            }
        }

        public void SendText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var textToSend = PreProcessStringToSend(text);
            communications.SendText(textToSend);
        }

        class CiscoRoomOsRequestHandler
        {
            private readonly Action<string> action;
            public readonly DateTime At;

            public CiscoRoomOsRequestHandler(Action<string> action)
            {
                this.action = action;
                At = DateTime.UtcNow;
            }

            public void HandleResponse(string response)
            {
                action(response);
            }
        }

        private readonly CCriticalSection requestSync = new CCriticalSection();
        private readonly Dictionary<string, CiscoRoomOsRequestHandler> requestCallbacks = new Dictionary<string, CiscoRoomOsRequestHandler>();

        public void SendTaggedRequest(string request)
        {
            SendTaggedRequest(request, (s => Debug.Console(1, this, "Unhandled response:{0}", s)));
        }

        public void SendTaggedRequest(string request, Action<string> onResponse)
        {
            var requestId = Guid.NewGuid().ToString("N");
            var textToSend = request.Trim() + ("|resultid=" + requestId);

            Action<string> callback = response =>
            {
                var cb = onResponse ?? (s => Debug.Console(1, this, "Unhandled response:{0}", s));
                try
                {
                    cb(response);
                }
                catch (Exception ex)
                {
                    Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Caught an exception in a callback for{0} {1}", request, ex);
                }
            };

            requestSync.Enter();
            try
            {
                requestCallbacks.Add(requestId, new CiscoRoomOsRequestHandler(callback));
            }
            finally
            {
                requestSync.Leave();
            }

            SendText(textToSend);
        }

        public void HandleResponse(string resultId, string response)
        {
            CiscoRoomOsRequestHandler handler;
            requestSync.Enter();
            try
            {
                if (!requestCallbacks.TryGetValue(resultId, out handler))
                {
                    Debug.Console(0, this, "Unhandled response:{0}", response);
                    return;
                }

                requestCallbacks.Remove(resultId);
            }
            finally
            {
                requestSync.Leave();
            }

            handler.HandleResponse(response);
        }

        public static string PreProcessStringToSend(string text)
        {
            const string delimiter = "\r";
            return text.Trim() + delimiter;
        }

        public void PollFeatures()
        {
            var polls = 
                from feature in features.OfType<IHasPolls>()
                from poll in feature.Polls.Distinct()
                select poll;

            foreach (var item in polls)
            {
                SendText(item);
            }
        }

        public void PollForFeedback()
        {
            Action<string> parseFeedbackRequest = response =>
            {
                var feedbackSubscriptions = 
                    from feature in features.OfType<IHasEventSubscriptions>()
                    from sub in feature.Subscriptions.Distinct()
                    select sub;

                foreach (var subscription in feedbackSubscriptions
                    .Where(sub =>
                        response.IndexOf(sub, StringComparison.OrdinalIgnoreCase) < 0))
                {
                    Debug.Console(0, this, "Registering for feedback:{0}", subscription);
                    SendText("xFeedback register " + subscription);
                }
            };

            SendTaggedRequest("xFeedback list", parseFeedbackRequest);
        }

        public BoolFeedback IsOnline
        {
            get { return CommunicationMonitor.IsOnlineFeedback; }
        }

        public StatusMonitorBase CommunicationMonitor { get; private set; }

        public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new CiscoBaseJoinMap(joinStart);
            if (bridge != null)
                bridge.AddJoinMap(Key, joinMap);

            IsOnline.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);

            trilist.SetSigTrueAction(joinMap.DtmfPound.JoinNumber, () => CallStatus.SendDtmf("#"));
            trilist.SetSigTrueAction(joinMap.DtmfStar.JoinNumber, () => CallStatus.SendDtmf("*"));

            for (uint x = 0; x < joinMap.DtmfJoins.JoinSpan; ++x)
            {
                var joinNumber = joinMap.DtmfJoins.JoinNumber + x;
                var dtmfNumber = Convert.ToString(x + 1);
                if (dtmfNumber == "10")
                {
                    Debug.Console(1, this, "Linkig DTMF:{0} to Join:{1}", "0", joinNumber);
                    trilist.SetSigTrueAction(joinNumber, () => CallStatus.SendDtmf("0"));
                }
                else
                {
                    Debug.Console(1, this, "Linkig DTMF:{0} to Join:{1}", dtmfNumber, joinNumber);
                    trilist.SetSigTrueAction(joinNumber, () => CallStatus.SendDtmf(dtmfNumber));
                }
            }

            trilist.SetSigTrueAction(joinMap.EndAllCalls.JoinNumber, () => CallStatus.EndAllCalls());

            CallStatus.CallIsConnectedOrConnecting.LinkInputSig(trilist.BooleanInput[joinMap.CallIsConnectedOrConnecting.JoinNumber]);
            CallStatus.CallIsIncoming.LinkInputSig(trilist.BooleanInput[joinMap.CallIsIncoming.JoinNumber]);
            CallStatus.CallStatusXSig.LinkInputSig(trilist.StringInput[joinMap.CallStatusXSig.JoinNumber]);
            CallStatus.IncomingCallName.LinkInputSig(trilist.StringInput[joinMap.IncomingName.JoinNumber]);
            CallStatus.IncomingCallNumber.LinkInputSig(trilist.StringInput[joinMap.IncomingNumber.JoinNumber]);
            CallStatus.NumberOfActiveCalls.LinkInputSig(trilist.UShortInput[joinMap.NumberOfActiveCalls.JoinNumber]);

            trilist.SetSigTrueAction(joinMap.AnswerIncoming.JoinNumber, () => CallStatus.AcceptCall());
            trilist.SetSigTrueAction(joinMap.RejectIncoming.JoinNumber, () => CallStatus.RejectCall());
            trilist.SetSigTrueAction(joinMap.JoinAllCalls.JoinNumber, () => CallStatus.JoinAllCalls());
            trilist.SetSigTrueAction(joinMap.HoldAllCalls.JoinNumber, () => CallStatus.HoldAllCalls());
            trilist.SetSigTrueAction(joinMap.ResumeAllCalls.JoinNumber, () => CallStatus.ResumeAllCalls());

            trilist.SetSigTrueAction(joinMap.NearEndPresentationStart.JoinNumber, () => Presentation.StartSharing());
            trilist.SetSigTrueAction(joinMap.NearEndPresentationStop.JoinNumber, () => Presentation.StopSharing());

            trilist.SetSigTrueAction(joinMap.StandbyOn.JoinNumber, () => Standby.StandbyActivate());
            trilist.SetSigTrueAction(joinMap.StandbyOff.JoinNumber, () => Standby.StandbyDeactivate());
            Standby.StandbyIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.StandbyOn.JoinNumber]);
            Standby.StandbyIsOnFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.StandbyOn.JoinNumber]);
            Standby.EnteringStandbyModeFeedback.LinkInputSig(trilist.BooleanInput[joinMap.EnteringStandby.JoinNumber]);

            trilist.SetSigTrueAction(joinMap.MicMuteOn.JoinNumber, PrivacyModeOn);
            trilist.SetSigTrueAction(joinMap.MicMuteOff.JoinNumber, PrivacyModeOff);
            trilist.SetSigTrueAction(joinMap.MicMuteToggle.JoinNumber, PrivacyModeToggle);

            trilist.SetBoolSigAction(joinMap.VolumeUp.JoinNumber, VolumeUp);
            trilist.SetBoolSigAction(joinMap.VolumeDown.JoinNumber, VolumeDown);
            trilist.SetSigTrueAction(joinMap.VolumeMuteOn.JoinNumber, MuteOn);
            trilist.SetSigTrueAction(joinMap.VolumeMuteOff.JoinNumber, MuteOff);
            trilist.SetSigTrueAction(joinMap.VolumeMuteToggle.JoinNumber, MuteToggle);
            trilist.SetUShortSigAction(joinMap.Volume.JoinNumber, SetVolume);
            VolumeLevelFeedback.LinkInputSig(trilist.UShortInput[joinMap.Volume.JoinNumber]);

            MuteFeedback.LinkInputSig(trilist.BooleanInput[joinMap.VolumeMuteOn.JoinNumber]);
            MuteFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.VolumeMuteOff.JoinNumber]);

            PrivacyModeIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.MicMuteOn.JoinNumber]);
            PrivacyModeIsOnFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.MicMuteOff.JoinNumber]);
       
            trilist.SetSigTrueAction(joinMap.SpeakerTrackEnabled.JoinNumber, () => CodecCameras.CameraAutoModeOn());
            trilist.SetSigTrueAction(joinMap.SpeakerTrackDisabled.JoinNumber, () => CodecCameras.CameraAutoModeOff());
            CodecCameras.SpeakerTrackIsAvailable.LinkInputSig(trilist.BooleanInput[joinMap.SpeakerTrackAvailable.JoinNumber]);

            for (uint x = 0; x < joinMap.HangUpCall.JoinSpan; ++x)
            {
                var joinNumber = joinMap.HangUpCall.JoinNumber + x;
                var index = (int) x;

                Debug.Console(1, this, "Linking End Call:{0} to Join:{1}", index, joinNumber);
                trilist.SetSigTrueAction(joinNumber, () =>
                {
                    var call = CallStatus.ActiveCalls.ElementAtOrDefault(index);
                    if (call != null)
                    {
                        CallStatus.EndCall(call);
                    }
                });
            }

            for (uint x = 0; x < joinMap.JoinCall.JoinSpan; ++x)
            {
                var joinNumber = joinMap.JoinCall.JoinNumber + x;
                var index = (int)x;

                Debug.Console(1, this, "Linking Join Call:{0} to Join:{1}", index, joinNumber);
                trilist.SetSigTrueAction(joinNumber, () =>
                {
                    var call = CallStatus.ActiveCalls.ElementAtOrDefault(index);
                    if (call != null)
                    {
                        CallStatus.JoinCall(call);
                    }
                });
            }

            for (uint x = 0; x < joinMap.HoldCall.JoinSpan; ++x)
            {
                var joinNumber = joinMap.HoldCall.JoinNumber + x;
                var index = (int)x;

                Debug.Console(1, this, "Linking HoldCall:{0} to Join:{1}", index, joinNumber);
                trilist.SetSigTrueAction(joinNumber, () =>
                {
                    var call = CallStatus.ActiveCalls.ElementAtOrDefault(index);
                    if (call != null)
                    {
                        CallStatus.HoldCall(call);
                    }
                });
            }

            for (uint x = 0; x < joinMap.ResumeCall.JoinSpan; ++x)
            {
                var joinNumber = joinMap.ResumeCall.JoinNumber + x;
                var index = (int)x;

                Debug.Console(1, this, "Linking ResumeCall:{0} to Join:{1}", index, joinNumber);
                trilist.SetSigTrueAction(joinNumber, () =>
                {
                    var call = CallStatus.ActiveCalls.ElementAtOrDefault(index);
                    if (call != null)
                    {
                        CallStatus.ResumeCall(call);
                    }
                });
            }

            Action<CameraBase> selectCameraFeedback = camera =>
            {
                if (camera == null)
                {
                    return;
                }

                var selectedCameraIndex = CodecCameras.Cameras.IndexOf(camera) + 1;
                trilist.SetUshort(joinMap.CameraSelect.JoinNumber, (ushort) selectedCameraIndex);
            };

            CodecCameras.CameraSelected += (sender, args) => selectCameraFeedback(args.SelectedCamera);
            selectCameraFeedback(CodecCameras.SelectedCamera);

            trilist.SetUShortSigAction(joinMap.CameraPresetActivate.JoinNumber, value =>
            {
                var camera = CodecCameras.SelectedCamera as IHasCameraPresets;
                if (camera != null)
                {
                    camera.PresetSelect(value);
                }
            });

            trilist.SetUShortSigAction(joinMap.FarEndCameraPresetActivate.JoinNumber, value =>
            {
                var camera = CodecCameras.FarEndCamera as IHasCameraPresets;
                if (camera != null)
                {
                    camera.PresetSelect(value);
                }
            });

            trilist.SetUShortSigAction(joinMap.CameraPresetStore.JoinNumber, value =>
            {
                var camera = CodecCameras.SelectedCamera as IHasCameraPresets;
                if (camera != null)
                {
                    camera.PresetStore(value, "preset" + value);
                }
            });

            trilist.SetUShortSigAction(joinMap.CameraSelect.JoinNumber, value =>
            {
                if (value == 0)
                {
                    return;
                }

                var cameraToSelect = CodecCameras.Cameras.ElementAtOrDefault(value - 1);
                if (cameraToSelect != null)
                {
                    CodecCameras.SelectCamera(cameraToSelect.Key);
                }
            });

            trilist.SetBoolSigAction(joinMap.NearEndCameraUp.JoinNumber, value =>
            {
                var selectedCamera = CodecCameras.SelectedCamera as IHasCameraPtzControl;
                if (selectedCamera == null)
                {
                    return;
                }

                if (value)
                {
                    selectedCamera.TiltUp();
                }
                else
                {
                    selectedCamera.TiltStop();
                }
            });

            trilist.SetBoolSigAction(joinMap.NearEndCameraDown.JoinNumber, value =>
            {
                var selectedCamera = CodecCameras.SelectedCamera as IHasCameraPtzControl;
                if (selectedCamera == null)
                {
                    return;
                }

                if (value)
                {
                    selectedCamera.TiltDown();
                }
                else
                {
                    selectedCamera.TiltStop();
                }
            });

            trilist.SetBoolSigAction(joinMap.NearEndCameraLeft.JoinNumber, value =>
            {
                var selectedCamera = CodecCameras.SelectedCamera as IHasCameraPtzControl;
                if (selectedCamera == null)
                {
                    return;
                }

                if (value)
                {
                    selectedCamera.PanLeft();
                }
                else
                {
                    selectedCamera.PanStop();
                }
            });

            trilist.SetBoolSigAction(joinMap.NearEndCameraRight.JoinNumber, value =>
            {
                var selectedCamera = CodecCameras.SelectedCamera as IHasCameraPtzControl;
                if (selectedCamera == null)
                {
                    return;
                }

                if (value)
                {
                    selectedCamera.PanRight();
                }
                else
                {
                    selectedCamera.PanStop();
                }
            });

            trilist.SetBoolSigAction(joinMap.NearEndCameraZoomIn.JoinNumber, value =>
            {
                var selectedCamera = CodecCameras.SelectedCamera as IHasCameraPtzControl;
                if (selectedCamera == null)
                {
                    return;
                }

                if (value)
                {
                    selectedCamera.ZoomIn();
                }
                else
                {
                    selectedCamera.ZoomStop();
                }
            });

            trilist.SetBoolSigAction(joinMap.NearEndCameraZoomOut.JoinNumber, value =>
            {
                var selectedCamera = CodecCameras.SelectedCamera as IHasCameraPtzControl;
                if (selectedCamera == null)
                {
                    return;
                }

                if (value)
                {
                    selectedCamera.ZoomOut();
                }
                else
                {
                    selectedCamera.ZoomStop();
                }
            });

            trilist.SetBoolSigAction(joinMap.NearEndCameraFocusIn.JoinNumber, value =>
            {
                var selectedCamera = CodecCameras.SelectedCamera as IHasCameraFocusControl;
                if (selectedCamera == null)
                {
                    return;
                }

                if (value)
                {
                    selectedCamera.FocusNear();
                }
                else
                {
                    selectedCamera.FocusStop();
                }
            });

            trilist.SetBoolSigAction(joinMap.NearEndCameraFocusOut.JoinNumber, value =>
            {
                var selectedCamera = CodecCameras.SelectedCamera as IHasCameraFocusControl;
                if (selectedCamera == null)
                {
                    return;
                }

                if (value)
                {
                    selectedCamera.FocusFar();
                }
                else
                {
                    selectedCamera.FocusStop();
                }
            });

            trilist.SetBoolSigAction(joinMap.FarEndCameraUp.JoinNumber, value =>
            {
                var selectedCamera = CodecCameras.FarEndCamera as IHasCameraPtzControl;
                if (selectedCamera == null)
                {
                    return;
                }

                if (value)
                {
                    selectedCamera.TiltUp();
                }
                else
                {
                    selectedCamera.TiltStop();
                }
            });

            trilist.SetBoolSigAction(joinMap.FarEndCameraDown.JoinNumber, value =>
            {
                var selectedCamera = CodecCameras.FarEndCamera as IHasCameraPtzControl;
                if (selectedCamera == null)
                {
                    return;
                }

                if (value)
                {
                    selectedCamera.TiltDown();
                }
                else
                {
                    selectedCamera.TiltStop();
                }
            });

            trilist.SetBoolSigAction(joinMap.FarEndCameraLeft.JoinNumber, value =>
            {
                var selectedCamera = CodecCameras.FarEndCamera as IHasCameraPtzControl;
                if (selectedCamera == null)
                {
                    return;
                }

                if (value)
                {
                    selectedCamera.PanLeft();
                }
                else
                {
                    selectedCamera.PanStop();
                }
            });

            trilist.SetBoolSigAction(joinMap.FarEndCameraRight.JoinNumber, value =>
            {
                var selectedCamera = CodecCameras.FarEndCamera as IHasCameraPtzControl;
                if (selectedCamera == null)
                {
                    return;
                }

                if (value)
                {
                    selectedCamera.PanRight();
                }
                else
                {
                    selectedCamera.PanStop();
                }
            });

            trilist.SetBoolSigAction(joinMap.FarEndCameraZoomIn.JoinNumber, value =>
            {
                var selectedCamera = CodecCameras.FarEndCamera as IHasCameraPtzControl;
                if (selectedCamera == null)
                {
                    return;
                }

                if (value)
                {
                    selectedCamera.ZoomIn();
                }
                else
                {
                    selectedCamera.ZoomStop();
                }
            });

            trilist.SetBoolSigAction(joinMap.FarEndCameraZoomOut.JoinNumber, value =>
            {
                var selectedCamera = CodecCameras.FarEndCamera as IHasCameraPtzControl;
                if (selectedCamera == null)
                {
                    return;
                }

                if (value)
                {
                    selectedCamera.ZoomOut();
                }
                else
                {
                    selectedCamera.ZoomStop();
                }
            });

            var dialNumber = string.Empty;

            trilist.SetStringSigAction(joinMap.DialNumber.JoinNumber, s =>
            {
                dialNumber = s;
            });

            trilist.SetSigTrueAction(joinMap.ManualDial.JoinNumber, () => CallStatus.Dial(dialNumber));

            const int xSigEncoding = 28591;

            Directory.DirectoryResultReturned += (sender, args) =>
            {
                var argsCount = args.DirectoryIsOnRoot
                    ? args.Directory.CurrentDirectoryResults.Count(a => a.ParentFolderId.Equals("root"))
                    : args.Directory.CurrentDirectoryResults.Count;

                trilist.SetUshort(joinMap.DirectoryNumberOfRows.JoinNumber, (ushort)argsCount);

                var clearBytes = XSigHelpers.ClearOutputs();

                trilist.SetString(joinMap.DirectoryXSig.JoinNumber,
                    Encoding.GetEncoding(xSigEncoding).GetString(clearBytes, 0, clearBytes.Length));

                var directoryXSig = CiscoDirectory.UpdateDirectoryXSig(args.Directory, args.DirectoryIsOnRoot);
                trilist.SetString(joinMap.DirectoryXSig.JoinNumber, directoryXSig);
            };

            DirectoryItem selectedContact = null;
            ContactMethod selectedContactMethod = null;

            var selectedItemNameFeedback = new StringFeedback(Key + "-SelectedContact-" + trilist.ID, () => 
                selectedContact == null ? string.Empty : selectedContact.Name);

            var selectedItemNumberOfContactMethods = new IntFeedback(Key + "-NumberOfContactMethods-" + trilist.ID, () =>
                selectedContact as DirectoryContact == null ? 0 : (selectedContact as DirectoryContact).ContactMethods.Count);

            var selectedItemCallMethodsFeedback = new StringFeedback(() =>
            {
                var clearBytes = XSigHelpers.ClearOutputs();
                var clearString = Encoding.GetEncoding(xSigEncoding).GetString(clearBytes, 0, clearBytes.Length);
                if (selectedContact == null)
                {
                    return clearString;
                }

                var result = selectedContact as DirectoryContact;
                return result == null ? clearString : Directory.UpdateContactMethodsXSig(result);
            });

            trilist.SetUShortSigAction(joinMap.DirectorySelectContact.JoinNumber,
                value =>
                {
                    selectedContactMethod = null;
                    if (value == 0)
                    {
                        selectedContact = null;
                    }

                    selectedContact = Directory.CurrentDirectoryResult.Contacts.ElementAtOrDefault(value - 1);
                    selectedItemNameFeedback.FireUpdate();
                    selectedItemNumberOfContactMethods.FireUpdate();
                    selectedItemCallMethodsFeedback.FireUpdate();
                });

            trilist.SetUShortSigAction(joinMap.DirectorySelectContactMethod.JoinNumber,
                value =>
                {
                    selectedContactMethod = null;
                    if (value == 0)
                    {
                        return;
                    }

                    var contact = selectedContact as DirectoryContact;
                    if (contact == null)
                    {
                        return;
                    }

                    selectedContactMethod = contact.ContactMethods.ElementAtOrDefault(value - 1);
                });

            trilist.SetSigTrueAction(joinMap.DialSelectedContact.JoinNumber, () =>
            {
                var contact = selectedContact as DirectoryContact;
                if (contact == null)
                {
                    return;
                }

                var contactMethod = selectedContactMethod ?? contact.ContactMethods.FirstOrDefault();
                if (contactMethod == null)
                {
                    return;
                }

                CallStatus.Dial(contactMethod.Number);
            });

            selectedItemNameFeedback.LinkInputSig(trilist.StringInput[joinMap.SelectedContactName.JoinNumber]);
            selectedItemNumberOfContactMethods.LinkInputSig(trilist.UShortInput[joinMap.SelectedDirectoryItemContactMethodsXsig.JoinNumber]);
            selectedItemCallMethodsFeedback.LinkInputSig(trilist.StringInput[joinMap.SelectedDirectoryItemContactMethodsXsig.JoinNumber]);

            selectedItemNameFeedback.RegisterForDebug(this);
            selectedItemNumberOfContactMethods.RegisterForDebug(this);

            Directory.SearchIsInProgress.LinkInputSig(trilist.BooleanInput[joinMap.SearchIsBusy.JoinNumber]);
            trilist.SetStringSigAction(joinMap.SearchDirectory.JoinNumber, s => Directory.SearchDirectory(s));

            trilist.SetUShortSigAction(joinMap.SelectRecentCall.JoinNumber, value => Recents.SelectedRecent = value);
            Recents.SelectedRecentName.LinkInputSig(trilist.StringInput[joinMap.SelectRecentName.JoinNumber]);
            Recents.SelectedRecentNumber.LinkInputSig(trilist.StringInput[joinMap.SelectRecentNumber.JoinNumber]);

            for (var x = 0; x < joinMap.Recents.JoinSpan; ++x)
            {
                var join = joinMap.Recents.JoinNumber + x;
                var index = x;
                var feedback = Recents.Feedbacks.ElementAtOrDefault(index);
                if (feedback == null)
                {
                    continue;
                }

                Debug.Console(1, this, "Linking recent call index:{0} Join{1}", index, join);
                feedback.LinkInputSig(trilist.StringInput[(uint)join]);
            }

            DoNotDisturbModeIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.DoNotDisturbOn.JoinNumber]);
            DoNotDisturbModeIsOnFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.DoNotDisturbOff.JoinNumber]);

            trilist.SetSigTrueAction(joinMap.DoNotDisturbOn.JoinNumber, ActivateDoNotDisturbMode);
            trilist.SetSigTrueAction(joinMap.DoNotDisturbOff.JoinNumber, DeactivateDoNotDisturbMode);
            trilist.SetSigTrueAction(joinMap.DoNotDisturbToggle.JoinNumber, ToggleDoNotDisturbMode);

            // we may need these, just need to verify what's available on the bridge
            // trilist.SetSigTrueAction(joinMap.SelfviewOn.JoinNumber, () => SelfView.SelfViewModeOn());
            // trilist.SetSigTrueAction(joinMap.SelfviewOff.JoinNumber, () => SelfView.SelfViewModeOff());

            trilist.SetSigTrueAction(joinMap.SelfviewToggle.JoinNumber, () => SelfView.SelfViewModeToggle());
            SelfView.SelfviewIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.SelfviewToggle.JoinNumber]);


            trilist.SetUShortSigAction(joinMap.NearEndPresentationSource.JoinNumber, value => Presentation.SetShareSource(value));
            Presentation.SharingSourceIntFeedback.LinkInputSig(trilist.UShortInput[joinMap.NearEndPresentationSource.JoinNumber]);

            communications.TextReceived += (sender, args) => trilist.SetString(joinMap.Coms.JoinNumber, args.Text);
            trilist.SetStringSigAction(joinMap.Coms.JoinNumber, value => communications.SendText(value));
        }

        public void VolumeUp(bool pressRelease)
        {
            ((IBasicVolumeControls) Audio).VolumeUp(pressRelease);
        }

        public void VolumeDown(bool pressRelease)
        {
            ((IBasicVolumeControls) Audio).VolumeDown(pressRelease);
        }

        public void MuteToggle()
        {
            ((IBasicVolumeControls) Audio).MuteToggle();
        }

        public void MuteOn()
        {
            ((IBasicVolumeWithFeedback) Audio).MuteOn();
        }

        public void MuteOff()
        {
            ((IBasicVolumeWithFeedback) Audio).MuteOff();
        }

        public void SetVolume(ushort level)
        {
            ((IBasicVolumeWithFeedback) Audio).SetVolume(level);
        }

        public BoolFeedback MuteFeedback
        {
            get { return Audio.MuteFeedback; }
        }

        public IntFeedback VolumeLevelFeedback
        {
            get { return Audio.VolumeLevelFeedback; }
        }

        public void PrivacyModeOn()
        {
            ((IPrivacy) Audio).PrivacyModeOn();
        }

        public void PrivacyModeOff()
        {
            ((IPrivacy) Audio).PrivacyModeOff();
        }

        public void PrivacyModeToggle()
        {
            ((IPrivacy) Audio).PrivacyModeToggle();
        }

        public BoolFeedback PrivacyModeIsOnFeedback
        {
            get { return Audio.PrivacyModeIsOnFeedback; }
        }

        public void ActivateDoNotDisturbMode()
        {
            ((IHasDoNotDisturbMode) DoNotDisturb).ActivateDoNotDisturbMode();
        }

        public void DeactivateDoNotDisturbMode()
        {
            ((IHasDoNotDisturbMode) DoNotDisturb).DeactivateDoNotDisturbMode();
        }

        public void ToggleDoNotDisturbMode()
        {
            ((IHasDoNotDisturbMode) DoNotDisturb).ToggleDoNotDisturbMode();
        }

        public BoolFeedback DoNotDisturbModeIsOnFeedback
        {
            get { return DoNotDisturb.DoNotDisturbModeIsOnFeedback; }
        }
    }

    public class CiscoBaseJoinMap : JoinMapBaseAdvanced
    {
        [JoinName("IsOnline")]
        public JoinDataComplete IsOnline = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "IsOnline",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("DtmfJoins")]
        public JoinDataComplete DtmfJoins = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 11,
                JoinSpan = 10
            },
            new JoinMetadata
            {
                Description = "DtmfJoins",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("DtmfStar")]
        public JoinDataComplete DtmfStar = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 21,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DtmfStar",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("DtmfPound")]
        public JoinDataComplete DtmfPound = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 22,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DtmfPound",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("NumberOfActiveCalls")]
        public JoinDataComplete NumberOfActiveCalls = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 25,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NumberOfActiveCalls",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("EndAllCalls")]
        public JoinDataComplete EndAllCalls = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 24,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "EndAllCalls",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("CallIsConnectedOrConnecting")]
        public JoinDataComplete CallIsConnectedOrConnecting = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 31,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "CallIsConnectedOrConnecting",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("CallIsIncoming")]
        public JoinDataComplete CallIsIncoming = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 50,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "CallIsIncoming",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("AnswerIncoming")]
        public JoinDataComplete AnswerIncoming = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 51,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "AnswerIncoming",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("RejectIncoming")]
        public JoinDataComplete RejectIncoming = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 52,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "RejectIncoming",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("IncomingName")]
        public JoinDataComplete IncomingName = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 51,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "IncomingName",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("IncomingNumber")]
        public JoinDataComplete IncomingNumber = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 52,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "IncomingNumber",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("HangUpCall")]
        public JoinDataComplete HangUpCall = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 81,
                JoinSpan = 8
            },
            new JoinMetadata
            {
                Description = "HangUpCall",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("JoinAllCalls")]
        public JoinDataComplete JoinAllCalls = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 90,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "JoinAllCalls",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("JoinCall")]
        public JoinDataComplete JoinCall = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 91,
                JoinSpan = 8
            },
            new JoinMetadata
            {
                Description = "JoinCall",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("HoldAllCalls")]
        public JoinDataComplete HoldAllCalls = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 220,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "HoldAllCalls",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("HoldCall")]
        public JoinDataComplete HoldCall = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 221,
                JoinSpan = 8
            },
            new JoinMetadata
            {
                Description = "HoldCall",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("ResumeAllCalls")]
        public JoinDataComplete ResumeAllCalls = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 230,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "ResumeAllCalls",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("ResumeCall")]
        public JoinDataComplete ResumeCall = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 231,
                JoinSpan = 8
            },
            new JoinMetadata
            {
                Description = "JoinCall",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("NearEndPresentationSource")]
        public JoinDataComplete NearEndPresentationSource = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 201,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NearEndPresentationSource",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("NearEndPresentationStart")]
        public JoinDataComplete NearEndPresentationStart = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 201,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NearEndPresentationStart",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("NearEndPresentationStop")]
        public JoinDataComplete NearEndPresentationStop = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 202,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NearEndPresentationStop",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("StandbyOn")]
        public JoinDataComplete StandbyOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 246,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "StandbyOn",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("StandbyOff")]
        public JoinDataComplete StandbyOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 247,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "StandbyOff",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("EnteringStandby")]
        public JoinDataComplete EnteringStandby = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 248,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "EnteringStandby",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("SearchIsBusy")]
        public JoinDataComplete SearchIsBusy = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 100,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SearchIsBusy",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("MicMuteOn")]
        public JoinDataComplete MicMuteOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 171,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "MicMuteOn",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("MicMuteOff")]
        public JoinDataComplete MicMuteOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 172,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "MicMuteOff",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("MicMuteToggle")]
        public JoinDataComplete MicMuteToggle = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 173,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "MicMuteToggle",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("VolumeUp")]
        public JoinDataComplete VolumeUp = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 174,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "VolumeUp",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("VolumeDown")]
        public JoinDataComplete VolumeDown = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 175,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "VolumeDown",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("Volume")]
        public JoinDataComplete Volume = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 174,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Volume",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("VolumeMuteOn")]
        public JoinDataComplete VolumeMuteOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 176,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "VolumeMuteOn",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("VolumeMuteOff")]
        public JoinDataComplete VolumeMuteOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 177,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "VolumeMuteOff",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("VolumeMuteToggle")]
        public JoinDataComplete VolumeMuteToggle = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 178,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "VolumeMuteToggle",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("DoNotDisturbOn")]
        public JoinDataComplete DoNotDisturbOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 241,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DoNotDisturbOn",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("DoNotDisturbOff")]
        public JoinDataComplete DoNotDisturbOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 242,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DoNotDisturbOff",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("DoNotDisturbToggle")]
        public JoinDataComplete DoNotDisturbToggle = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 243,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DoNotDisturbToggle",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        /* We will probably need these one day
        [JoinName("SelfviewOn")]
        public JoinDataComplete SelfviewOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 241,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SelfviewOn",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("SelfviewOff")]
        public JoinDataComplete SelfviewOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 242,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SelfviewOff",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });
         * */

        [JoinName("SelfviewToggle")]
        public JoinDataComplete SelfviewToggle = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 141,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SelfviewToggle",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });


        [JoinName("NearEndCameraUp")]
        public JoinDataComplete NearEndCameraUp = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 111,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NearEndCameraUp",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("NearEndCameraDown")]
        public JoinDataComplete NearEndCameraDown = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 112,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NearEndCameraDown",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("NearEndCameraLeft")]
        public JoinDataComplete NearEndCameraLeft = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 113,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NearEndCameraLeft",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("NearEndCameraRight")]
        public JoinDataComplete NearEndCameraRight = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 114,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NearEndCameraRight",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("NearEndCameraZoomIn")]
        public JoinDataComplete NearEndCameraZoomIn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 115,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NearEndCameraZoomIn",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("NearEndCameraZoomOut")]
        public JoinDataComplete NearEndCameraZoomOut = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 116,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NearEndCameraUp",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("NearEndCameraFocusIn")]
        public JoinDataComplete NearEndCameraFocusIn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 117,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NearEndCameraFocusIn",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("NearEndCameraFocusOut")]
        public JoinDataComplete NearEndCameraFocusOut = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 121,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NearEndCameraFocusOut",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });


        [JoinName("FarEndCameraUp")]
        public JoinDataComplete FarEndCameraUp = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 122,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "FarEndCameraUp",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("FarEndCameraDown")]
        public JoinDataComplete FarEndCameraDown = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 123,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "FarEndCameraDown",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("FarEndCameraLeft")]
        public JoinDataComplete FarEndCameraLeft = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 124,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "FarEndCameraLeft",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("FarEndCameraRight")]
        public JoinDataComplete FarEndCameraRight = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 125,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "FarEndCameraRight",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("FarEndCameraZoomIn")]
        public JoinDataComplete FarEndCameraZoomIn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 126,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "FarEndCameraZoomIn",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("FarEndCameraZoomOut")]
        public JoinDataComplete FarEndCameraZoomOut = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 127,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "FarEndCameraUp",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });


        [JoinName("SpeakerTrackEnabled")]
        public JoinDataComplete SpeakerTrackEnabled = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 131,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SpeakerTrackEnabled",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("SpeakerTrackDisabled")]
        public JoinDataComplete SpeakerTrackDisabled = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 132,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SpeakerTrackDisabled",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("SpeakerTrackAvailable")]
        public JoinDataComplete SpeakerTrackAvailable = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 143,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SpeakerTrackAvailable",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("ManualDial")]
        public JoinDataComplete ManualDial = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 71,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "ManualDial",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("DialNumber")]
        public JoinDataComplete DialNumber = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DialNumber",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("CallStatusXSig")]
        public JoinDataComplete CallStatusXSig = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "CallStatusXSig",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("DirectoryXSig")]
        public JoinDataComplete DirectoryXSig = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 101,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DirectoryXSig",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("SearchDirectory")]
        public JoinDataComplete SearchDirectory = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 100,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SearchDirectory",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("DirectoryNumberOfRows")]
        public JoinDataComplete DirectoryNumberOfRows = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 101,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DirectoryNumberOfRows",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("DirectorySelectContact")]
        public JoinDataComplete DirectorySelectContact = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 101,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DirectorySelectContact",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("SelectedContactName")]
        public JoinDataComplete SelectedContactName = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 102,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SelectedContactName",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("DirectorySelectContactMethod")]
        public JoinDataComplete DirectorySelectContactMethod = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 103,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DirectorySelectContactMethod",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("SelectedDirectoryItemNumberOfContactMethods")]
        public JoinDataComplete SelectedDirectoryItemNumberOfContactMethods = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 102,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SelectedDirectoryItemNumberOfContactMethods",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("SelectedDirectoryItemContactMethodsXsig")]
        public JoinDataComplete SelectedDirectoryItemContactMethodsXsig = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 103,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SelectedDirectoryItemContactMethodsXsig",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("SelectedContactNumber")]
        public JoinDataComplete SelectedContactNumber = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 104,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SelectedContactNumber",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("ClearPhonebookSearch")]
        public JoinDataComplete ClearPhonebookSearch = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 110,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "ClearPhonebookSearch",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("DialSelectedContact")]
        public JoinDataComplete DialSelectedContact = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 106,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DialSelectedContact",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });


        [JoinName("SelectRecentCall")]
        public JoinDataComplete SelectRecentCall = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 180,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SelectRecentCall",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("SelectRecentName")]
        public JoinDataComplete SelectRecentName = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 171,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SelectRecentName",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("SelectRecentNumber")]
        public JoinDataComplete SelectRecentNumber = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 171,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SelectRecentNumber",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("Recents")]
        public JoinDataComplete Recents = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 181,
                JoinSpan = 10
            },
            new JoinMetadata
            {
                Description = "Recents",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("CameraSelect")]
        public JoinDataComplete CameraSelect = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 60,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "CameraSelect",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("CameraPresetActivate")]
        public JoinDataComplete CameraPresetActivate = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 121,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "CameraPresetActivate",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("FarEndCameraPresetActivate")]
        public JoinDataComplete FarEndCameraPresetActivate = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 122,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "FarEndCameraPresetActivate",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("CameraPresetStore")]
        public JoinDataComplete CameraPresetStore = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 123,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "CameraPresetStore",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("Coms")]
        public JoinDataComplete Coms = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 5,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Coms",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Serial
            });

        public CiscoBaseJoinMap(uint joinStart)
            : base(joinStart, typeof(CiscoBaseJoinMap))
        {
        }
    }
}