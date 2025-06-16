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
using PepperDash.Essentials.Devices.Common.Cameras;
using PepperDash.Essentials.Devices.Common.Codec;
using PepperDash.Essentials.Devices.Common.VideoCodec;

namespace epi_videoCodec_ciscoExtended.V2
{
	public class CiscoRoomOsDevice : EssentialsDevice, IHasCodecCameras, IOnline, ICommunicationMonitor, IBridgeAdvanced, IBasicVolumeWithFeedback, IPrivacy, IHasDoNotDisturbMode, IHasCodecLayoutsAvailable
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
		internal readonly CiscoLayouts Layouts;

		internal readonly uint phoneBookLimit;

		private readonly IBasicCommunication _communications;
		private readonly IList<CiscoRoomOsFeature> features = new List<CiscoRoomOsFeature>();
		private readonly CTimer requestTimeout;

		private CTimer _pollTimer;

		private readonly string _username = String.Empty;
		private readonly string _password = String.Empty;

		private readonly bool _isSerialComm;

		private bool _isLoggedIn;

		public bool IsLoggedIn
		{
			get { return _isLoggedIn; }
			set
			{
				if (value == _isLoggedIn) return;
				_isLoggedIn = value;

				IsLoggedInFeedback.FireUpdate();

				if (_isSerialComm)
				{
					if (_isLoggedIn)
					{
						_communications.TextReceived -= OnTextReceived;
						if (_pollTimer == null)
							PollTimerStart();
						else
							PollTimerReset(250, 30000);
					}
					else
					{
						_communications.TextReceived += OnTextReceived;
						//CommunicationMonitor.Stop();
						PollTimerStop();
					}
				}
				else
				{
					CommunicationMonitor.Start();
					PollTimerStart();	
				}
			}
		}

		public BoolFeedback IsLoggedInFeedback;

		private StringBuilder _buffer = new StringBuilder();

		public CiscoRoomOsDevice(string key, string name, CiscoCodecConfig props, IBasicCommunication communications)
			: base(key, name)
		{
			_username = props.Username;
			_password = props.Password;
			phoneBookLimit = props.PhonebookResultsLimit;

			_communications = communications;
			_communications.TextReceived += OnTextReceived;

			var gather = new CommunicationGather(_communications, Delimiter);
			gather.LineReceived += OnLineRecevied;

			var socket = _communications as ISocketStatus;
			if (socket != null)
				socket.ConnectionChange += OnSocketOnConnectionChange;

			_isSerialComm = (socket == null) || props.SerialOverIp;
			Debug.Console(0, this, Debug.ErrorLogLevel.Notice, "Constructor: _isSerialComm == {0}", _isSerialComm);
			
			if (props.CommunicationMonitorProperties != null)
			{
				CommunicationMonitor = new GenericCommunicationMonitor(
					this,
					_communications,
					props.CommunicationMonitorProperties);
			}
			else
			{
				const string pollString = "xStatus SystemUnit Hardware\r";
				CommunicationMonitor = new GenericCommunicationMonitor(
					this,
					_communications,
					30000,
					1800000,
					3000000,
					() => SendText(pollString));
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
			Layouts = new CiscoLayouts(this);

			features.Add(CodecCameras);
			features.Add(Standby);
			features.Add(CallStatus);
			features.Add(Presentation);
			features.Add(Audio);
			features.Add(Directory);
			features.Add(DoNotDisturb);
			features.Add(SelfView);
			features.Add(Recents);
			features.Add(Layouts);

			CodecCameras.CameraSelected += CameraSelected;
			Layouts.AvailableLayoutsChanged += AvailableLayoutsChanged;
			Layouts.CurrentLayoutChanged += CurrentLayoutChanged;

			IsLoggedInFeedback = new BoolFeedback(() => IsLoggedIn);

			requestTimeout = new CTimer(_ =>
			{
				var now = DateTime.UtcNow;

				var handlersToRemove =
					requestCallbacks.Where(h => (now - h.Value.At) > TimeSpan.FromMinutes(1))
						.Select(h => h.Key);

				foreach (var handlerKey in handlersToRemove)
				{
					requestSync.Enter();
					try
					{
						requestCallbacks.Remove(handlerKey);
					}
					finally
					{
						requestSync.Leave();
					}
				}
			}, Timeout.Infinite);
		}

		public override void Initialize()
		{
			IsLoggedIn = false;

			_communications.Connect();
			CommunicationMonitor.Start();

			CrestronEnvironment.ProgramStatusEventHandler += OnProgramStatusEventHandler;
		}

		private void OnProgramStatusEventHandler(eProgramStatusEventType type)
		{
			if (type == eProgramStatusEventType.Stopping)
			{
				PollTimerStop();
			}
		}

		private void OnSocketOnConnectionChange(object sender, GenericSocketStatusChageEventArgs args)
		{
			switch (args.Client.ClientStatus)
			{
				case SocketStatus.SOCKET_STATUS_CONNECTED:
				{
					if (_isSerialComm)
					{
						// TODO
					}
					else
					{
						// TODO
						PollTimerStart();
					}
					break;
				}
				default:
				{
					PollTimerStop();
					break;
				}
			}
		}

		private void OnTextReceived(object sender, GenericCommMethodReceiveTextArgs args)
		{
			var data = args.Text.Trim();
			//Debug.Console(2, this, "OnTextReceived: {0}", data);

			if (data.ToLower().StartsWith("login:"))
			{
				Debug.Console(0, this, "OnTextReceived: data == '{0}'", data);
				Debug.Console(0, this, "OnTextReceived: login request, sending '{0}'", _username);
				IsLoggedIn = false;
				//var text = PreProcessStringToSend(_username);
				//_communications.SendText(text);
				SendText(_username);
			}
			else if (data.ToLower().StartsWith("password:"))
			{
				Debug.Console(0, this, "OnTextReceived: data == '{0}'", data);
				Debug.Console(0, this, "OnTextReceived: password request, sending '{0}'", _password);
				IsLoggedIn = false;
				//var text = PreProcessStringToSend(_password);
				//_communications.SendText(text);
				SendText(_password);
			}
			else if (data.ToLower().Contains("login incorrect"))
			{
				IsLoggedIn = false;
			}
		}

		private void OnLineRecevied(object sender, GenericCommMethodReceiveTextArgs args)
		{
			var data = args.Text.Trim();
			//Debug.Console(2, this, "OnLineRecieved: '{0}'", data);

			if (string.IsNullOrEmpty(data)
				|| data == "Command not recognized."
				|| data == "OK"
				|| data == "ERROR")
			{
				return;
			}

			if (data.StartsWith("*s SystemUnit"))
			{
				if (!_isSerialComm)
					IsLoggedIn = true;
			}
			if (data.StartsWith("*r Login successful") || data.Contains("Welcome to"))
			{
				IsLoggedIn = true;
			}
			else if (data.ToLower().StartsWith("login incorrect"))
			{
				IsLoggedIn = false;
			}
			else if (data == "** end")
			{
				var dataToProcess = _buffer.ToString();
				_buffer = new StringBuilder();

				if (!string.IsNullOrEmpty(dataToProcess))
				{
					ProcessResponse(dataToProcess);
				}
			}
			else if (data.ToLower().StartsWith("login:"))
			{
				Debug.Console(0, this, "OnLineRecevied: data == '{0}'", data);
				Debug.Console(0, this, "OnLineRecevied: login request, sending '{0}'", _username);
				IsLoggedIn = false;
				//var text = PreProcessStringToSend(_username);
				//_communications.SendText(text);
				SendText(_username);
			}
			else if (data.ToLower().StartsWith("password:"))
			{
				Debug.Console(0, this, "OnLineRecevied: data == '{0}'", data);
				Debug.Console(0, this, "OnLineRecevied: password request, sending '{0}'", _password);
				IsLoggedIn = false;
				//var text = PreProcessStringToSend(_password);
				//_communications.SendText(text);
				SendText(_password);
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
				IsLoggedIn = false;
			}
			else
			{
				_buffer.Append(data);
				_buffer.Append("|");
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

		public void SendText(string text)
		{
			if (string.IsNullOrEmpty(text))
				return;

			var textToSend = PreProcessStringToSend(text);
			_communications.SendText(textToSend);
		}

		public static string PreProcessStringToSend(string text)
		{
			const string delimiter = "\r";
			return text.Trim() + delimiter;
		}

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
				requestTimeout.Reset(60000, 60000);
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

		public void PollTimerStart()
		{
			if (_pollTimer == null)
			{
				Debug.Console(1, this, Debug.ErrorLogLevel.Warning, "PollTimerStart: _pollTimer is null, creating new timer");
				_pollTimer = new CTimer(
					_ =>
					{
						PollFeatures();
						PollForFeedback();
					},
					Timeout.Infinite);
			}

			if (_isSerialComm)
			{
				Rs232LoggedIn += (sender, args) =>
				{
					Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "PollTimerStart: Rs232Logged In, resetting timer, polling or features and feedback...");
					PollTimerReset(250, 30000);
					PollFeatures();
					PollForFeedback();
				};
			}
		}

		public void PollTimerStop()
		{
			if (_pollTimer == null)
			{
				Debug.Console(1, this, Debug.ErrorLogLevel.Warning, "PollTimerStop: _pollTimer is null");
				return;
			}

			_pollTimer.Stop();
		}

		public void PollTimerReset(long dueTime, long repeatPeriod)
		{
			if (_pollTimer == null)
			{
				Debug.Console(1, this, Debug.ErrorLogLevel.Warning, "PollTimerReset: _pollTimer is null");
				return;
			}
			_pollTimer.Reset(dueTime, repeatPeriod);
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
			var joinMap = new CiscoJoinMap(joinStart);
			if (bridge != null)
				bridge.AddJoinMap(Key, joinMap);

			IsOnline.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
			IsLoggedInFeedback.LinkInputSig(trilist.BooleanInput[joinMap.IsLoggedIn.JoinNumber]);

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
			Standby.StandbyIsOnFeedback.LinkComplementInputSig(trilist.BooleanInput[joinMap.StandbyOff.JoinNumber]);
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
				var index = (int)x;

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
				trilist.SetUshort(joinMap.CameraSelect.JoinNumber, (ushort)selectedCameraIndex);
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

			trilist.SetSigTrueAction(joinMap.GetRecents.JoinNumber, () => Recents.RefreshHistoryList());



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


			trilist.SetSigTrueAction(joinMap.ToggleLayout.JoinNumber, LocalLayoutToggle);
			trilist.SetStringSigAction(joinMap.SelectLayout.JoinNumber, LayoutSet);
			Layouts.LocalLayoutFeedback.LinkInputSig(trilist.StringInput[joinMap.CurrentLayout.JoinNumber]);
			Layouts.AvailableLayoutsFeedback.LinkInputSig(trilist.StringInput[joinMap.AvailableLayouts.JoinNumber]);

			// we may need these, just need to verify what's available on the bridge
			// trilist.SetSigTrueAction(joinMap.SelfviewOn.JoinNumber, () => SelfView.SelfViewModeOn());
			// trilist.SetSigTrueAction(joinMap.SelfviewOff.JoinNumber, () => SelfView.SelfViewModeOff());

			trilist.SetSigTrueAction(joinMap.SelfviewToggle.JoinNumber, () => SelfView.SelfViewModeToggle());
			SelfView.SelfviewIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.SelfviewToggle.JoinNumber]);


			trilist.SetUShortSigAction(joinMap.NearEndPresentationSource.JoinNumber, value => Presentation.SetShareSource(value));
			Presentation.SharingSourceIntFeedback.LinkInputSig(trilist.UShortInput[joinMap.NearEndPresentationSource.JoinNumber]);

			_communications.TextReceived += (sender, args) => trilist.SetString(joinMap.Coms.JoinNumber, args.Text);
			trilist.SetStringSigAction(joinMap.Coms.JoinNumber, value => _communications.SendText(value));
		}

		public void VolumeUp(bool pressRelease)
		{
			((IBasicVolumeControls)Audio).VolumeUp(pressRelease);
		}

		public void VolumeDown(bool pressRelease)
		{
			((IBasicVolumeControls)Audio).VolumeDown(pressRelease);
		}

		public void MuteToggle()
		{
			((IBasicVolumeControls)Audio).MuteToggle();
		}

		public void MuteOn()
		{
			((IBasicVolumeWithFeedback)Audio).MuteOn();
		}

		public void MuteOff()
		{
			((IBasicVolumeWithFeedback)Audio).MuteOff();
		}

		public void SetVolume(ushort level)
		{
			((IBasicVolumeWithFeedback)Audio).SetVolume(level);
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
			((IPrivacy)Audio).PrivacyModeOn();
		}

		public void PrivacyModeOff()
		{
			((IPrivacy)Audio).PrivacyModeOff();
		}

		public void PrivacyModeToggle()
		{
			((IPrivacy)Audio).PrivacyModeToggle();
		}

		public BoolFeedback PrivacyModeIsOnFeedback
		{
			get { return Audio.PrivacyModeIsOnFeedback; }
		}

		public void ActivateDoNotDisturbMode()
		{
			((IHasDoNotDisturbMode)DoNotDisturb).ActivateDoNotDisturbMode();
		}

		public void DeactivateDoNotDisturbMode()
		{
			((IHasDoNotDisturbMode)DoNotDisturb).DeactivateDoNotDisturbMode();
		}

		public void ToggleDoNotDisturbMode()
		{
			((IHasDoNotDisturbMode)DoNotDisturb).ToggleDoNotDisturbMode();
		}

		public BoolFeedback DoNotDisturbModeIsOnFeedback
		{
			get { return DoNotDisturb.DoNotDisturbModeIsOnFeedback; }
		}

		public void LocalLayoutToggle()
		{
			((IHasCodecLayouts)Layouts).LocalLayoutToggle();
		}

		public void LocalLayoutToggleSingleProminent()
		{
			((IHasCodecLayouts)Layouts).LocalLayoutToggleSingleProminent();
		}

		public void MinMaxLayoutToggle()
		{
			((IHasCodecLayouts)Layouts).MinMaxLayoutToggle();
		}

		public StringFeedback LocalLayoutFeedback
		{
			get { return Layouts.LocalLayoutFeedback; }
		}

		public event EventHandler<AvailableLayoutsChangedEventArgs> AvailableLayoutsChanged;

		public event EventHandler<CurrentLayoutChangedEventArgs> CurrentLayoutChanged;

		public StringFeedback AvailableLayoutsFeedback
		{
			get { return Layouts.AvailableLayoutsFeedback; }
		}

		public List<CodecCommandWithLabel> AvailableLayouts
		{
			get { return Layouts.AvailableLayouts; }
		}

		public void LayoutSet(string layout)
		{
			((IHasCodecLayoutsAvailable)Layouts).LayoutSet(layout);
		}

		public void LayoutSet(CodecCommandWithLabel layout)
		{
			((IHasCodecLayoutsAvailable)Layouts).LayoutSet(layout);
		}
	}
}