using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Prng;
using PepperDash.Core;
using PepperDash.Core.Intersystem;
using PepperDash.Core.Intersystem.Tokens;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.DeviceInfo;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Core.Queues;
using PepperDash.Essentials.Devices.Common.Cameras;
using PepperDash.Essentials.Devices.Common.Codec;
using PepperDash.Essentials.Devices.Common.Codec.Cisco;
using PepperDash.Essentials.Devices.Common.VideoCodec;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Enums;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Interfaces;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Config;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Navigator;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.WebView;
using Serilog.Events;
using Feedback = PepperDash.Essentials.Core.Feedback;


namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{

	/// <summary>
	/// Extended Cisco video codec implementation that provides comprehensive control and monitoring capabilities for Cisco Room OS devices.
	/// This class extends VideoCodecBase and implements numerous interfaces to support advanced features including
	/// call management, directory services, camera control, UI extensions, scheduling, and occupancy sensing.
	/// </summary>
	/// <remarks>
	/// This implementation supports a wide range of Cisco codec models running Room OS and provides:
	/// - Call history and favorites management
	/// - Directory and phonebook integration
	/// - Camera tracking (SpeakerTrack and PresenterTrack)
	/// - UI extensions for custom panels
	/// - Room preset management
	/// - External source switching
	/// - Do Not Disturb and half-wake modes
	/// - Occupancy and people count monitoring
	/// - WebEx integration and branding
	/// </remarks>
	public partial class CiscoCodec
		: VideoCodecBase,
			IHasCallHistory,
			IHasCallFavorites,
			IHasDirectory,
			IHasScheduleAwareness,
			IOccupancyStatusProvider,
			IHasCodecLayoutsAvailable,
			IHasCodecSelfView,
			ICommunicationMonitor,
			IRoutingSinkWithSwitching,
			IRoutingSource,
			IHasCodecCameras,
			IHasCameraAutoMode,
			IHasCodecRoomPresets,
			IHasExternalSourceSwitching,
			IHasBranding,
			IHasCameraOff,
			IHasCameraMute,
			IHasDoNotDisturbMode,
			IHasHalfWakeMode,
			IHasCallHold,
			IJoinCalls,
			IDeviceInfoProvider,
			IHasPhoneDialing,
			ICiscoCodecCameraConfig,
			ISpeakerTrack,
			IPresenterTrack,
			IEmergencyOSD,
			IHasWebView
	{
		public event EventHandler<AvailableLayoutsChangedEventArgs> AvailableLayoutsChanged;
		public event EventHandler<CurrentLayoutChangedEventArgs> CurrentLayoutChanged;
		private event EventHandler<MinuteChangedEventArgs> MinuteChanged;
		public event EventHandler<CodecInfoChangedEventArgs> CodecInfoChanged;
		public event EventHandler<CameraTrackingCapabilitiesArgs> CameraTrackingCapabilitiesChanged;
		public event EventHandler<WebViewStatusChangedEventArgs> WebViewStatusChanged;

		public eCameraTrackingCapabilities CameraTrackingCapabilities { get; private set; }

		private CTimer _scheduleCheckTimer;
		private DateTime _scheduleCheckLast;

		public Meeting ActiveMeeting { get; private set; }

		private const int XSigEncoding = 28591;
		public bool EndAllCallsOnMeetingJoin;
		private List<Meeting> _currentMeetings;

		private StringBuilder _feedbackListMessage;

		private bool _feedbackListMessageIncoming;

		private int _selectedPreset;

		private bool _IsInPresentation;

		private MediaChannelStatus _incomingPresentation;

		private readonly Version _testedCodecFirmware = new Version("10.11.5.2");
		private readonly Version _enhancedLayoutsFirmware = new Version("9.15.10.8");
		private readonly Version _regressionFirmware = new Version("9.15.3.26");
		private readonly Version _zoomDialFeatureFirmware = new Version("11.1.0.0");
		public Version CodecFirmware { get; private set; }

		private bool EnhancedLayouts
		{
			get
			{
				if (CodecFirmware == null)
					return false;
				var returnValue = CodecFirmware.CompareTo(_enhancedLayoutsFirmware) >= 0;
				return returnValue;
			}
		}

		private bool ZoomDialerFirmware
		{
			get
			{
				var returnValue = FirmwareCompare(_zoomDialFeatureFirmware);
				this.LogVerbose("Zoom Dialer Functionality is {0}.", returnValue ? "enabled" : "disabled");
				return returnValue;
			}
		}

		public readonly WebexPinRequestHandler WebexPinRequestHandler;
		public readonly DoNotDisturbHandler DoNotDisturbHandler;
		public readonly UIExtensionsHandler UIExtensionsHandler;

		private Meeting _currentMeeting;

		private StandbyState _standbyState;
		private bool _presentationActive;
		public UiExtensions UiExtensions { get; set; }

		private readonly bool _phonebookAutoPopulate;
		private bool _phonebookInitialSearch;

		private string _lastSearched;
		private CiscoCodecConfig _config;
		private readonly int _joinableCooldownSeconds;

		public string ZoomMeetingId { get; private set; }
		public string ZoomMeetingPasscode { get; private set; }
		public string ZoomMeetingCommand { get; private set; }
		public string ZoomMeetingHostKey { get; private set; }
		public string ZoomMeetingReservedCode { get; private set; }
		public string ZoomMeetingDialCode { get; private set; }
		public string ZoomMeetingIp { get; private set; }

		public string WebexMeetingNumber { get; private set; }
		public string WebexMeetingRole { get; private set; }
		public string WebexMeetingPin { get; private set; }

		public eCodecPresentationStates PresentationStates;

		private bool _externalSourceChangeRequested;

		public bool Room { get; private set; }

		public event EventHandler<DirectoryEventArgs> DirectoryResultReturned;

		private CTimer _brandingTimer;
		private CTimer _registrationCheckTimer;

		public CommunicationGather PortGather { get; private set; }

		public StatusMonitorBase CommunicationMonitor { get; private set; }

		private readonly GenericQueue _receiveQueue;

		public BoolFeedback PresentationViewMaximizedFeedback { get; private set; }

		public BoolFeedback PresentationViewMinimizedFeedback { get; private set; }

		public BoolFeedback PresentationViewDefaultFeedback { get; private set; }

		public FeedbackGroup PresentationViewFeedbackGroup { get; private set; }

		private string _currentPresentationView;

		private eCameraTrackingCapabilities PreferredTrackingMode { get; set; }

		public BoolFeedback RoomIsOccupiedFeedback { get; private set; }

		public IntFeedback PeopleCountFeedback { get; private set; }

		public BoolFeedback SelfviewIsOnFeedback { get; private set; }

		public StringFeedback SelfviewPipPositionFeedback { get; private set; }

		public StringFeedback LocalLayoutFeedback { get; private set; }

		public BoolFeedback PresentationActiveFeedback { get; private set; }

		public bool SpeakerTrackAvailability { get; private set; }
		public bool SpeakerTrackStatus { get; private set; }
		public bool PresenterTrackAvailability { get; private set; }
		public bool PresenterTrackStatus { get; private set; }
		public bool WebviewIsVisible { get; private set; }
		public string PresenterTrackStatusName { get; private set; }

		private string _currentLayoutBacker;

		private string CurrentLayout
		{
			get { return _currentLayoutBacker != "Grid" ? _currentLayoutBacker : "Side by Side"; }
			set { _currentLayoutBacker = value; }
		}

		public StringFeedback AvailableLayoutsFeedback { get; private set; }

		public BoolFeedback LocalLayoutIsProminentFeedback { get; private set; }

		public BoolFeedback FarEndIsSharingContentFeedback { get; private set; }

		#region AutoCamera Feedbacks

		public BoolFeedback CameraAutoModeIsOnFeedback { get; private set; }
		public BoolFeedback SpeakerTrackStatusOnFeedback { get; private set; }
		public BoolFeedback PresenterTrackStatusOnFeedback { get; private set; }

		public StringFeedback PresenterTrackStatusNameFeedback { get; private set; }
		public BoolFeedback PresenterTrackStatusOffFeedback { get; private set; }
		public BoolFeedback PresenterTrackStatusFollowFeedback { get; private set; }
		public BoolFeedback PresenterTrackStatusBackgroundFeedback { get; private set; }
		public BoolFeedback PresenterTrackStatusPersistentFeedback { get; private set; }

		public BoolFeedback CameraAutoModeAvailableFeedback { get; private set; }
		public BoolFeedback SpeakerTrackAvailableFeedback { get; private set; }
		public BoolFeedback PresenterTrackAvailableFeedback { get; private set; }
		public BoolFeedback DirectorySearchInProgress { get; private set; }

		public FeedbackGroup PresenterTrackFeedbackGroup { get; private set; }

		#endregion

		public IntFeedback RingtoneVolumeFeedback { get; private set; }

		public List<CodecCommandWithLabel> AvailableLayouts { get; private set; }

		private CodecCommandWithLabel _currentSelfviewPipPosition;

		private readonly List<CodecCommandWithLabel> _legacyLayouts =
			new List<CodecCommandWithLabel>()
			{
                //new CodecCommandWithLabel("auto", "Auto"),
                //new CiscoCodecLocalLayout("custom", "Custom"),    // Left out for now
                new CodecCommandWithLabel("equal", "Equal"),
				new CodecCommandWithLabel("overlay", "Overlay"),
				new CodecCommandWithLabel("prominent", "Prominent"),
				new CodecCommandWithLabel("single", "Single")
			};

		private CodecCommandWithLabel _currentLegacyLayout;

		public List<CodecCommandWithLabel> SelfviewPipPositions = new List<CodecCommandWithLabel>()
		{
			new CodecCommandWithLabel("CenterLeft", "Center Left"),
			new CodecCommandWithLabel("CenterRight", "Center Right"),
			new CodecCommandWithLabel("LowerLeft", "Lower Left"),
			new CodecCommandWithLabel("LowerRight", "Lower Right"),
			new CodecCommandWithLabel("UpperCenter", "Upper Center"),
			new CodecCommandWithLabel("UpperLeft", "Upper Left"),
			new CodecCommandWithLabel("UpperRight", "Upper Right"),
		};

		private CiscoCodecConfiguration.RootObject CodecConfiguration =
			new CiscoCodecConfiguration.RootObject();

		private CiscoCodecStatus.RootObject CodecStatus;

		private CiscoCodecEvents.RootObject CodecEvents = new CiscoCodecEvents.RootObject();

		public CodecCallHistory CallHistory { get; private set; }

		public CodecCallFavorites CallFavorites { get; private set; }

		public CodecDirectory DirectoryRoot { get; private set; }

		public CodecDirectory CurrentDirectoryResult
		{
			get
			{
				if (DirectoryBrowseHistory.Count > 0)
					return DirectoryBrowseHistory[DirectoryBrowseHistory.Count - 1];
				else
					return DirectoryRoot;
			}
		}

		public BoolFeedback CurrentDirectoryResultIsNotDirectoryRoot { get; private set; }

		public List<CodecDirectory> DirectoryBrowseHistory { get; private set; }

		public CodecScheduleAwareness CodecSchedule { get; private set; }

		protected override Func<int> VolumeLevelFeedbackFunc
		{
			get
			{
				return () =>
					CrestronEnvironment.ScaleWithLimits(
						CodecStatus.Status.Audio.Volume.IntValue,
						100,
						0,
						65535,
						0
					);
			}
		}

		protected override Func<bool> PrivacyModeIsOnFeedbackFunc
		{
			get { return () => CodecStatus.Status.Audio.Microphones.Mute.BoolValue; }
		}

		protected override Func<bool> StandbyIsOnFeedbackFunc
		{
			get { return () => _standbyState == StandbyState.Standby; }
		}

		protected override Func<string> SharingSourceFeedbackFunc
		{
			get { return () => _presentationSourceKey; }
		}

		protected override Func<bool> SharingContentIsOnFeedbackFunc
		{
			get
			{
				return () => PresentationActiveFeedback.BoolValue;
			}
		}

		protected Func<bool> FarEndIsSharingContentFeedbackFunc
		{
			get
			{
				return () =>
					CodecStatus
						.Status
						.StatusConference
						.Presentation
						.ModeValueProperty
						.ReceivingBoolValue;
			}
		}

		protected override Func<bool> MuteFeedbackFunc
		{
			get { return () => CodecStatus.Status.Audio.VolumeMute.BoolValue; }
		}

		protected Func<bool> RoomIsOccupiedFeedbackFunc
		{
			get { return () => CodecStatus.Status.RoomAnalytics.PeoplePresence.BoolValue; }
		}

		protected Func<bool> PhoneOffHookFeedbackFunc
		{
			get { return CheckAudioCallActive; }
		}

		protected Func<string> CallerIdNumberFeedbackFunc
		{
			get { return GetCallerIdNumber; }
		}

		private string GetCallerIdNumber()
		{
			var activeAudioCall = ActiveCalls.FirstOrDefault(o =>
				o.Type == eCodecCallType.Audio && o.Direction == eCodecCallDirection.Incoming
			);
			return activeAudioCall == null ? "" : activeAudioCall.Number;
		}

		private string GetCallerIdName()
		{
			var activeAudioCall = ActiveCalls.FirstOrDefault(o =>
				o.Type == eCodecCallType.Audio && o.Direction == eCodecCallDirection.Incoming
			);
			return activeAudioCall == null ? "" : activeAudioCall.Name;
		}

		protected Func<string> CallerIdNameFeedbackFunc
		{
			get { return GetCallerIdName; }
		}

		private bool CheckAudioCallActive()
		{
			var activeAudioCall = ActiveCalls.FirstOrDefault(o => o.Type == eCodecCallType.Audio);
			return activeAudioCall != null;
		}

		protected Func<bool> PresentationActiveFeedbackFunc
		{
			get
			{
				return () => /*CodecStatus.Status.StatusConference.Presentation.ModeValueProperty.ActiveBoolValue*/
					_presentationActive;
			}
		}

		protected Func<int> PeopleCountFeedbackFunc
		{
			get
			{
				return () =>
					CodecStatus.Status.RoomAnalytics.PeopleCount.CurrentPeopleCount.IntValue;
			}
		}

		protected Func<string> AvailableLayoutsFeedbackFunc
		{
			get { return () => UpdateLayoutsXSig(AvailableLayouts); }
		}

		protected Func<bool> SelfViewIsOnFeedbackFunc
		{
			get { return () => CodecStatus.Status.Video.Selfview.SelfViewMode.BoolValue; }
		}

		protected Func<string> SelfviewPipPositionFeedbackFunc
		{
			get { return () => _currentSelfviewPipPosition.Label; }
		}

		protected Func<string> LocalLayoutFeedbackFunc
		{
			get { return () => CurrentLayout; }
		}

		protected Func<bool> LocalLayoutIsProminentFeedbackFunc
		{
			get
			{
				return () => CurrentLayout.Contains("Prominent") || CurrentLayout.Contains("Stack");
			}
		}

		private Func<JObject, string, JToken> JTokenValidInObject = CheckJTokenInObject;

		private Func<JToken, string, JToken> JTokenValidInToken = CheckJTokenInToken;

		private static JToken CheckJTokenInToken(JToken jToken, string tokenSelector)
		{
			try
			{
				if (jToken == null)
				{
					return null;
				}

				if (string.IsNullOrEmpty(tokenSelector))
				{
					return null;
				}

				return jToken.SelectToken(tokenSelector, false);
			}
			catch (Exception ex)
			{
				Debug.LogError("Exception in CheckJTokenInToken: {message}", ex.Message);
				Debug.LogVerbose(ex, "Exception");
				return null;
			}
		}

		private static JToken CheckJTokenInObject(JObject jObject, string tokenSelector)
		{
			try
			{
				if (jObject == null)
				{
					return null;
				}

				if (string.IsNullOrEmpty(tokenSelector))
				{
					return null;
				}

				return jObject.SelectToken(tokenSelector, false);
			}
			catch (Exception ex)
			{
				Debug.LogError("Exception: {message}", ex.Message);
				Debug.LogVerbose(ex, "Exception");
				return null;
			}
		}

		private bool FirmwareCompare(Version ver)
		{
			if (CodecFirmware == null)
				return false;
			var returnValue = CodecFirmware.CompareTo(ver) >= 0;
			return returnValue;
		}

		#region CameraAutoTrackingFeedbackFunc


		protected Func<bool> CameraTrackingAvailableFeedbackFunc
		{
			get { return () => PresenterTrackAvailability || SpeakerTrackAvailability; }
		}

		protected Func<bool> CameraTrackingOnFeedbackFunc
		{
			get
			{
				return () =>
					(SpeakerTrackAvailability && SpeakerTrackStatus)
					|| (PresenterTrackAvailability && PresenterTrackStatus);
			}
		}

		protected Func<bool> PresenterTrackAvailableFeedbackFunc
		{
			get { return () => PresenterTrackAvailability; }
		}

		protected Func<bool> SpeakerTrackAvailableFeedbackFunc
		{
			get { return () => SpeakerTrackAvailability; }
		}

		protected Func<bool> SpeakerTrackStatusOnFeedbackFunc
		{
			get { return () => SpeakerTrackStatus; }
		}

		protected Func<string> PresenterTrackStatusNameFeedbackFunc
		{
			get { return () => PresenterTrackStatusName; }
		}

		protected Func<bool> PresenterTrackStatusOnFeedbackFunc
		{
			get
			{
				return () =>
					PresenterTrackStatus || string.IsNullOrEmpty(PresenterTrackStatusName);
			}
		}

		protected Func<bool> PresenterTrackStatusOffFeedbackFunc
		{
			get { return () => PresenterTrackStatusName == "off"; }
		}

		protected Func<bool> PresenterTrackStatusFollowFeedbackFunc
		{
			get { return () => PresenterTrackStatusName == "follow"; }
		}

		protected Func<bool> PresenterTrackStatusBackgroundFeedbackFunc
		{
			get { return () => PresenterTrackStatusName == "background"; }
		}

		protected Func<bool> PresenterTrackStatusPersistentFeedbackFunc
		{
			get { return () => PresenterTrackStatusName == "persistent"; }
		}

		#endregion


		public CodecSyncState SyncState { get; }

		public CodecPhonebookSyncState PhonebookSyncState { get; private set; }

		private StringBuilder _jsonMessage;

		private bool _jsonFeedbackMessageIsIncoming;

		public bool CommDebuggingIsOn;

		internal const string Delimiter = "\r\n";

		public IntFeedback PresentationSourceFeedback { get; private set; }

		public BoolFeedback PresentationSendingLocalOnlyFeedback { get; private set; }

		public BoolFeedback PresentationSendingLocalRemoteFeedback { get; private set; }

		public BoolFeedback ContentInputActiveFeedback { get; private set; }

		private int _presentationSource;

		private int _desiredPresentationSource;

		private string _presentationSourceKey;

		private bool _presentationLocalOnly;

		private bool _presentationLocalRemote;

		private string _phonebookMode = "Local"; // Default to Local

		private uint _phonebookResultsLimit = 255; // Could be set later by config.

		private CTimer _loginMessageReceivedTimer;
		private CTimer _retryConnectionTimer;

		// **___________________________________________________________________**
		//  Timers to be moved to the global system timer at a later point....
		private CTimer BookingsRefreshTimer;
		private CTimer PhonebookRefreshTimer;

		// **___________________________________________________________________**

		//public RoutingInputPort CodecOsdIn { get; private set; }

		public RoutingInputPort HdmiIn1 { get; private set; }
		public RoutingInputPort HdmiIn2 { get; private set; }
		public RoutingInputPort HdmiIn3 { get; private set; }
		public RoutingInputPort HdmiIn4 { get; private set; }
		public RoutingInputPort HdmiIn5 { get; private set; }
		public RoutingInputPort SdiInput { get; private set; }


		public RoutingOutputPort HdmiOut1 { get; private set; }
		public RoutingOutputPort HdmiOut2 { get; private set; }
		public RoutingOutputPort HdmiOut3 { get; private set; }

		public ExtensionsHandler UiExtensionsHandler { get; set; }

		private readonly IBasicCommunication _comms;

		// Constructor for IBasicCommunication
		public CiscoCodec(DeviceConfig config, IBasicCommunication comm)
			: base(config)
		{
			CodecStatus = new CiscoCodecStatus.RootObject();
			DeviceInfo = new DeviceInfo();
			CodecInfo = new CiscoCodecInfo(this);
			_lastSearched = string.Empty;
			_phonebookInitialSearch = true;
			CurrentLayout = string.Empty;
			_receiveQueue = new GenericQueue(Key + "-queue", 500);
			WebexPinRequestHandler = new WebexPinRequestHandler(this, comm, _receiveQueue);
			DoNotDisturbHandler = new DoNotDisturbHandler(this, comm, _receiveQueue);
			UIExtensionsHandler = new UIExtensionsHandler(this, comm, _receiveQueue);
			_comms = comm;

			CrestronEnvironment.ProgramStatusEventHandler += a =>
			{
				if (a != eProgramStatusEventType.Stopping)
					return;
				EndGracefully();
			};

			CrestronEnvironment.SystemEventHandler += a =>
			{
				if (a != eSystemEventType.Rebooting)
					return;
				EndGracefully();
			};

			var props = JsonConvert.DeserializeObject<CiscoCodecConfig>(
				config.Properties.ToString()
			);

			UiExtensions = props.Extensions;
			if (props?.Extensions?.ConfigId > 0)
			{
				UiExtensionsHandler = new ExtensionsHandler(
					this,
					EnqueueCommand
				);
			}

			_scheduleCheckTimer = new CTimer(ScheduleTimeCheck, null, 0, 15000);

			_config = props;

			MeetingsToDisplay = _config.OverrideMeetingsLimit ? 50 : 0;
			_timeFormatSpecifier = _config.TimeFormatSpecifier ?? "t";
			_dateFormatSpecifier = _config.DateFormatSpecifier ?? "d";
			_joinableCooldownSeconds = _config.JoinableCooldownSeconds;
			EndAllCallsOnMeetingJoin = _config.EndAllCallsOnMeetingJoin;

			if (_config.Sharing != null)
			{
				PresentationStates = _config.Sharing.DefaultShareLocalOnly
					? eCodecPresentationStates.LocalOnly
					: eCodecPresentationStates.LocalRemote;
			}
			else
			{
				PresentationStates = eCodecPresentationStates.LocalRemote;
			}

			PreferredTrackingMode = eCameraTrackingCapabilities.SpeakerTrack;

			var trackingMode = _config.DefaultCameraTrackingMode ?? string.Empty;

			if (!string.IsNullOrEmpty(trackingMode))
			{
				if (trackingMode.Contains("presenter"))
				{
					PreferredTrackingMode = eCameraTrackingCapabilities.PresenterTrack;
				}
			}

			// Use the configured phonebook results limit if present
			if (props.PhonebookResultsLimit > 0)
			{
				_phonebookResultsLimit = props.PhonebookResultsLimit;
			}

			// The queue that will collect the repsonses in the order they are received

			RoomIsOccupiedFeedback = new BoolFeedback(RoomIsOccupiedFeedbackFunc);
			PeopleCountFeedback = new IntFeedback(PeopleCountFeedbackFunc);
			SelfviewIsOnFeedback = new BoolFeedback(SelfViewIsOnFeedbackFunc);
			SelfviewPipPositionFeedback = new StringFeedback(SelfviewPipPositionFeedbackFunc);
			LocalLayoutFeedback = new StringFeedback(LocalLayoutFeedbackFunc);
			LocalLayoutIsProminentFeedback = new BoolFeedback(LocalLayoutIsProminentFeedbackFunc);
			FarEndIsSharingContentFeedback = new BoolFeedback(FarEndIsSharingContentFeedbackFunc);
			CameraIsOffFeedback = new BoolFeedback(
				() => CodecStatus.Status.Video.VideoInput.MainVideoMute.BoolValue
			);
			AvailableLayoutsFeedback = new StringFeedback(AvailableLayoutsFeedbackFunc);
			DirectorySearchInProgress = new BoolFeedback(() => _searchInProgress);
			PhoneOffHookFeedback = new BoolFeedback(PhoneOffHookFeedbackFunc);
			CallerIdNameFeedback = new StringFeedback(CallerIdNameFeedbackFunc);
			CallerIdNumberFeedback = new StringFeedback(CallerIdNumberFeedbackFunc);

			//PresentationActiveFeedback = new BoolFeedback(PresentationActiveFeedbackFunc);


			#region CameraAutoFeedbackRegistration

			CameraAutoModeIsOnFeedback = new BoolFeedback(CameraTrackingOnFeedbackFunc);
			SpeakerTrackStatusOnFeedback = new BoolFeedback(SpeakerTrackStatusOnFeedbackFunc);
			PresenterTrackStatusOnFeedback = new BoolFeedback(PresenterTrackStatusOnFeedbackFunc);

			PresenterTrackStatusNameFeedback = new StringFeedback(
				PresenterTrackStatusNameFeedbackFunc
			);
			PresenterTrackStatusOffFeedback = new BoolFeedback(PresenterTrackStatusOffFeedbackFunc);
			PresenterTrackStatusFollowFeedback = new BoolFeedback(
				PresenterTrackStatusFollowFeedbackFunc
			);
			PresenterTrackStatusBackgroundFeedback = new BoolFeedback(
				PresenterTrackStatusBackgroundFeedbackFunc
			);
			PresenterTrackStatusPersistentFeedback = new BoolFeedback(
				PresenterTrackStatusPersistentFeedbackFunc
			);

			CameraAutoModeAvailableFeedback = new BoolFeedback(CameraTrackingAvailableFeedbackFunc);
			PresenterTrackAvailableFeedback = new BoolFeedback(PresenterTrackAvailableFeedbackFunc);
			SpeakerTrackAvailableFeedback = new BoolFeedback(SpeakerTrackAvailableFeedbackFunc);

			PresenterTrackFeedbackGroup = new FeedbackGroup(
				new FeedbackCollection<Feedback>()
				{
					PresenterTrackStatusOnFeedback,
					PresenterTrackStatusNameFeedback,
					PresenterTrackStatusOffFeedback,
					PresenterTrackStatusFollowFeedback,
					PresenterTrackStatusBackgroundFeedback,
					PresenterTrackStatusPersistentFeedback
				}
			);

			#endregion



			CameraIsMutedFeedback = CameraIsOffFeedback;
			SupportsCameraOff = true;

			HalfWakeModeIsOnFeedback = new BoolFeedback(
				() => _standbyState == StandbyState.HalfWake
            );

			EnteringStandbyModeFeedback = new BoolFeedback(
				() => _standbyState == StandbyState.EnteringStandby
            );

			PresentationViewMaximizedFeedback = new BoolFeedback(
				() => _currentPresentationView == "Maximized"
			);
			PresentationViewMinimizedFeedback = new BoolFeedback(
				() => _currentPresentationView == "Minimized"
			);
			PresentationViewDefaultFeedback = new BoolFeedback(
				() => _currentPresentationView == "Default"
			);

			PresentationViewFeedbackGroup = new FeedbackGroup(
				new FeedbackCollection<Feedback>()
				{
					PresentationViewMaximizedFeedback,
					PresentationViewMinimizedFeedback,
					PresentationViewDefaultFeedback
				}
			);

			RingtoneVolumeFeedback = new IntFeedback(
				() => CodecConfiguration.Configuration.Audio.SoundsAndAlerts.RingVolume.Volume
			);

			PresentationSourceFeedback = new IntFeedback(() => _presentationSource);
			PresentationSendingLocalOnlyFeedback = new BoolFeedback(() => _presentationLocalOnly);
			PresentationSendingLocalRemoteFeedback = new BoolFeedback(
				() => _presentationLocalRemote
			);
			PresentationActiveFeedback = new BoolFeedback(() => _presentationActive);
			ContentInputActiveFeedback = new BoolFeedback(() => _presentationSource != 0);

			PresentationActiveFeedback.OutputChange += (o, a) => SharingContentIsOnFeedback.FireUpdate();

			Communication = comm;

			if (props.CommunicationMonitorProperties != null)
			{
				CommunicationMonitor = new GenericCommunicationMonitor(
					this,
					Communication,
					props.CommunicationMonitorProperties
				);
			}
			else
			{
				const string pollString =
					"xstatus systemunit\r"
					+ "xstatus cameras\r"
					+ "xstatus sip/registration\r"
					+ "xStatus Audio Volume\r";

				CommunicationMonitor = new GenericCommunicationMonitor(
					this,
					Communication,
					30000,
					120000,
					300000,
					pollString
				);
			}

			if (props.Sharing != null)
				AutoShareContentWhileInCall = props.Sharing.AutoShareContentWhileInCall;

			ShowSelfViewByDefault = props.ShowSelfViewByDefault;

			//DeviceManager.AddDevice(CommunicationMonitor);

			_phonebookMode = props.PhonebookMode;
			_phonebookAutoPopulate = !props.PhonebookDisableAutoPopulate;

			SyncState = new CodecSyncState(Key + "--Sync", this);

			PhonebookSyncState = new CodecPhonebookSyncState(Key + "--PhonebookSync");

			SyncState.InitialSyncCompleted += SyncState_InitialSyncCompleted;

			PortGather = new CommunicationGather(Communication, Delimiter)
			{
				IncludeDelimiter = true
			};
			PortGather.LineReceived += Port_LineReceived;

			CallHistory = new CodecCallHistory();

			if (props.Favorites != null)
			{
				CallFavorites = new CodecCallFavorites();
				CallFavorites.Favorites = props.Favorites;
			}

			DirectoryRoot = new CodecDirectory();

			DirectoryBrowseHistory = new List<CodecDirectory>();

			CurrentDirectoryResultIsNotDirectoryRoot = new BoolFeedback(
				() => DirectoryBrowseHistory.Count > 0
			);

			CurrentDirectoryResultIsNotDirectoryRoot.FireUpdate();

			CodecSchedule = new CodecScheduleAwareness();

			//Set Feedback Actions
			SetFeedbackActions();

			//CodecOsdIn = new RoutingInputPort(
			//	RoutingPortNames.CodecOsd,
			//	eRoutingSignalType.Audio | eRoutingSignalType.Video,
			//	eRoutingPortConnectionType.Hdmi,
			//	new Action(StopSharing),
			//	this
			//);
			HdmiIn1 = new RoutingInputPort(
	RoutingPortNames.HdmiIn1,
	eRoutingSignalType.Audio | eRoutingSignalType.Video,
	eRoutingPortConnectionType.Hdmi,
	new Action(SelectPresentationSource1),
	this
);
			HdmiIn2 = new RoutingInputPort(
	RoutingPortNames.HdmiIn2,
	eRoutingSignalType.Audio | eRoutingSignalType.Video,
	eRoutingPortConnectionType.Hdmi,
	new Action(SelectPresentationSource2),
	this
);
			HdmiIn3 = new RoutingInputPort(
				RoutingPortNames.HdmiIn3,
				eRoutingSignalType.Audio | eRoutingSignalType.Video,
				eRoutingPortConnectionType.Hdmi,
				new Action(() => SelectPresentationSource(3)),
				this
			);
			HdmiIn4 = new RoutingInputPort(
	RoutingPortNames.HdmiIn4,
	eRoutingSignalType.Audio | eRoutingSignalType.Video,
	eRoutingPortConnectionType.Hdmi,
	new Action(() => SelectPresentationSource(4)),
	this
);
			HdmiIn5 = new RoutingInputPort(
					RoutingPortNames.HdmiIn5,
					eRoutingSignalType.Audio | eRoutingSignalType.Video,
					eRoutingPortConnectionType.Hdmi,
					new Action(() => SelectPresentationSource(5)),
					this
			);
			SdiInput = new RoutingInputPort(
				RoutingPortNames.SdiIn,
				eRoutingSignalType.Video,
				eRoutingPortConnectionType.Sdi,
				new Action(() => SelectPresentationSource(6)),
				this
				);
			HdmiOut1 = new RoutingOutputPort(
				RoutingPortNames.HdmiOut1,
				eRoutingSignalType.Audio | eRoutingSignalType.Video,
				eRoutingPortConnectionType.Hdmi,
				null,
				this
			);
			HdmiOut2 = new RoutingOutputPort(
				RoutingPortNames.HdmiOut2,
				eRoutingSignalType.Audio | eRoutingSignalType.Video,
				eRoutingPortConnectionType.Hdmi,
				null,
				this
			);
			HdmiOut3 = new RoutingOutputPort(
	RoutingPortNames.HdmiOut3,
	eRoutingSignalType.Audio | eRoutingSignalType.Video,
	eRoutingPortConnectionType.Hdmi,
	null,
	this
);

			//InputPorts.Add(CodecOsdIn);
			InputPorts.Add(HdmiIn1);
			InputPorts.Add(HdmiIn2);
			InputPorts.Add(HdmiIn3);
			InputPorts.Add(HdmiIn4);
			InputPorts.Add(HdmiIn5);
			InputPorts.Add(SdiInput);
			OutputPorts.Add(HdmiOut1);
			OutputPorts.Add(HdmiOut2);
			OutputPorts.Add(HdmiOut3);
			//CreateOsdSource();

			ExternalSourceListEnabled = props.ExternalSourceListEnabled;
			ExternalSourceInputPort = props.ExternalSourceInputPort;

			// moved these event subscriptions prior to checking for UI Braning, as they weren't being subscribed to at all.
			AvailableLayoutsChanged += CiscoCodec_AvailableLayoutsChanged;
			CurrentLayoutChanged += CiscoCodec_CurrentLayoutChanged;
			CallStatusChange += CiscoCodec_CallStatusChange;
			CodecInfoChanged += CiscoCodec_CodecInfoChanged;

			if (props.UiBranding == null)
			{
				return;
			}

			_brandingUrl = props.UiBranding.BrandingUrl;
		}

		private void EndGracefully()
		{
			if (BookingsRefreshTimer != null)
			{
				BookingsRefreshTimer.Stop();
				BookingsRefreshTimer.Dispose();
			}
			if (PhonebookRefreshTimer != null)
			{
				PhonebookRefreshTimer.Stop();
				PhonebookRefreshTimer.Dispose();
			}
			if (_loginMessageReceivedTimer != null)
			{
				_loginMessageReceivedTimer.Stop();
				_loginMessageReceivedTimer.Dispose();
			}
			if (_retryConnectionTimer != null)
			{
				_retryConnectionTimer.Stop();
				_retryConnectionTimer.Dispose();
			}
			if (_scheduleCheckTimer != null)
			{
				_scheduleCheckTimer.Stop();
				_scheduleCheckTimer.Dispose();
			}
		}

		private void CiscoCodec_CodecInfoChanged(object sender, CodecInfoChangedEventArgs args)
		{
			if (args.InfoChangeType == eCodecInfoChangeType.Firmware)
			{
				if (string.IsNullOrEmpty(args.Firmware))
					return;
				CodecFirmware = new Version(args.Firmware);
				if (_testedCodecFirmware > CodecFirmware)
				{
					this.LogWarning(
						"Be advised that all functionality may not be available for this plugin. The installed firmware is {installedFirmware} and the minimum tested firmware is {minimumFirmware}",
						CodecFirmware.ToString(),
						_testedCodecFirmware.ToString()
					);
				}
				if (!string.IsNullOrEmpty(CodecFirmware.ToString()))
				{
					DeviceInfo.FirmwareVersion = CodecFirmware.ToString();
					UpdateDeviceInfo();
				}
				SyncState.InitialSoftwareVersionMessageReceived();
			}
			if (args.InfoChangeType == eCodecInfoChangeType.SerialNumber)
			{
				if (!string.IsNullOrEmpty(args.SerialNumber))
				{
					DeviceInfo.SerialNumber = args.SerialNumber;
					UpdateDeviceInfo();
				}
			}
			if (args.InfoChangeType == eCodecInfoChangeType.Network)
			{
				if (!string.IsNullOrEmpty(args.IpAddress))
				{
					DeviceInfo.IpAddress = args.IpAddress;
					UpdateDeviceInfo();
				}
			}
		}

		public void DialZoom()
		{
			if (ZoomMeetingId.NullIfEmpty() == null)
				return;
			var zoomDialCommand = ZoomDialerFirmware ? DialZoomEnhanced() : DialZoomLegacy();
			EnqueueCommand(zoomDialCommand);
		}

		private string DialZoomLegacy()
		{
			var dialOptions = string
				.Format(
					"{0}.{1}.{2}.{3}.{4}.{5}",
					ZoomMeetingId,
					ZoomMeetingPasscode,
					ZoomMeetingCommand,
					ZoomMeetingHostKey,
					ZoomMeetingReservedCode,
					ZoomMeetingDialCode
				)
				.Trim('.');
			var dialAddress = ZoomMeetingIp.NullIfEmpty() ?? "zoomcrc.com";
			var dialString = string.Format("{0}@{1}", dialOptions, dialAddress);

			Dial(dialString);
			return string.Empty;
		}

		public string DialZoomEnhanced()
		{
			var zoomMeetingId =
				ZoomMeetingId.NullIfEmpty() == null
					? string.Empty
					: string.Format("MeetingID: \"{0}\"", ZoomMeetingId);
			var zoomHostKey =
				ZoomMeetingHostKey.NullIfEmpty() == null
					? string.Empty
					: string.Format("HostKey: \"{0}\"", ZoomMeetingHostKey);
			var zoomPasscode =
				ZoomMeetingPasscode.NullIfEmpty() == null
					? string.Empty
					: string.Format("MeetingPasscode: \"{0}\"", ZoomMeetingPasscode);

			var zoomCmd = string
				.Format("xCommand Zoom Join {0} {1} {2}", zoomMeetingId, zoomHostKey, zoomPasscode)
				.Trim();

			return zoomCmd;
		}

		public void DialWebex()
		{
			var webexNumber =
				WebexMeetingNumber.NullIfEmpty() == null
					? string.Empty
					: string.Format("Number: \"{0}\"", WebexMeetingNumber);
			var webexRole =
				WebexMeetingRole.NullIfEmpty() == null
					? string.Empty
					: string.Format("Role: {0}", WebexMeetingRole);
			var webexPin =
				WebexMeetingPin.NullIfEmpty() == null
					? string.Empty
					: string.Format("Pin: \"{0}\"", WebexMeetingPin);

			if (webexNumber == null)
				return;

			var webexCmd = string
				.Format(
					"xCommand Webex Join DisplayName: \"{0}\" {1} {2} {3}",
					this.CodecInfo.SipUri,
					webexNumber,
					webexRole,
					webexPin
				)
				.Trim();

			EnqueueCommand(webexCmd);
		}

		private void ScheduleTimeCheck(object time)
		{
			DateTime currentTime;

			if (time != null)
			{
				var currentTimeString = (time as string);
				if (string.IsNullOrEmpty(currentTimeString))
					return;
				currentTime = DateTime.ParseExact(
					currentTimeString,
					"o",
					CultureInfo.InvariantCulture
				);
			}
			else
				currentTime = DateTime.Now;

			if (_scheduleCheckLast == DateTime.MinValue)
			{
				_scheduleCheckLast = currentTime;
				return;
			}
			if (currentTime.Minute == _scheduleCheckLast.Minute)
				return;
			_scheduleCheckLast = currentTime;
			OnMinuteChanged(currentTime);
		}

		private void OnMinuteChanged(DateTime currentTime)
		{
			var handler = MinuteChanged;
			if (MinuteChanged == null)
				return;
			handler(this, new MinuteChangedEventArgs(currentTime));
		}

		private void CiscoCodec_CallStatusChange(
			object sender,
			CodecCallStatusItemChangeEventArgs e
		)
		{
			var callPresent = ActiveCalls.Any(call => call.IsActiveCall);
			if (!EnhancedLayouts)
			{
				OnAvailableLayoutsChanged(_legacyLayouts);
			}
			if (callPresent)
				return;
			OnAvailableLayoutsChanged(new List<CodecCommandWithLabel>());
			OnCurrentLayoutChanged(string.Empty);
		}

		private string UpdateActiveMeetingXSig(Meeting currentMeeting)
		{
			//const int _meetingsToDisplay = 3;
			const int maxDigitals = 3;
			const int maxStrings = 8;
			const int offset = maxDigitals + maxStrings;
			const int digitalIndex = maxStrings; //15
			const int stringIndex = 0;
			const int meetingIndex = 0;
			var meeting = currentMeeting;

			var tokenArray = new XSigToken[offset];
			/*
             * Digitals
             * IsJoinable - 1
             * IsDialable - 2
             * IsAvailable - 3
             *
             * Serials
             * Organizer - 1
             * Title - 2
             * Start Date - 3
             * Start Time - 4
             * End Date - 5
             * End Time - 6
             * OrganizerId - 7
             * Active "StartTime - EndTime" - 8
            */
			try
			{
				if (meeting != null)
				{
					//digitals
					tokenArray[digitalIndex] = new XSigDigitalToken(
						digitalIndex + 1,
						meeting.Joinable
					);
					tokenArray[digitalIndex + 1] = new XSigDigitalToken(
						digitalIndex + 2,
						meeting.Dialable
					);
					tokenArray[digitalIndex + 2] = new XSigDigitalToken(
						digitalIndex + 3,
						meeting.Joinable && meeting.Dialable
					);

					tokenArray[stringIndex] = new XSigSerialToken(
						stringIndex + 1,
						meeting.Organizer
					);
					tokenArray[stringIndex + 1] = new XSigSerialToken(
						stringIndex + 2,
						meeting.Title
					);
					tokenArray[stringIndex + 2] = new XSigSerialToken(
						stringIndex + 3,
						meeting.StartTime.ToString(_dateFormatSpecifier, Global.Culture)
					);
					tokenArray[stringIndex + 3] = new XSigSerialToken(
						stringIndex + 4,
						meeting.StartTime.ToString(_timeFormatSpecifier, Global.Culture)
					);
					tokenArray[stringIndex + 4] = new XSigSerialToken(
						stringIndex + 5,
						meeting.EndTime.ToString(_dateFormatSpecifier, Global.Culture)
					);
					tokenArray[stringIndex + 5] = new XSigSerialToken(
						stringIndex + 6,
						meeting.EndTime.ToString(_timeFormatSpecifier, Global.Culture)
					);
					tokenArray[stringIndex + 6] = new XSigSerialToken(stringIndex + 7, meeting.Id);
					tokenArray[stringIndex + 7] = new XSigSerialToken(
						stringIndex + 8,
						string.Format(
							"{0} - {1}",
							meeting.StartTime.ToString(_timeFormatSpecifier, Global.Culture),
							meeting.EndTime.ToString(_timeFormatSpecifier, Global.Culture)
						)
					);
				}
				else
				{
					//digitals
					tokenArray[digitalIndex] = new XSigDigitalToken(digitalIndex + 1, false);
					tokenArray[digitalIndex + 1] = new XSigDigitalToken(digitalIndex + 2, false);
					tokenArray[digitalIndex + 2] = new XSigDigitalToken(digitalIndex + 3, false);

					//serials
					tokenArray[stringIndex] = new XSigSerialToken(stringIndex + 1, string.Empty);
					tokenArray[stringIndex + 1] = new XSigSerialToken(
						stringIndex + 2,
						string.Empty
					);
					tokenArray[stringIndex + 2] = new XSigSerialToken(
						stringIndex + 3,
						string.Empty
					);
					tokenArray[stringIndex + 3] = new XSigSerialToken(
						stringIndex + 4,
						string.Empty
					);
					tokenArray[stringIndex + 4] = new XSigSerialToken(
						stringIndex + 5,
						string.Empty
					);
					tokenArray[stringIndex + 5] = new XSigSerialToken(
						stringIndex + 6,
						string.Empty
					);
					tokenArray[stringIndex + 6] = new XSigSerialToken(
						stringIndex + 7,
						string.Empty
					);
					tokenArray[stringIndex + 7] = new XSigSerialToken(
						stringIndex + 8,
						string.Empty
					);
				}

				return GetXSigString(tokenArray);
			}
			catch (Exception e)
			{
				this.LogError("Exception in UpdateActiveMeetingXsig : {message}", e.Message);
				this.LogVerbose(e, "Exception");
				return string.Empty;
			}
		}

		private void SetFeedbackActions()
		{
			CodecStatus.Status.Audio.Volume.ValueChangedAction = VolumeLevelFeedback.FireUpdate;
			CodecStatus.Status.Audio.VolumeMute.ValueChangedAction = MuteFeedback.FireUpdate;

			CodecStatus.Status.Audio.Microphones.Mute.ValueChangedAction =
				PrivacyModeIsOnFeedback.FireUpdate;

			CodecStatus.Status.Standby.State.ValueChangedAction = () =>
			{
				StandbyIsOnFeedback.FireUpdate();
				HalfWakeModeIsOnFeedback.FireUpdate();
				EnteringStandbyModeFeedback.FireUpdate();
			};

			CodecStatus.Status.RoomAnalytics.PeoplePresence.ValueChangedAction =
				RoomIsOccupiedFeedback.FireUpdate;

			CodecStatus.Status.RoomAnalytics.PeopleCount.CurrentPeopleCount.ValueChangedAction =
				PeopleCountFeedback.FireUpdate;

			CodecStatus.Status.Video.Layout.CurrentLayouts.ActiveLayout.ValueChangedAction = () =>
			{
				this.LogVerbose("CurrentLayout = {layout}", CurrentLayout);
				OnCurrentLayoutChanged(
					CodecStatus.Status.Video.Layout.CurrentLayouts.ActiveLayout.Value
				);
			};

			CodecStatus.Status.Video.Selfview.SelfViewMode.ValueChangedAction =
				SelfviewIsOnFeedback.FireUpdate;

			CodecStatus.Status.Video.Selfview.PipPosition.ValueChangedAction =
				ComputeSelfviewPipStatus;

			CodecStatus.Status.Video.Layout.CurrentLayouts.ActiveLayout.ValueChangedAction =
				LocalLayoutFeedback.FireUpdate;

			CodecStatus.Status.Video.Layout.LayoutFamily.Local.ValueChangedAction =
				ComputeLegacyLayout;

			CodecConfiguration.Configuration.Audio.SoundsAndAlerts.RingVolume.ValueChangedAction =
				RingtoneVolumeFeedback.FireUpdate;

			#region CameraTrackingFeedbackRegistration

			CodecStatus.Status.Cameras.SpeakerTrack.SpeakerTrackStatus.ValueChangedAction += () =>
			{
				SpeakerTrackStatusOnFeedback.FireUpdate();
				CameraAutoModeIsOnFeedback.FireUpdate();
			};
			CodecStatus.Status.Cameras.PresenterTrack.PresenterTrackStatus.ValueChangedAction +=
				() =>
				{
					PresenterTrackFeedbackGroup.FireUpdate();
					CameraAutoModeIsOnFeedback.FireUpdate();
				};
			CodecStatus.Status.Cameras.SpeakerTrack.Availability.ValueChangedAction += () =>
			{
				SpeakerTrackAvailableFeedback.FireUpdate();
				CameraAutoModeAvailableFeedback.FireUpdate();
				OnCameraTrackingCapabilitiesChanged();
			};
			CodecStatus.Status.Cameras.PresenterTrack.Availability.ValueChangedAction += () =>
			{
				PresenterTrackAvailability = CodecStatus.Status.Cameras.PresenterTrack.Availability.BoolValue;
				PresenterTrackAvailableFeedback.FireUpdate();
				CameraAutoModeAvailableFeedback.FireUpdate();
				OnCameraTrackingCapabilitiesChanged();
			};

			#endregion

			try
			{
				CodecStatus.Status.Video.VideoInput.MainVideoMute.ValueChangedAction =
					CameraIsOffFeedback.FireUpdate;
			}
			catch (Exception ex)
			{
				this.LogError("Error setting MainVideoMute Action: {message}", ex.Message);
				this.LogVerbose(ex, "Exception");
			}
		}

		public void InitializeBranding(string roomKey)
		{
			this.LogDebug("Initializing Branding for room {0}", roomKey);

			if (!BrandingEnabled)
			{
				return;
			}

			var mcBridgeKey = string.Format("mobileControlBridge-{0}", roomKey);


			var mcBridge =
				DeviceManager.GetDeviceForKey(mcBridgeKey) as IMobileControlRoomMessenger;

			if (!string.IsNullOrEmpty(_brandingUrl))
			{
				this.LogVerbose("Branding URL found: {url}", _brandingUrl);
				if (_brandingTimer != null)
				{
					_brandingTimer.Stop();
					_brandingTimer.Dispose();
				}

				_brandingTimer = new CTimer(
					(o) =>
					{
						if (_sendMcUrl)
						{
							SendMcBrandingUrl(mcBridge);
							_sendMcUrl = false;
						}
						else
						{
							SendBrandingUrl();
							_sendMcUrl = true;
						}
					},
					0,
					15000
				);
			}
			else if (string.IsNullOrEmpty(_brandingUrl))
			{
				this.LogVerbose("No Branding URL found");
				if (mcBridge == null)
					return;

				this.LogVerbose("Setting QR code URL: {url}", mcBridge.QrCodeUrl);

				mcBridge.UserCodeChanged += (o, a) => SendMcBrandingUrl(mcBridge);
				mcBridge.UserPromptedForCode += (o, a) => DisplayUserCode(mcBridge.UserCode);

				SendMcBrandingUrl(mcBridge);
			}
		}

		public void PollSpeakerTrack()
		{
			EnqueueCommand("xStatus Cameras SpeakerTrack");
		}

		public void PollPresenterTrack()
		{
			EnqueueCommand("xStatus Cameras PresenterTrack");
		}

		private void DisplayUserCode(string code)
		{
			EnqueueCommand(
				string.Format(
					"xcommand userinterface message alert display title:\"Mobile Control User Code:\" text:\"{0}\" duration: 30",
					code
				)
			);
		}

#if SERIES4
		private void SendMcBrandingUrl(IMobileControlRoomMessenger roomMessenger)
#else
		private void SendMcBrandingUrl(IMobileControlRoomBridge roomMessenger)
#endif
		{
			if (roomMessenger == null)
			{
				return;
			}

			this.LogVerbose("Sending url: {url}", roomMessenger.QrCodeUrl);

			EnqueueCommand(
				"xconfiguration userinterface custommessage: \"Scan the QR code with a mobile phone to get started\""
			);
			EnqueueCommand(
				"xconfiguration userinterface osd halfwakemessage: \"Tap the touch panel or scan the QR code with a mobile phone to get started\""
			);

			var checksum = !string.IsNullOrEmpty(roomMessenger.QrCodeChecksum)
				? string.Format("checksum: {0} ", roomMessenger.QrCodeChecksum)
				: string.Empty;

			EnqueueCommand(
				string.Format(
					"xcommand userinterface branding fetch {1}type: branding url: {0}",
					roomMessenger.QrCodeUrl,
					checksum
				)
			);
			EnqueueCommand(
				string.Format(
					"xcommand userinterface branding fetch {1}type: halfwakebranding url: {0}",
					roomMessenger.QrCodeUrl,
					checksum
				)
			);
		}

		private void SendBrandingUrl()
		{
			this.LogDebug("Sending url: {brandingUrl}", _brandingUrl);

			EnqueueCommand(
				string.Format(
					"xcommand userinterface branding fetch type: branding url: {0}",
					_brandingUrl
				)
			);
			EnqueueCommand(
				string.Format(
					"xcommand userinterface branding fetch type: halfwakebranding url: {0}",
					_brandingUrl
				)
			);
		}

		public override bool CustomActivate()
		{
			CrestronConsole.AddNewConsoleCommand(
				SetCommDebug,
				"SetCodecCommDebug",
				"0 for Off, 1 for on",
				ConsoleAccessLevelEnum.AccessOperator
			);
			CrestronConsole.AddNewConsoleCommand(
				GetPhonebook,
				"GetCodecPhonebook",
				"Triggers a refresh of the codec phonebook",
				ConsoleAccessLevelEnum.AccessOperator
			);
			CrestronConsole.AddNewConsoleCommand(
				GetBookings,
				"GetCodecBookings",
				"Triggers a refresh of the booking data for today",
				ConsoleAccessLevelEnum.AccessOperator
			);

			PhonebookSyncState.InitialSyncCompleted += PhonebookSyncState_InitialSyncCompleted;
			CameraTrackingCapabilitiesChanged += CiscoCodec_CameraTrackingCapabilitiesChanged;

			//Reserved for future use
			CodecSchedule.MeetingsListHasChanged += (sender, args) => { };
			CodecSchedule.MeetingEventChange += (sender, args) => { };

			var mc = DeviceManager.AllDevices.OfType<IMobileControl>().FirstOrDefault();

			if (mc == null)
			{
				return base.CustomActivate();
			}

			var speakerTrackMessenger = new ISpeakerTrackMessenger($"speakerTrack-{Key}", $"/device/{Key}", this);
			mc.AddDeviceMessenger(speakerTrackMessenger);

			var presenterTrackMessenger = new IPresenterTrackMessenger($"presenterTrack-{Key}", $"/device/{Key}", this);
			mc.AddDeviceMessenger(presenterTrackMessenger);

			return base.CustomActivate();
		}

		private void CiscoCodec_CameraTrackingCapabilitiesChanged(
			object sender,
			CameraTrackingCapabilitiesArgs e
		)
		{
			if (e == null)
				return;
			CameraTrackingCapabilities = e.CameraTrackingCapabilites;
			SupportsCameraAutoMode = CameraTrackingCapabilities != eCameraTrackingCapabilities.None;
		}

		private void CiscoCodec_AvailableLayoutsChanged(
			object sender,
			AvailableLayoutsChangedEventArgs e
		)
		{
			if (e == null)
				return;
			AvailableLayouts = e.AvailableLayouts;
			AvailableLayoutsFeedback.FireUpdate();
		}

		private void CiscoCodec_CurrentLayoutChanged(object sender, CurrentLayoutChangedEventArgs e)
		{
			if (e == null)
				return;
			CurrentLayout = e.CurrentLayout;
			LocalLayoutFeedback.FireUpdate();
		}

		private void PhonebookSyncState_InitialSyncCompleted(object sender, EventArgs e)
		{
			this.LogVerbose("Phonebook Initial Sync Completed");
			if (DirectoryRoot == null)
				return;
			OnDirectoryResultReturned(DirectoryRoot);
		}

		#region Overrides of Device

		public override void Initialize()
		{
			try
			{
				RegisterNetworkEvents();
				RegisterRoomPresetEvents();
				RegisterH323Configuration();
				RegisterAutoAnswer();
				RegisterDisconnectEvents();

				if (Communication is ISocketStatus socket)
				{
					socket.ConnectionChange += Socket_ConnectionChange;

					if (socket is GenericSshClient ssh)
					{
						DeviceInfo.IpAddress = ssh.Hostname;
						DeviceInfo.HostName = ssh.Hostname;
					}


					if (socket is GenericTcpIpClient tcp)
					{
						DeviceInfo.IpAddress = tcp.Hostname;
						DeviceInfo.HostName = tcp.Hostname;
					}
				}

				if (Communication == null)
					throw new NullReferenceException("Coms");

				CommDebuggingIsOn = _config.EnableCommDebugOnStartup;

				Communication.Connect();

				CommunicationMonitor.Start();
			}
			catch (Exception ex)
			{
				this.LogError("Caught an exception in initialize: {message}", ex.Message);
				this.LogVerbose(ex, "Exception");
			}
		}

		#endregion

		private string BuildFeedbackRegistrationExpression()
		{
			const string prefix = "xFeedback register ";

			var feedbackRegistrationExpression =
				prefix
				+ "/Configuration"
				+ Delimiter
				+ prefix
				+ "/Status/Audio"
				+ Delimiter
				+ prefix
				+ "/Status/Audio/Microphones/Mute"
				+ Delimiter
				+ prefix
				+ "/Status/Call"
				+ Delimiter
				+ prefix
				+ "/Status/Conference/Presentation"
				+ Delimiter
				+ prefix
				+ "/Status/Conference/Call/AuthenticationRequest"
				+ Delimiter
				+ prefix
				+ "/Status/Conference/DoNotDisturb"
								+ Delimiter
								+ prefix
								+ "/Status/Cameras/Camera"
								+ Delimiter
				+ prefix
				+ "/Status/Cameras/SpeakerTrack"
				+ Delimiter
				+ prefix
				+ "/Status/Cameras/SpeakerTrack/Status"
				+ Delimiter
				+ prefix
				+ "/Status/Cameras/SpeakerTrack/Availability"
				+ Delimiter
				+ prefix
				+ "/Status/Cameras/PresenterTrack"
				+ Delimiter
				+ prefix
				+ "/Status/Cameras/PresenterTrack/Status"
				+ Delimiter
				+ prefix
				+ "/Status/Cameras/PresenterTrack/Availability"
				+ Delimiter
				+ prefix
				+ "/Status/RoomAnalytics"
				+ Delimiter
				+ prefix
				+ "/Status/RoomPreset"
				+ Delimiter
				+ prefix
				+ "/Status/Standby"
				+ Delimiter
				+ prefix
				+ "/Status/Video/Selfview"
				+ Delimiter
				+ prefix
				+ "/Status/MediaChannels/Call"
				+ Delimiter
				+ prefix
				+ "/Status/Video/Layout/CurrentLayouts"
				+ Delimiter
				+ prefix
				+ "/Status/Video/Layout/LayoutFamily"
				+ Delimiter
				+ prefix
				+ "/Status/Video/Input/MainVideoMute"
				+ Delimiter
				+ prefix
				+ "/Bookings"
				+ Delimiter
				+ prefix
				+ "/Event/Bookings"
				+ Delimiter
				+ prefix
				+ "/Event/CameraPresetListUpdated"
								+ Delimiter
								+ prefix
								+ "/Event/Peripherals"
								+ Delimiter
				+ prefix
				+ "/Event/Conference/Call/AuthenticationResponse"
				+ Delimiter
				+ prefix
				+ "/Event/UserInterface/Presentation/ExternalSource/Selected/SourceIdentifier"
				+ Delimiter
				+ prefix
				+ "Status/UserInterface/WebView/Status"
				+ Delimiter
				+ prefix
				+ "Status/Network/Ethernet/MacAddress"
				+ Delimiter
				+ prefix
				+ "/Event/UserInterface/Extensions/Panel/Clicked"
				+ Delimiter
                + prefix
				+ "/Event/CallDisconnect"
				+ Delimiter;
			// Keep CallDisconnect last to detect when feedback registration completes correctly
			return feedbackRegistrationExpression;
		}

		private void SyncState_InitialSyncCompleted(object sender, EventArgs e)
		{
			this.LogInformation(
				"Initial Sync Complete - There are {activeCallCount} Active Calls",
				ActiveCalls.Count
			);

			if (_config.GetPhonebookOnStartup)
			{
				this.LogInformation("Getting phonebook on startup");
				SearchDirectory("");
			}

			if (ActiveCalls.Count < 1)
			{
				OnCallStatusChange(
					new CodecActiveCallItem()
					{
						Name = string.Empty,
						Number = string.Empty,
						Type = eCodecCallType.Unknown,
						Status = eCodecCallStatus.Unknown,
						Direction = eCodecCallDirection.Unknown,
						Id = string.Empty
					}
				);
			}

			// Check for camera config info 
			if (_config.CameraInfo != null && _config.CameraInfo.Count > 0)
			{
				SetUpCamerasFromConfig(_config.CameraInfo);
			}
			else
			{
				this.LogDebug(
					"No cameraInfo defined in video codec config.  Attempting to get camera info from codec status data"
				);
				try
				{
					var cameraInfo = new List<CameraInfo>();

					this.LogDebug(
						"Codec reports {cameraCount} camera(s)",
						CodecStatus.Status.Cameras.CameraList.Count
					);

					foreach (var camera in CodecStatus.Status.Cameras.CameraList)
					{
						var id = Convert.ToUInt16(camera.CameraId);
						var newCamera = cameraInfo.FirstOrDefault(o => o.CameraNumber == id);
						if (newCamera != null)
							continue;
						var info = new CameraInfo()
						{
							CameraNumber = id,
							Name = string.Format(
								"{0} {1}",
								camera.Manufacturer.Value,
								camera.Model.Value
							),
							SourceId = camera.DetectedConnector.DetectedConnectorId
						};
						cameraInfo.Add(info);
					}

					this.LogDebug(
						"Got cameraInfo for {0} cameras from codec.",
						cameraInfo.Count
					);

					SetUpCameras(cameraInfo);
				}
				catch (Exception ex)
				{
					this.LogError("Error generating camera info from codec status data: {message}", ex.Message);
					this.LogVerbose(ex, "Exception");
				}
			}

			GetCallHistory();

			if (_config.GetPhonebookOnStartup)
			{
				PhonebookRefreshTimer = new CTimer(CheckCurrentHour, 3600000, 3600000);
				// check each hour to see if the phonebook should be downloaded
				GetPhonebook(null);
			}

			if (_config.GetBookingsOnStartup)
			{
				BookingsRefreshTimer = new CTimer(GetBookings, 900000, 900000);
				// 15 minute timer to check for new booking info
				GetBookings(null);
			}

			var msg =
				UiExtensions != null
					? "Initializing Video Codec UI Extensions"
					: "No Ui Extensions in config";

			this.LogDebug(msg);

			if (UiExtensions != null)
			{
				UiExtensions?.Initialize(this, EnqueueCommand);
				UiExtensions?.PanelsHandler?.Initialize(string.Empty);
			}

			// Fire the ready event
			SetIsReady();

			_registrationCheckTimer = new CTimer(EnqueueCommand, "xFeedback list", 90000, 90000);
		}

		public void SetCommDebug(string s)
		{
			if (s == "1")
			{
				CommDebuggingIsOn = true;
				CrestronConsole.ConsoleCommandResponse($"${Key} Codec Comm Debug Enabled.");
			}
			else
			{
				CommDebuggingIsOn = false;
				CrestronConsole.ConsoleCommandResponse($"${Key} Codec Comm Debug Disabled.");
			}
		}

		private void Socket_ConnectionChange(object sender, GenericSocketStatusChageEventArgs e)
		{
			this.LogDebug("Socket status change {clientStatus}", e.Client.ClientStatus);
			if (e.Client.IsConnected)
			{
				if (!SyncState.LoginMessageWasReceived)
					_loginMessageReceivedTimer = new CTimer(
						o => DisconnectClientAndReconnect(),
						5000
					);
			}
			else
			{
				SyncState.CodecDisconnected();
				PhonebookSyncState.CodecDisconnected();
				PhonebookRefreshTimer?.Stop();
				PhonebookRefreshTimer = null;
				BookingsRefreshTimer?.Stop();
				BookingsRefreshTimer = null;
			}
		}

		private void DisconnectClientAndReconnect()
		{
			this.LogDebug("Retrying connection to codec.");

			Communication.Disconnect();

			_retryConnectionTimer = new CTimer(o => Communication.Connect(), 2000);
		}

		private void Port_LineReceived(object dev, GenericCommMethodReceiveTextArgs args)
		{
			var message = new ProcessStringMessage(args.Text, ProcessResponse);
			_receiveQueue.Enqueue(message);
		}

		private void ProcessResponse(string response)
		{
			try
			{
				if (response.ToLower().Contains("xcommand"))
				{
					return;
				}

				if (
					!response.StartsWith("/")
					&& _feedbackListMessage != null
					&& _feedbackListMessageIncoming
				)
				{
					_feedbackListMessageIncoming = false;

					var feedbackListString = _feedbackListMessage.ToString();
					_feedbackListMessage = null;

					ProcessFeedbackList(feedbackListString);
				}

				if (response.StartsWith("/"))
				{
					_feedbackListMessageIncoming = true;
					if (_feedbackListMessage == null)
						_feedbackListMessage = new StringBuilder();
				}

				if (_feedbackListMessageIncoming && _feedbackListMessage != null)
				{
					_feedbackListMessage.Append(response);
					return;
				}

				if (!SyncState.InitialSyncComplete)
				{
					var data = response.Trim().ToLower();
					if (data.Contains("*r login successful") || data.Contains("xstatus systemunit"))
					{
						SyncState.LoginMessageReceived();

						_loginMessageReceivedTimer?.Stop();

						//SendText("echo off");
					}
					else if (data.Contains("xpreferences outputmode json"))
					{
						if (SyncState.JsonResponseModeSet)
							return;

						SyncState.JsonResponseModeMessageReceived();

						if (!SyncState.InitialStatusMessageWasReceived)
						{
							SendText("xStatus Cameras");
							SendText("xStatus SIP");
							SendText("xStatus Call");
							SendText("xStatus");
						}
					}
					else if (data.Contains("xfeedback register /event/calldisconnect"))
					{
						SyncState.FeedbackRegistered();
					}
				}

				if (response == "{" + Delimiter) // Check for the beginning of a new JSON message
				{
					_jsonFeedbackMessageIsIncoming = true;
					_jsonMessage = new StringBuilder();
				}
				else if (response == "}" + Delimiter) // Check for the end of a JSON message
				{
					_jsonFeedbackMessageIsIncoming = false;

					_jsonMessage.Append(response);

					// Enqueue the complete message to be deserialized

					DeserializeResponse(_jsonMessage.ToString());

					return;
				}

				if (!_jsonFeedbackMessageIsIncoming)
					return;
				_jsonMessage.Append(response);
			}
			catch (Exception ex)
			{
				this.LogDebug("Swallowing an exception processing a response:{message}", ex.Message);
			}
		}

		private void ProcessFeedbackList(string data)
		{
			if (
				data.Split('\n').Count() >= BuildFeedbackRegistrationExpression().Split('\n').Count()
			)
				return;

			this.LogWarning("Codec Feedback Registrations Lost - Registering Feedbacks");

			EnqueueCommand(BuildFeedbackRegistrationExpression());
		}

		public void EnqueueCommand(string command)
		{
			SyncState.AddCommandToQueue(command);
		}

		private void EnqueueCommand(object command)
		{
			var cmd = command as string;
			if (string.IsNullOrEmpty(cmd))
				return;
			SyncState.AddCommandToQueue(cmd);
		}

		public void SendText(string command)
		{
			Communication.SendText(command + Delimiter);
		}

		private void UpdateLayoutList()
		{
			var layoutData = new List<CodecCommandWithLabel>();
			if (CodecStatus.Status.Video.Layout.CurrentLayouts.AvailableLayouts != null)
			{
				layoutData.AddRange(
					CodecStatus.Status.Video.Layout.CurrentLayouts.AvailableLayouts.Select(
						r => new CodecCommandWithLabel(r.LayoutName.Value, r.LayoutName.Value)
					)
				);
			}
			AvailableLayouts = layoutData;
			AvailableLayoutsFeedback.FireUpdate();
		}

		private void UpdateLayoutList(CiscoCodecStatus.CurrentLayouts layout)
		{
			var layoutData = new List<CodecCommandWithLabel>();
			if (CodecStatus.Status.Video.Layout.CurrentLayouts.AvailableLayouts != null)
			{
				layoutData.AddRange(
					layout.AvailableLayouts.Select(r => new CodecCommandWithLabel(
						r.LayoutName.Value,
						r.LayoutName.Value
					))
				);
			}
			AvailableLayouts = layoutData;
			AvailableLayoutsFeedback.FireUpdate();
		}

		private void UpdateLayoutList(JToken layout)
		{
			if (layout == null)
				return;

			if (!(layout is JArray layoutArray))
				return;
			var layoutData = (
				from o in layoutArray.Children<JObject>()
				select o.SelectToken("LayoutName.Value").ToString() into name
				where !string.IsNullOrEmpty(name)
				select new CodecCommandWithLabel(name, name)
			).ToList();

			AvailableLayouts = layoutData;
			AvailableLayoutsFeedback.FireUpdate();
		}

		private void UpdateCurrentLayout(JToken layout)
		{
			if (layout == null)
				return;
			CurrentLayout = layout.ToString();
			if (CurrentLayout == string.Empty)
			{
				ClearLayouts();
				return;
			}
			LocalLayoutFeedback.FireUpdate();
		}

		private void ClearLayouts()
		{
			var nullLayout = new CiscoCodecStatus.CurrentLayouts()
			{
				AvailableLayouts = new List<CiscoCodecStatus.LayoutData>(),
				AvailableLayoutsCount = new CiscoCodecStatus.AvailableLayoutsCount() { Value = 0 },
				ActiveLayout = new CiscoCodecStatus.ActiveLayout() { Value = "" }
			};

			CodecStatus.Status.Video.Layout.CurrentLayouts = nullLayout;

			AvailableLayouts = new List<CodecCommandWithLabel>();
			AvailableLayoutsFeedback.FireUpdate();
			CurrentLayout = string.Empty;
			LocalLayoutFeedback.FireUpdate();
			OnAvailableLayoutsChanged(new List<CodecCommandWithLabel>());
			OnCurrentLayoutChanged(string.Empty);
		}

		private void RegisterSystemUnitEvents()
		{
			CodecStatus.Status.SystemUnit.SystemUnitSoftware.Firmware.ValueChangedAction += () =>
				ParseFirmwareObject(
					CodecStatus.Status.SystemUnit.SystemUnitSoftware.Firmware.FirmwareValue
				);

			CodecStatus
				.Status
				.SystemUnit
				.SystemUnitSoftware
				.OptionKeys
				.MultiSite
				.ValueChangedAction += () =>
				OnCodecInfoChanged(
					new CodecInfoChangedEventArgs(eCodecInfoChangeType.Multisite)
					{
						MultiSiteOptionIsEnabled = CodecStatus
							.Status
							.SystemUnit
							.SystemUnitSoftware
							.OptionKeys
							.MultiSite
							.BoolValue
					}
				);

			CodecStatus.Status.SystemUnit.Hardware.Module.SerialNumber.ValueChangedAction += () =>
				ParseSerialNumberObject(CodecStatus.Status.SystemUnit.Hardware.Module.SerialNumber);
		}

		private void ParseSystemUnit(JToken systemUnitToken)
		{
			try
			{
				var jToken = systemUnitToken;
				const string softwareDisplayTokenSelector = "Software.DisplayName.Value";
				const string multisiteSelector = "Software.OptionKeys.Multisite.Value";
				const string serialSelector = "Hardware.Module.SerialNumber.Value";

				var firmwareToken = JTokenValidInToken(jToken, softwareDisplayTokenSelector);
				var multisiteToken = JTokenValidInToken(jToken, multisiteSelector);
				var serialToken = JTokenValidInToken(jToken, serialSelector);
				if (firmwareToken != null)
				{
					ParseFirmwareToken(firmwareToken);
				}
				if (multisiteToken != null)
				{
					ParseMultisiteToken(multisiteToken);
				}
				if (serialToken != null)
				{
					ParseSerialToken(serialToken);
				}
			}
			catch (Exception e)
			{
				this.LogError("Exception in ParseSystemUnit: {message}", e.Message);
				this.LogVerbose(e, "Exception");
			}
		}

		private void ParseSerialToken(JToken serialToken)
		{
			var serial = serialToken.ToString();
			if (string.IsNullOrEmpty(serial))
				return;

			OnCodecInfoChanged(
				new CodecInfoChangedEventArgs(eCodecInfoChangeType.SerialNumber)
				{
					SerialNumber = serial
				}
			);
			if (DeviceInfo == null)
				return;
			DeviceInfo.SerialNumber = serial;
			UpdateDeviceInfo();
		}

		private void ParseMultisiteToken(JToken multisiteToken)
		{
			var multisite = multisiteToken.ToString();
			if (string.IsNullOrEmpty(multisite))
				return;
			OnCodecInfoChanged(
				new CodecInfoChangedEventArgs(eCodecInfoChangeType.Multisite)
				{
					MultiSiteOptionIsEnabled = bool.Parse(multisite)
				}
			);
		}

		private void ParseFirmwareToken(JToken firmwareToken)
		{
			var firmware = firmwareToken.ToString();
			if (string.IsNullOrEmpty(firmware))
				return;

			var parts = firmware.Split(' ');
			if (parts.Length <= 1)
				return;
			CodecFirmware = new Version(parts[1]);

			var codecFirmwareString = CodecFirmware.ToString();

			OnCodecInfoChanged(
				new CodecInfoChangedEventArgs(eCodecInfoChangeType.Firmware)
				{
					Firmware = codecFirmwareString
				}
			);
			if (DeviceInfo == null)
				return;
			DeviceInfo.FirmwareVersion = codecFirmwareString;
			UpdateDeviceInfo();
			SyncState.InitialSoftwareVersionMessageReceived();
		}

		private void RegisterH323Configuration()
		{
			try
			{
				CodecConfiguration.Configuration.H323.H323Alias.E164.ValueChangedAction += () =>
				{
					var e164 =
						CodecConfiguration.Configuration.H323.H323Alias.E164.Value.NullIfEmpty()
						?? "unknown";
					OnCodecInfoChanged(
						new CodecInfoChangedEventArgs(eCodecInfoChangeType.H323)
						{
							E164Alias = e164,
						}
					);
				};

				CodecConfiguration.Configuration.H323.H323Alias.H323AliasId.ValueChangedAction +=
					() =>
					{
						var h323Id =
							CodecConfiguration.Configuration.H323.H323Alias.H323AliasId.Value.NullIfEmpty()
							?? "unknown";
						OnCodecInfoChanged(
							new CodecInfoChangedEventArgs(eCodecInfoChangeType.H323)
							{
								H323Id = h323Id
							}
						);
					};
			}
			catch (Exception e)
			{
				this.LogError("Caught an exception in RegisterH323Configuration: {message}", e.Message);
				this.LogVerbose(e, "Exception");

			}
		}

		private void RegisterDisconnectEvents()
		{
			CodecEvents.Event.CallDisconnect.ValueChangedAction += () =>
			{
				//For Future Use
			};
		}

		private void RegisterUserInterfaceEvents()
		{
			CodecEvents
				.Event
				.UserInterface
				.Presentation
				.ExternalSource
				.Selected
				.SourceIdentifier
				.ValueChangedAction += () =>
				{
					//For Future Use
				};

			// triggered when CodecEvents.Event.UserInterface.Extensions is updated
			/*
             * CodecEvents.Event.UserInterface.Extensions.ValueChangedAction +=
                () =>
                {
                    var act_ = CodecEvents.Event.UserInterface.Extensions.Action;
                    Debug.Console(1, this, "UserInterface.Extensions.ValueChangedAction: /{0} /{1} /{2}", act_.Type, act_.Id, act_.Value);
                };
             * */
		}

		private void RegisterAutoAnswer()
		{
			CodecConfiguration
				.Configuration
				.Conference
				.AutoAnswer
				.AutoAnswerMode
				.ValueChangedAction += () =>
				OnCodecInfoChanged(
					new CodecInfoChangedEventArgs(eCodecInfoChangeType.AutoAnswer)
					{
						AutoAnswerEnabled = CodecConfiguration
							.Configuration
							.Conference
							.AutoAnswer
							.AutoAnswerMode
							.BoolValue
					}
				);
		}

		private void SetPresentationActiveState(bool state)
		{
			_presentationActive = state;
			PresentationActiveFeedback.FireUpdate();
			if (!state)
			{
				SetPresentationSource(0);
			}
		}

		private void SetPresentationSource(int source)
		{
			_presentationSource = (ushort)source;

			PresentationSourceFeedback.FireUpdate();
			ContentInputActiveFeedback.FireUpdate();
			if (_presentationSource == 0)
			{
				ClearLayouts();
				return;
			}
			CodecPollLayouts();
		}

		private void SetPresentationSource(string source)
		{
			_presentationSource = ushort.Parse(source);

			PresentationSourceFeedback.FireUpdate();
			ContentInputActiveFeedback.FireUpdate();
			if (_presentationSource == 0)
			{
				ClearLayouts();
				return;
			}
			CodecPollLayouts();
		}

		private void SetPresentationLocalOnly(bool state)
		{
			_presentationLocalOnly = state;

			PresentationSendingLocalOnlyFeedback.FireUpdate();
			CodecPollLayouts();
		}

		private void SetPresentationLocalRemote(bool state)
		{
			_presentationLocalRemote = state;

			PresentationSendingLocalRemoteFeedback.FireUpdate();
			CodecPollLayouts();
		}

		private void SetPresentationMode(string value)
		{
			var localOnly = value.ToLower() == "localonly";
			var localRemote = value.ToLower() == "localremote";
			var activeState = localOnly || localRemote;

			SetPresentationLocalOnly(localOnly);
			SetPresentationLocalRemote(localRemote);
			SetPresentationActiveState(activeState);
		}

		private void ParseSerialNumberObject(CiscoCodecStatus.SerialNumber serialNumber)
		{
			OnCodecInfoChanged(
				new CodecInfoChangedEventArgs(eCodecInfoChangeType.SerialNumber)
				{
					SerialNumber = serialNumber.Value
				}
			);
			if (DeviceInfo == null)
				return;
			DeviceInfo.SerialNumber = serialNumber.Value;
			UpdateDeviceInfo();
		}

		private void ParseFirmwareObject(Version firmware)
		{
			CodecFirmware = firmware;
			var codecFirmwareString = CodecFirmware.ToString();

			OnCodecInfoChanged(
				new CodecInfoChangedEventArgs(eCodecInfoChangeType.Firmware)
				{
					Firmware = codecFirmwareString
				}
			);
			if (DeviceInfo == null)
				return;
			DeviceInfo.FirmwareVersion = codecFirmwareString;
			UpdateDeviceInfo();
			SyncState.InitialSoftwareVersionMessageReceived();
		}

		private void RegisterSipEvents()
		{
			CodecStatus.Status.Sip.RegistrationCount.ValueChangedAction += () =>
			{
				if (CodecStatus.Status.Sip.Registrations.Count <= 0)
					return;
				ParseSipObject(CodecStatus.Status.Sip);
			};
		}

		private void RegisterNetworkEvents()
		{
			CodecStatus.Status.NetworkCount.ValueChangedAction += () =>
			{
				this.LogVerbose("CodecStatus.Status.NetworkCount.Value = {value}", CodecStatus.Status.NetworkCount.Value);
				if (CodecStatus.Status.NetworkCount.Value <= 0)
					return;
				ParseNetworkList(CodecStatus.Status.Networks);
			};
		}

		private void RegisterRoomPresetEvents()
		{
			CodecStatus.Status.RoomPresetsChange.ValueChangedAction += () =>
			{
				if (CodecStatus.Status.RoomPresets == null)
				{
					NearEndPresets = (
						new List<CiscoCodecStatus.RoomPreset>().GetGenericPresets<
							CiscoCodecStatus.RoomPreset,
							CodecRoomPreset
						>()
					);
				}
				else
				{
					NearEndPresets = CodecStatus.Status.RoomPresets.GetGenericPresets<
						CiscoCodecStatus.RoomPreset,
						CodecRoomPreset
					>();
				}
				CodecRoomPresetsListHasChanged?.Invoke(this, new EventArgs());
			};
		}

		private void ParseNetworkList(IEnumerable<CiscoCodecStatus.Network> networks)
		{
			var myNetwork = networks.FirstOrDefault(i => i.NetworkId == "1");
			if (myNetwork == null)
				return;
			var hostname = myNetwork.Cdp.DeviceId.Value.NullIfEmpty() ?? "Unknown";
			var ipAddress = myNetwork.IPv4.Address.Value.NullIfEmpty() ?? "Unknown";
			var macAddress = myNetwork.Ethernet.MacAddress.Value.NullIfEmpty() ?? "Unknown";

			OnCodecInfoChanged(
				new CodecInfoChangedEventArgs(eCodecInfoChangeType.Network)
				{
					IpAddress = ipAddress
				}
			);

			DeviceInfo.HostName = hostname;
			DeviceInfo.IpAddress = ipAddress;
			DeviceInfo.MacAddress = macAddress;
			UpdateDeviceInfo();
		}

		public void ParseSipToken(JToken sipToken)
		{
			try
			{
				if (string.IsNullOrEmpty(sipToken.ToString()))
					return;
				var registrationArrayToken = sipToken.SelectToken("Registration");
				if (!(registrationArrayToken is JArray registrationArray))
					return;

				var sipPhoneNumber = "Unknown";
				var sipUri = "Unknown";

				var registrationItem = registrationArray
					.Children<JObject>()
					.FirstOrDefault(o => o.SelectToken("id").ToString() == "1");

				if (registrationItem != null)
				{
					sipUri =
						registrationItem.SelectToken("URI.Value").ToString().NullIfEmpty()
						?? "Unknown";
					var match = Regex.Match(sipUri, @"(\d+)");
					sipPhoneNumber = match.Success ? match.Groups[1].Value : "Unknown";
				}

				OnCodecInfoChanged(
					new CodecInfoChangedEventArgs(eCodecInfoChangeType.Sip)
					{
						SipPhoneNumber = sipPhoneNumber,
						SipUri = sipUri
					}
				);
			}
			catch (Exception e)
			{
				this.LogError("Exception in ParseSipToken: {message}", e.Message);
				this.LogVerbose(e, "Exception");
			}
		}

		private void UpdateCameraAutoModeFeedbacks()
		{
			CameraAutoModeIsOnFeedback.FireUpdate();
			SpeakerTrackStatusOnFeedback.FireUpdate();
			PresenterTrackStatusNameFeedback.FireUpdate();
			PresenterTrackStatusOffFeedback.FireUpdate();
			PresenterTrackStatusFollowFeedback.FireUpdate();
			PresenterTrackStatusBackgroundFeedback.FireUpdate();
			PresenterTrackStatusPersistentFeedback.FireUpdate();
			CameraAutoModeAvailableFeedback.FireUpdate();
			PresenterTrackAvailableFeedback.FireUpdate();
			SpeakerTrackAvailableFeedback.FireUpdate();
		}

		private void ParseWebviewStatusToken(JToken webviewStatusToken)
		{
			try
			{
				if (string.IsNullOrEmpty(webviewStatusToken.ToString()))
					return;
				if (!(webviewStatusToken is JObject webviewStatusObject))
					return;
				var statusToken = webviewStatusObject.SelectToken("Status.Value");
				if (statusToken != null)
				{
					var status = statusToken.ToString().ToLower();
					if (!string.IsNullOrEmpty(status))
					{
						WebViewStatusChanged?.Invoke(this, new WebViewStatusChangedEventArgs(status));
						if (status == "visible")
						{
							WebviewIsVisible = true;
						}
						else if (status == "notvisible")
						{
							WebviewIsVisible = false;
						}
					}
				}
			}
			catch (Exception e)
			{
				this.LogError("Exception in ParseWebviewStatusToken: {message}", e.Message);
				this.LogVerbose(e, "Exception");
			}
		}
		private void ParseSpeakerTrackToken(JToken speakerTrackToken)
		{
			try
			{
				if (string.IsNullOrEmpty(speakerTrackToken.ToString()))
					return;
				if (!(speakerTrackToken is JObject speakerTrackObject))
					return;
				var availabilityToken = speakerTrackObject.SelectToken("Availability.Value");
				var statusToken = speakerTrackObject.SelectToken("Status.Value");
				if (availabilityToken != null)
					SpeakerTrackAvailability =
						availabilityToken.ToString().ToLower() == "available";
				if (statusToken != null)
					SpeakerTrackStatus = statusToken.ToString().ToLower() == "active";

				UpdateCameraAutoModeFeedbacks();
			}
			catch (Exception e)
			{
				this.LogError("Exception in ParseSpeakerTrackToken: {message}", e.Message);
				this.LogVerbose(e, "Exception");
			}
		}

		private void ParsePresenterTrackToken(JToken presenterTrackToken)
		{
			try
			{
				if (string.IsNullOrEmpty(presenterTrackToken.ToString()))
					return;
				if (!(presenterTrackToken is JObject presenterTrackObject))
					return;
				var availabilityToken = presenterTrackObject.SelectToken("Availability.Value");
				var statusToken = presenterTrackObject.SelectToken("Status.Value");
				if (availabilityToken != null)
					PresenterTrackAvailability =
						availabilityToken.ToString().ToLower() == "available";
				if (statusToken != null)
				{
					var status = statusToken.ToString().ToLower();
					if (!string.IsNullOrEmpty(status))
					{
						PresenterTrackStatusName = status;
						switch (status)
						{
							case ("follow"):
								PresenterTrackStatus = true;
								break;
							case ("background"):
								PresenterTrackStatus = true;
								break;
							case ("persistent"):
								PresenterTrackStatus = true;
								break;
							default:
								PresenterTrackStatus = false;
								break;
						}
					}
				}
				UpdateCameraAutoModeFeedbacks();
			}
			catch (Exception e)
			{
				this.LogError("Exception in ParsePresenterTrackToken: {message}", e.Message);
				this.LogVerbose(e, "Exception");
			}
		}

		private void ParseNetworkToken(JToken networkToken)
		{
			try
			{
				if (string.IsNullOrEmpty(networkToken.ToString()))
					return;
				if (!(networkToken is JArray networkArray))
					return;
				foreach (var n in networkArray.Children<JObject>())
				{
					if (n.SelectToken("id").ToString() != "1")
						continue;
					var hostname =
						n.SelectToken("Cdp.DeviceId.Value")?.ToString().NullIfEmpty() ?? "Unknown";
					var ipAddress =
						n.SelectToken("IPv4.Address.Value")?.ToString().NullIfEmpty() ?? "Unknown";
					var macAddress =
						n.SelectToken("Ethernet.MacAddress.Value")?.ToString().NullIfEmpty()
						?? "Unknown";
					OnCodecInfoChanged(
						new CodecInfoChangedEventArgs(eCodecInfoChangeType.Network)
						{
							IpAddress = ipAddress
						}
					);

					if (DeviceInfo == null)
						return;
					DeviceInfo.HostName = hostname;
					DeviceInfo.IpAddress = ipAddress;
					DeviceInfo.MacAddress = macAddress;
					UpdateDeviceInfo();
					return;
				}
			}
			catch (Exception e)
			{
				this.LogError("Exception in ParseNetworkToken: {message}", e.Message);
				this.LogVerbose(e, "Exception");
			}
		}

		private void ParseSipObject(CiscoCodecStatus.Sip sipObject)
		{
			if (sipObject.Registrations.Count <= 0)
				return;
			var sipUri = sipObject.Registrations.First().Uri.Value.NullIfEmpty() ?? "Unknown";
			var match = Regex.Match(sipUri, @"(\d+)");
			var sipPhoneNumber = match.Success ? match.Groups[1].Value : "Unknown";
			OnCodecInfoChanged(
				new CodecInfoChangedEventArgs(eCodecInfoChangeType.Sip)
				{
					SipPhoneNumber = sipPhoneNumber,
					SipUri = sipUri
				}
			);
		}

		private void ParseLayoutObject(CiscoCodecStatus.CurrentLayouts layoutObject)
		{
			if (layoutObject != null)
			{
				if (layoutObject.AvailableLayouts != null)
				{
					UpdateLayoutList(layoutObject);
				}
				if (layoutObject.ActiveLayout == null)
					return;
			}
		}

		private void ParseLayoutToken(JToken layoutToken)
		{
			//if ((_presentationLocalOnly || _presentationSource == 0) && (_incomingPresentation == IncomingPresentationStatus.False)) return;
			if (!_IsInPresentation)
			{
				ClearLayouts();
				return;
			}

			if (string.IsNullOrEmpty(layoutToken.ToString()))
				return;
			UpdateCurrentLayout(layoutToken.SelectToken("ActiveLayout.Value"));
			UpdateLayoutList(layoutToken.SelectToken("AvailableLayouts"));
		}

		private void ParseCallArrayToken(JToken callToken)
		{
			try
			{

				if (!(callToken is JArray callArray))
					return;
				foreach (var item in callArray.Cast<JObject>().Where(item => item != null))
				{
					var callIdToken = CheckJTokenInObject(item, "id");
					if (callIdToken == null)
						continue;
					CodecActiveCallItem callObject = null;

					var callId = callIdToken.ToString();

					var callGhostToken = CheckJTokenInObject(item, "ghost");
					var callGhost = callGhostToken != null && bool.Parse(callGhostToken.ToString());

					if (!callGhost)
					{
						callObject = ParseCallObject(item);
						if (callObject == null)
							continue;
					}

					var activeCall = ActiveCalls.FirstOrDefault(o => o.Id == callId);

					if (activeCall != null)
					{
						if (callGhost)
							ActiveCalls.Remove(activeCall);
						if (callObject != null)
							if (!MergeCallData(activeCall, callObject))
								continue;
						PrintCallItem(activeCall);

						SetSelfViewMode();

						this.LogDebug(
							"On Call ID {id} Status Change - Status == {status}",
							activeCall.Id,
							activeCall.Status
						);

						OnCallStatusChange(activeCall);
						ListCalls();
						CodecPollLayouts();

						continue;
					}

					if (callGhost)
						continue;
					ActiveCalls.Add(callObject);

					SetSelfViewMode();

					ListCalls();

					this.LogDebug(
							"On Call ID {id} Status Change - Status == {status}",
							activeCall.Id,
							activeCall.Status
						);

					OnCallStatusChange(callObject);

					CodecPollLayouts();
				}
			}
			catch (Exception ex)
			{
				this.LogError("Exception in ParseCallArrayToken: {message}", ex.Message);
				this.LogVerbose(ex, "Exception");
			}
		}

		private void ParseMediaChannelsTokenArray(JToken mediaChannelsTokenArray)
		{
			try
			{
				if (!(mediaChannelsTokenArray is JArray channelArray))
					return;
				var channelStatus = MediaChannelStatus.Unknown;
				foreach (var item in channelArray.Cast<JObject>().Where(item => item != null))
				{
					JToken callIdToken;
					JToken channelToken;

					var callId = item.TryGetValue("id", out callIdToken)
						? callIdToken.ToString()
						: string.Empty;
					if (string.IsNullOrEmpty(callId))
						continue;
					var activeCall = ActiveCalls.FirstOrDefault(o =>
						o.Id.Equals(callId, StringComparison.OrdinalIgnoreCase)
					);
					if (activeCall == null)
						continue;

					if (!item.TryGetValue("Channel", out channelToken))
						continue;
					channelStatus =
						channelStatus | ParseMediaChannelsToken(channelToken, activeCall);
					_incomingPresentation = channelStatus;
					ListCalls();
					if (channelStatus == MediaChannelStatus.Video)
					{
						activeCall.Type = eCodecCallType.Video;
						SetSelfViewMode();
						CodecPollLayouts();
						OnCallStatusChange(activeCall);
					}
					PrintCallItem(activeCall);
				}
			}
			catch (Exception ex)
			{
				this.LogError("Exception in ParseMediaChannelsTokenArray: {message}", ex.Message);
				this.LogVerbose(ex, "Exception");
			}
		}

		private MediaChannelStatus ParseMediaChannelsToken(
			JToken mediaChannelsToken,
			CodecActiveCallItem call
		)
		{
			try
			{

				var channelStatus = MediaChannelStatus.Unknown;
				var channelToken = mediaChannelsToken;
				if (!(channelToken is JArray channelArray))
				{

					return channelStatus;
				}
				foreach (var jToken in channelArray)
				{
					if (!(jToken is JObject item))
					{
						return channelStatus;
					}

					var channelDirectionToken = CheckJTokenInObject(item, "Direction.Value");
					var channelVideoToken = CheckJTokenInObject(item, "Video");
					var channelAudioToken = CheckJTokenInObject(item, "Audio");

					if (channelVideoToken == null && channelAudioToken == null)
					{
						return _incomingPresentation;
					}

					var channelDirection =
						channelDirectionToken != null
							? channelDirectionToken.ToString()
							: "Unknown";

					channelStatus = channelDirection.Equals(
						"incoming",
						StringComparison.OrdinalIgnoreCase
					)
						? channelStatus | MediaChannelStatus.Incoming
						: channelStatus;
					channelStatus = channelDirection.Equals(
						"outgoing",
						StringComparison.OrdinalIgnoreCase
					)
						? channelStatus | MediaChannelStatus.Outgoing
						: channelStatus;

					channelStatus =
						channelVideoToken != null
							? channelStatus
								| ParseMediaChannelToken(
									channelVideoToken,
									MediaChannelStatus.Video
								)
							: channelStatus;
					channelStatus =
						channelAudioToken != null
							? channelStatus
								| ParseMediaChannelToken(
									channelAudioToken,
									MediaChannelStatus.Audio
								)
							: channelStatus;
				}

				return channelStatus;
			}
			catch (Exception ex)
			{
				this.LogError("Exception in ParseMediaChannelsToken: {message}", ex.Message);
				this.LogVerbose(ex, "Exception");
				return MediaChannelStatus.Unknown;
			}
		}

		private MediaChannelStatus ParseMediaChannelToken(
			JToken mediaChannelsToken,
			MediaChannelStatus identifier
		)
		{
			try
			{
				var channelStatus = MediaChannelStatus.Unknown;
				var channelToken = mediaChannelsToken;
				if (channelToken == null)
					return channelStatus;

				if (!(channelToken is JObject item))
				{
					return channelStatus;
				}
				//JToken channelRoleToken;

				var protocolPresent = false;

				var channelRoleToken = CheckJTokenInObject(item, "ChannelRole.Value");

				var channelRole =
					channelRoleToken != null ? channelRoleToken.ToString() : string.Empty;

				var protocolToken = CheckJTokenInObject(item, "Protocol.Value");
				if (protocolToken != null)
				{
					protocolPresent = !protocolToken
						.ToString()
						.Equals("off", StringComparison.OrdinalIgnoreCase);
				}

				channelStatus = channelRole.Equals(
					"presentation",
					StringComparison.OrdinalIgnoreCase
				)
					? channelStatus | MediaChannelStatus.Presentation
					: channelStatus;
				channelStatus = channelRole.Equals("main", StringComparison.OrdinalIgnoreCase)
					? channelStatus | MediaChannelStatus.Main
					: channelStatus;
				channelStatus = protocolPresent ? channelStatus | identifier : channelStatus;
				return channelStatus;
			}
			catch (Exception ex)
			{
				this.LogError("Exception in ParseMediaChannelToken: {message}", ex.Message);
				this.LogVerbose(ex, "Exception");
				return MediaChannelStatus.Unknown;
			}
		}

		private MediaChannelStatus CheckIncomingPresentation(
			string id,
			IEnumerable<CiscoCodecStatus.MediaChannelCall> calls
		)
		{
			var mediaChannelStatus = MediaChannelStatus.Unknown;
			var currentCall = calls.FirstOrDefault(p => p.MediaChannelCallId == id);
			if (currentCall == null)
			{
				return mediaChannelStatus | MediaChannelStatus.None;
			}

			var incomingChannels = currentCall.Channels.Where(x =>
				x.Direction.Value.ToLower() == "incoming"
			);
			if (incomingChannels.Any())
				mediaChannelStatus = mediaChannelStatus | MediaChannelStatus.Incoming;
			var outgoingChannels = currentCall.Channels.Where(x =>
				x.Direction.Value.ToLower() == "outgoing"
			);
			if (outgoingChannels.Any())
				mediaChannelStatus = mediaChannelStatus | MediaChannelStatus.Outgoing;

			mediaChannelStatus = currentCall.Channels.Any(x =>
				x.ChannelVideo.ChannelRole.Value.ToLower() == "presentation"
			)
				? mediaChannelStatus | MediaChannelStatus.Presentation
				: mediaChannelStatus;
			mediaChannelStatus = currentCall.Channels.Any(x =>
				x.ChannelVideo.ChannelRole.Value.ToLower() == "main"
			)
				? mediaChannelStatus | MediaChannelStatus.Main
				: mediaChannelStatus;
			mediaChannelStatus = currentCall.Channels.Any(x =>
				!string.IsNullOrEmpty(x.ChannelVideo.Protocol.Value)
			)
				? mediaChannelStatus | MediaChannelStatus.Video
				: mediaChannelStatus;
			mediaChannelStatus = currentCall.Channels.Any(x =>
				!string.IsNullOrEmpty(x.ChannelAudio.Protocol.Value)
			)
				? mediaChannelStatus | MediaChannelStatus.Audio
				: mediaChannelStatus;

			return mediaChannelStatus;
		}

		private CodecActiveCallItem ParseCallObject(JObject call)
		{
			try
			{
				if (call == null)
					return null;

				var callIdToken = CheckJTokenInObject(call, "id");
				var callId = callIdToken != null ? callIdToken.ToString() : string.Empty;
				if (string.IsNullOrEmpty(callId))
					return null;
				this.LogDebug("Found an ID! : {id}", callId);

				var callStatusToken = CheckJTokenInObject(call, "Status.Value");
				var callStatus =
					(callStatusToken != null) ? callStatusToken.ToString() : string.Empty;

				var callTypeToken = CheckJTokenInObject(call, "CallType.Value");
				var callType =
					callTypeToken != null
						? callTypeToken
							.ToString()
							.Equals("video", StringComparison.OrdinalIgnoreCase)
							? "audio"
							: callTypeToken.ToString()
						: string.Empty;

				var callDirectionToken = CheckJTokenInObject(call, "Direction.Value");
				var callDirection =
					callDirectionToken != null ? callDirectionToken.ToString() : string.Empty;

				var callRemoteNumberToken = CheckJTokenInObject(call, "RemoteNumber.Value");
				var callRemoteNumber =
					callRemoteNumberToken != null ? callRemoteNumberToken.ToString() : string.Empty;

				var callDisplayNameToken = CheckJTokenInObject(call, "DisplayName.Value");
				var callDisplayName =
					callDisplayNameToken != null ? callDisplayNameToken.ToString() : string.Empty;

				var callDurationToken = CheckJTokenInObject(call, "Duration.Value");
				var callDuration =
					callDurationToken != null
						? new TimeSpan(0, 0, int.Parse(callDurationToken.ToString()))
						: new TimeSpan(0, 0, int.MaxValue);

				var callPlacedOnHoldToken = CheckJTokenInObject(call, "PlacedOnHold.Value");
				var callPlacedOnHold =
					callPlacedOnHoldToken != null
					&& callPlacedOnHoldToken
						.ToString()
						.Equals("true", StringComparison.OrdinalIgnoreCase);

				var callStatusEnum = ConvertToStatusEnum(callStatus);

				var callTypeEnum = ConvertToTypeEnum(callType);
				var callDirectionEnum = ConvertToDirectionEnum(callDirection);

				if (callStatusEnum == eCodecCallStatus.OnHold)
				{
					callPlacedOnHold = true;
				}

				var newCallItem = new CodecActiveCallItem()
				{
					Id = callId,
					Status = callStatusEnum,
					Name = callDisplayName,
					Number = callRemoteNumber,
					Type = callTypeEnum,
					Direction = callDirectionEnum,
					Duration = callDuration,
					IsOnHold = callPlacedOnHold
				};

				return newCallItem;
			}
			catch (Exception ex)
			{
				this.LogError("Exception in ParseCallObject : {message}", ex.Message);
				this.LogVerbose(ex, "Exception");
				return null;
			}
		}

		public void PrintCallItem(CodecActiveCallItem callData)
		{
			var newLine = CrestronEnvironment.NewLine;
			var sb = new StringBuilder(
				string.Format("New Call Item : ID = {1}{0}", newLine, callData.Id)
			);
			sb.AppendFormat("Status : {0}{1}", callData.Status, newLine);
			sb.AppendFormat("Name : {0}{1}", callData.Name, newLine);
			sb.AppendFormat("Number : {0}{1}", callData.Number, newLine);
			sb.AppendFormat("Type : {0}{1}", callData.Type, newLine);
			sb.AppendFormat("Direction : {0}{1}", callData.Direction, newLine);
			sb.AppendFormat("Duraion : {0}{1}", callData.Duration, newLine);
			sb.AppendFormat("IsOnHold : {0}{1}", callData.IsOnHold, newLine);
			this.LogDebug(sb.ToString());
		}

		public eCodecCallType ConvertToTypeEnum(string s)
		{
			try
			{
				if (string.IsNullOrEmpty(s))
					return eCodecCallType.Unknown;
				return (eCodecCallType)Enum.Parse(typeof(eCodecCallType), s, true);
			}
			catch (Exception ex)
			{
				this.LogError("Unable to parse enum: {message}", ex.Message);

				return eCodecCallType.Unknown;
			}
		}

		public eCodecCallDirection ConvertToDirectionEnum(string s)
		{
			try
			{
				if (string.IsNullOrEmpty(s))
					return eCodecCallDirection.Unknown;
				return (eCodecCallDirection)Enum.Parse(typeof(eCodecCallDirection), s, true);
			}
			catch (Exception ex)
			{
				this.LogError("Unable to parse enum: {message}", ex.Message);
				return eCodecCallDirection.Unknown;
			}
		}

		public eCodecCallStatus ConvertToStatusEnum(string s)
		{
			var stringToProcess = s.Replace("Dialling", "Dialing");

			try
			{
				if (string.IsNullOrEmpty(stringToProcess))
					return eCodecCallStatus.Unknown;
				return (eCodecCallStatus)
					Enum.Parse(typeof(eCodecCallStatus), stringToProcess, true);
			}
			catch (Exception ex)
			{
				this.LogError("Unable to parse {stringToParse}: {message}", stringToProcess, ex.Message);
				this.LogVerbose("Exception", ex);
				return eCodecCallStatus.Unknown;
			}
		}

		public bool MergeCallData(
			CodecActiveCallItem existingCallData,
			CodecActiveCallItem newCallData
		)
		{

			PrintCallItem(existingCallData);
			PrintCallItem(newCallData);
			bool valueChanged = false;

			if (
				existingCallData.Direction != newCallData.Direction
				&& newCallData.Direction != eCodecCallDirection.Unknown
			)
			{
				existingCallData.Direction = newCallData.Direction;
				valueChanged = true;
			}

			if (
				existingCallData.Duration != newCallData.Duration
				&& newCallData.Duration.Seconds != int.MaxValue
			)
			{
				existingCallData.Duration = newCallData.Duration;
				valueChanged = true;
			}

			if (
				!existingCallData.Name.Equals(newCallData.Name, StringComparison.OrdinalIgnoreCase)
				&& !string.IsNullOrEmpty(newCallData.Name)
			)
			{
				existingCallData.Name = newCallData.Name;
				valueChanged = true;
			}

			if (
				!existingCallData.Number.Equals(
					newCallData.Number,
					StringComparison.OrdinalIgnoreCase
				) && !string.IsNullOrEmpty(newCallData.Number)
			)
			{
				existingCallData.Number = newCallData.Number;
				valueChanged = true;
			}

			if (
				existingCallData.Status != newCallData.Status
				&& newCallData.Status != eCodecCallStatus.Unknown
			)
			{
				existingCallData.Status = newCallData.Status;
				existingCallData.IsOnHold = newCallData.IsOnHold;
				valueChanged = true;
			}
			if (
				existingCallData.Type != newCallData.Type
				&& newCallData.Type != eCodecCallType.Unknown
			)
			{
				existingCallData.Type = newCallData.Type;
				valueChanged = true;
			}
			return valueChanged;
		}

		private void ParseRoomPresetList(List<CiscoCodecStatus.RoomPreset> presetList)
		{
			if (presetList.Count == 0)
				return;
			var extantPresets = CodecStatus.Status.RoomPresets;
			if (extantPresets == null || extantPresets.Count == 0)
			{
				CodecStatus.Status.RoomPresets = presetList;
				return;
			}

			var newItems = presetList.Except(CodecStatus.Status.RoomPresets).ToList();
			var updatedItems = CodecStatus
				.Status.RoomPresets.Where(c =>
					presetList.Any(d => c.RoomPresetId == d.RoomPresetId)
				)
				.ToList();
			CodecStatus.Status.RoomPresets = newItems.Concat(updatedItems).ToList();
		}

		private void ParseUserInterfaceEvent(CiscoCodecEvents.UserInterface userInterfaceObject)
		{
			if (userInterfaceObject == null)
				return;

			if (userInterfaceObject.Presentation != null)
			{
				//var _userInterfaceObject = userInterfaceObject.SelectToken("Presentation.ExternalSource.Selected.SourceIdentifier");

				var val_ = JsonConvert.SerializeObject(userInterfaceObject);
				//Debug.Console(1, this, "userInterfaceObject val: {0}", val_);

				if (RunRouteAction != null && !_externalSourceChangeRequested)
				{
					RunRouteAction(
						userInterfaceObject
							.Presentation
							.ExternalSource
							.Selected
							.SourceIdentifier
							.Value,
						null
					);
				}

				_externalSourceChangeRequested = false;
			}

			if (userInterfaceObject.Extensions != null)
			{
				//Debug.Console(2, this, "Extensions Event");
				try
				{
					var val_ = JsonConvert.SerializeObject(userInterfaceObject.Extensions);

					this.LogVerbose(
						"*** Got an Extensions Event {0}",
						userInterfaceObject.Extensions
					);

					if (
						userInterfaceObject.Extensions.Widget != null
						&& userInterfaceObject.Extensions.Widget.WidgetAction != null
						&& userInterfaceObject.Extensions.Widget.WidgetAction.Type != null
					)
					{
						this.LogVerbose(
							"*** Got an Extensions Widget Action {0}",
							userInterfaceObject.Extensions.Widget
						);
						val_ = JsonConvert.SerializeObject(userInterfaceObject.Extensions.Widget);
						//Debug.Console(1, this, "Widget val: {0}", val_);
						UIExtensionsHandler.ParseStatus(userInterfaceObject.Extensions.Widget);
					}

					if (
						userInterfaceObject.Extensions.WidgetEvent != null
						&& userInterfaceObject.Extensions.WidgetEvent.Id != null
					)
					{
						this.LogVerbose(
							"*** Got an Extensions Widget Event {0}",
							userInterfaceObject.Extensions.WidgetEvent
						);

						val_ = JsonConvert.SerializeObject(
							userInterfaceObject.Extensions.WidgetEvent
						);

						UIExtensionsHandler.ParseStatus(userInterfaceObject.Extensions.WidgetEvent);
					}

					if (
						userInterfaceObject.Extensions != null
						&& userInterfaceObject.Extensions.Panel != null
					)
					{
						var val = userInterfaceObject.Extensions?.Panel?.Clicked?.PanelId?.Value;
						var msg =
							val == null
								? "*** Got a Null Extensions Panel Event"
								: $"*** Got an Extensions Panel Event {val}";
						this.LogVerbose(msg);

						if (val == null)
							return;

						UiExtensions?.PanelsHandler?.ParseStatus(
							userInterfaceObject.Extensions.Panel
						);

						UiExtensionsHandler?.ParseStatus(
								userInterfaceObject.Extensions.Panel
							);
					}
				}
				catch (Exception e)
				{
					this.LogError(
						"Exception: ParseUserInterfaceEvent.Extensions - {message}",
						e.Message
					);
					this.LogVerbose(e, "Exception");
				}
			}
		}

		private void PopulateObjectWithToken(JToken jToken, string tokenSelector, object target)
		{
			var tokenString = string.Empty;
			try
			{
				//Debug.Console(2, this, "PopulateObjectWithToken: {0}", tokenSelector);
				var token = JTokenValidInToken(jToken, tokenSelector); // JObject
				if (token == null)
					return;
				tokenString = token.ToString();
				JsonConvert.PopulateObject(tokenString, target);
				//Debug.Console(2, this, "PopulateObject complete");
			}
			catch (Exception e)
			{
				this.LogError("Exception: PopulateObjectWithToken - {message}", e.Message);
				this.LogVerbose(e, "Exception");
			}
		}

		private void ParseStatusObject(JToken statusToken)
		{
			if (statusToken == null || (statusToken.Type == JTokenType.Object && !statusToken.HasValues))
			{
				return;
			}

			var status = new CiscoCodecStatus.Status();
			var legacyLayoutsToken = statusToken.SelectToken("Video.Layout.LayoutFamily");
			var layoutsToken = statusToken.SelectToken("Video.Layout.CurrentLayouts");
			var selfviewToken = statusToken.SelectToken("Video.Selfview.Mode");
			var mediaChannelsToken = statusToken.SelectToken("MediaChannels.Call");
			var systemUnitToken = statusToken.SelectToken("SystemUnit");
			var cameraToken = statusToken.SelectToken("Cameras.Camera");
			var speakerTrackToken = statusToken.SelectToken("Cameras.SpeakerTrack");
			var presenterTrackToken = statusToken.SelectToken("Cameras.PresenterTrack");
			var networkToken = statusToken.SelectToken("Network");
			var sipToken = statusToken.SelectToken("SIP");
			var conferenceToken = statusToken.SelectToken("Conference");
			var webViewStatusToken = statusToken.SelectToken("UserInterface.WebView");
			var callToken = statusToken.SelectToken("Call");
			var errorToken = JTokenValidInToken(statusToken, "Reason");

			var serializedToken = statusToken.ToString();
			if (errorToken != null)
			{
				UiExtensionsHandler?.ParseErrorStatus(statusToken);
				//This is an Error - Deal with it somehow?

				this.LogError("Error in Status Response: {error}", statusToken.ToString());
				return;
			}

			JsonConvert.PopulateObject(serializedToken, status);

			if (status?.UserInterface?.WebViews != null && status.UserInterface.WebViews.Count > 0)
			{
				UiExtensionsHandler?.ParseStatus(status.UserInterface.WebViews);
			}

			var standbyToken = statusToken.SelectToken("Standby");
			if (standbyToken != null)
			{
				var currentStandbyStatusToken = (string)standbyToken.SelectToken("State.Value");
				if (!string.IsNullOrEmpty(currentStandbyStatusToken))
				{
					switch (currentStandbyStatusToken.ToLower())
					{
						case "standby":
							_standbyState = StandbyState.Standby;
							this.LogInformation("Standby State = Standby");
                            break;
						case "enteringstandby":
							_standbyState = StandbyState.EnteringStandby;
							this.LogInformation("Standby State = EnteringStandby");
                            break;
						case "off":
							_standbyState = StandbyState.Off;
							this.LogInformation("Standby State = Off");
                            break;
						case "halfwake":
                            _standbyState = StandbyState.HalfWake;
							this.LogInformation("Standby State = HalfWake");
                            break;
						default:
							_standbyState = StandbyState.Unknown;
							this.LogError("Unknown Standby State: {state}", currentStandbyStatusToken);
							break;
                    }

					StandbyIsOnFeedback.FireUpdate();
					EnteringStandbyModeFeedback.FireUpdate();
					HalfWakeModeIsOnFeedback.FireUpdate();

                    return;
				}
			}
			if (legacyLayoutsToken != null && !EnhancedLayouts)
			{
				var localValueToken = (string)legacyLayoutsToken.SelectToken("Local.Value");
				if (!string.IsNullOrEmpty(localValueToken))
				{
					OnAvailableLayoutsChanged(_legacyLayouts);
					ComputeLegacyLayout(localValueToken);
					CurrentLayout = localValueToken;
					OnCurrentLayoutChanged(CurrentLayout);
				}
			}

			if (systemUnitToken != null)
			{
				ParseSystemUnit(systemUnitToken);
			}
			if (cameraToken != null)
			{
				var listWasUpdated = false;
				var cameraInfo = cameraToken.ToObject<List<JObject>>();

				foreach (var cam in cameraInfo)
				{
					var modernId = cam.SelectToken("CameraId")?.ToString();
					var legacyId = cam.SelectToken("id")?.ToString();
					var camId = string.IsNullOrEmpty(modernId) ? legacyId : modernId;

					if (string.IsNullOrEmpty(camId))
					{
						this.LogDebug("CameraId and id are null or empty. Skipping camera.");
						continue;
					}

					var existingCam =
						CodecStatus.Status.Cameras.CameraList.FirstOrDefault(c => c.CameraId == camId);

					if (existingCam == null)
					{
						var newCam = cam.ToObject<CiscoCodecStatus.Camera>();
						CodecStatus.Status.Cameras.CameraList.Add(newCam);
					}
					else
					{
						JsonConvert.PopulateObject(
							cam.ToString(),
							existingCam,
							new JsonSerializerSettings
							{
								NullValueHandling = NullValueHandling.Ignore,
								MissingMemberHandling = MissingMemberHandling.Ignore,
							});
					}

					listWasUpdated = true;
				}

				if (listWasUpdated)
				{
					this.LogDebug(
							"Connected Cameras: {@cameras}",
							CodecStatus.Status.Cameras.CameraList.Count(c =>
								c.Connected?.Value.ToLower() == "true"));

					foreach (var cam in CodecStatus.Status.Cameras.CameraList)
					{
						this.LogDebug(
							"Camera: {cameraId} connected: {connected} serial: {serialNumber}",
							cam.CameraId, cam.Connected?.Value ?? "false",
							cam.SerialNumber?.Value ?? "--empty---");
					}
				}
			}
			if (speakerTrackToken != null)
			{
				ParseSpeakerTrackToken(speakerTrackToken);
			}
			if (presenterTrackToken != null)
			{
				ParsePresenterTrackToken(presenterTrackToken);
			}
			if (networkToken != null)
			{
				ParseNetworkToken(networkToken);
			}
			if (sipToken != null)
			{
				ParseSipToken(sipToken);
			}
			if (layoutsToken != null)
			{
				ParseLayoutToken(layoutsToken);
			}
			if (selfviewToken != null)
			{
				ParseSelfviewToken(selfviewToken);
			}
			if (conferenceToken != null)
			{
				ParseConferenceToken(conferenceToken);
			}
			if (callToken != null)
			{
				ParseCallArrayToken(callToken);
			}
			if (mediaChannelsToken != null)
			{
				ParseMediaChannelsTokenArray(mediaChannelsToken);
			}
			if (status.Audio != null)
			{
				PopulateObjectWithToken(statusToken, "Audio", CodecStatus.Status.Audio);
			}
			if (status.RoomPresets != null)
			{
				ParseRoomPresetList(status.RoomPresets);
			}
			if (webViewStatusToken != null)
			{
				ParseWebviewStatusToken(webViewStatusToken[0]);
			}

			// we don't want to do this... this will expand lists infinitely
			//JsonConvert.PopulateObject(serializedToken, CodecStatus.Status);

			if (SyncState.InitialStatusMessageWasReceived)
				return;

			SyncState.InitialStatusMessageReceived();

			if (!SyncState.InitialConfigurationMessageWasReceived)
			{
				this.LogDebug("Sending Configuration");
				SendText("xConfiguration");
			}
			if (SyncState.FeedbackWasRegistered)
				return;
			this.LogDebug("Sending Feedback");

			SendText(BuildFeedbackRegistrationExpression());
			UIExtensionsHandler.RegisterFeedback();
		}

		private void ParseSelfviewToken(JToken selfviewToken)
		{
			var selfviewValueToken = selfviewToken.SelectToken("Value");
			if (selfviewValueToken == null)
				return;
			var selfviewValue = selfviewValueToken.ToString();
			if (string.IsNullOrEmpty(selfviewValue))
				return;
			if (CodecStatus.Status.Video.Selfview.SelfViewMode != null)
				CodecStatus.Status.Video.Selfview.SelfViewMode.Value = selfviewValue;
		}

		private void ParseConferenceToken(JToken conferenceToken)
		{
			var ghostToken = JTokenValidInToken(
				conferenceToken,
				"Presentation.LocalInstance[0].ghost"
			);
			if (!ProcessConferencePresentationGhost(ghostToken))
			{
				var sourceToken = JTokenValidInToken(
					conferenceToken,
					"Presentation.LocalInstance[0].Source.Value"
				);
				var sendingModeToken = JTokenValidInToken(
					conferenceToken,
					"Presentation.LocalInstance[0].SendingMode.Value"
				);
				var modeToken = JTokenValidInToken(conferenceToken, "Presentation.Mode.Value");
				if (sourceToken != null)
				{
					SetPresentationSource(sourceToken.ToString());
				}
				if (sendingModeToken != null)
				{
					SetPresentationMode(sendingModeToken.ToString());
				}
				if (modeToken != null)
				{
					if (string.IsNullOrEmpty(modeToken.ToString()))
						return;
					_IsInPresentation = modeToken.ToString().ToLower() != "off";
					CodecPollLayouts();
				}
			}

			WebexPinRequestHandler.ParseAuthenticationRequest(conferenceToken);
			DoNotDisturbHandler.ParseStatus(conferenceToken);

		}

		private bool ProcessConferencePresentationGhost(JToken ghostToken)
		{
			if (ghostToken != null && ghostToken.ToString().ToLower() == "true")
			{
				SetPresentationSource(0);
				SetPresentationLocalOnly(false);
				SetPresentationLocalRemote(false);
				SetPresentationActiveState(false);
				return true;
			}
			return false;
		}

		private void ParseConfigurationObject(JToken configurationToken)
		{
			try
			{
				if (configurationToken == null || (configurationToken.Type == JTokenType.Object && !configurationToken.HasValues))
					return;
				var configuration = new CiscoCodecConfiguration.Configuration();
				try
				{
					if (configuration.H323.H323Alias != null)
					{
						PopulateObjectWithToken(
							configurationToken,
							"H323.H323Alias",
							CodecConfiguration.Configuration.H323.H323Alias
						);
					}
				}
				catch (Exception e)
				{
					this.LogError("Exception in ParseConfigurationObject H323 : {message}", e.Message);
					this.LogVerbose(e, "Exception");
				}

				try
				{
					if (configuration.Conference.AutoAnswer != null)
					{
						PopulateObjectWithToken(
							configurationToken,
							"Conference.AutoAnswer",
							CodecConfiguration.Configuration.Conference.AutoAnswer
						);
					}
				}
				catch (Exception e)
				{
					this.LogError("Exception in ParseConfigurationObject.Populat Autoanswer: {message}", e.Message);
					this.LogVerbose(e, "Exception");
					throw;
				}
				if (SyncState.InitialConfigurationMessageWasReceived)
					return;
				this.LogVerbose("InitialConfig Received");
				SyncState.InitialConfigurationMessageReceived();
				if (!SyncState.InitialSoftwareVersionMessageWasReceived)
				{
					SendText("xStatus SystemUnit");
				}
			}
			catch (Exception e)
			{
				this.LogError("Exception in ParseConfigurationObject : {message}", e.Message);
				this.LogVerbose(e, "Exception");
			}
		}

		private void ParsePhonebookDirectoryResponseTypical(
			CiscoCodecExtendedPhonebook.PhonebookSearchResult phonebookSearchResultResponseObject,
			string resultId
		)
		{
			try
			{
				while (_searches.Count > 0)
				{
					var expectedResultId = _searches.Dequeue();
					this.LogDebug("Expected = {0} ; Parsed = {1}", expectedResultId,
						resultId
					);
					if (resultId != expectedResultId)
						continue;

					var directoryResults = new CodecDirectory();

					if (phonebookSearchResultResponseObject.ResultInfo.TotalRows.Value != "0")
						directoryResults =
							CiscoCodecExtendedPhonebook.ConvertCiscoPhonebookToGeneric(
								phonebookSearchResultResponseObject
							);

					PrintDirectory(directoryResults);

					DirectoryBrowseHistory.Add(directoryResults);

					OnDirectoryResultReturned(directoryResults);
					_searchInProgress = false;
					DirectorySearchInProgress.FireUpdate();
				}
			}
			catch (Exception ex)
			{
				this.LogError("Exception in ParsePhonebookDirectoryResponseTypical : {message}", ex.Message);
				this.LogVerbose(ex, "Exception");
			}
		}

		private void ParsePhonebookDirectoryFolders(
			CiscoCodecExtendedPhonebook.PhonebookSearchResult phonebookSearchResultResponseObject
		)
		{
			try
			{
				PhonebookSyncState.InitialPhonebookFoldersReceived();

				PhonebookSyncState.SetPhonebookHasFolders(
					phonebookSearchResultResponseObject.Folder.Count > 0
				);

				if (PhonebookSyncState.PhonebookHasFolders)
				{
					DirectoryRoot.AddFoldersToDirectory(
						CiscoCodecExtendedPhonebook.GetRootFoldersFromSearchResult(
							phonebookSearchResultResponseObject
						)
					);
				}

				// Get the number of contacts in the phonebook
				GetPhonebookContacts();
			}
			catch (Exception ex)
			{
				this.LogError("Exception in ParsePhonebookDirectoryFolders : {message}", ex.Message);
				this.LogVerbose(
					ex,
					"Exception"
				);
			}
		}

		private void ParsePhonebookNumberOfContacts(
			CiscoCodecExtendedPhonebook.PhonebookSearchResult phonebookSearchResultResponseObject
		)
		{
			try
			{
				if (PhonebookSyncState == null)
					return;
				PhonebookSyncState.SetNumberOfContacts(
					int.Parse(phonebookSearchResultResponseObject.ResultInfo.TotalRows.Value)
				);
				if (DirectoryRoot == null)
					return;
				DirectoryRoot.AddContactsToDirectory(
					CiscoCodecExtendedPhonebook.GetRootContactsFromSearchResult(
						phonebookSearchResultResponseObject
					)
				);
				PhonebookSyncState.PhonebookRootEntriesReceived();
				PrintDirectory(DirectoryRoot);
			}
			catch (Exception ex)
			{
				this.LogError("Exception in ParsePhonebookNumberOfContacts : {message}", ex.Message);
				this.LogVerbose(ex, "Exception");
			}
		}

		private void ParseEventObject(JToken eventToken)
		{
			if (
				eventToken == null
				|| (eventToken.Type == JTokenType.Object && !eventToken.HasValues)
			)
			{
				return;
			}

			try
			{
				var codecEvent = new CiscoCodecEvents.EventObject();
				var bookingsEvent = eventToken.SelectToken("Bookings");
				//var userInterfaceEvent = eventToken.SelectToken("UserInterface.Presentation.ExternalSource.Selected.SourceIdentifier");
				var userInterfaceEvent = eventToken.SelectToken("UserInterface");
				var conferenceEvent = eventToken.SelectToken("Conference");

				if (codecEvent.CallDisconnect != null)
				{
					PopulateObjectWithToken(
						eventToken,
						"CallDisconnect",
						codecEvent.CallDisconnect
					);
					EvalutateDisconnectEvent(codecEvent);
				}
				if (bookingsEvent != null)
				{
					GetBookings(null);
				}
				if (userInterfaceEvent != null)
				{
					PopulateObjectWithToken(eventToken, "UserInterface", codecEvent.UserInterface);

					ParseUserInterfaceEvent(codecEvent.UserInterface);
				}
				if (conferenceEvent != null)
				{
					WebexPinRequestHandler.ParseAuthenticationResponse(conferenceEvent);
				}
			}
			catch (Exception ex)
			{
				this.LogError("Exception parsing event: {message}", ex.Message);
				this.LogVerbose(ex, "Exception");
			}
		}

		private void ParseCallHistoryResponseToken(JToken callHistoryResponseToken)
		{
			if (callHistoryResponseToken == null)
				return;
			var codecCallHistory = new CiscoCallHistory.CallHistoryRecentsResult();
			PopulateObjectWithToken(
				callHistoryResponseToken,
				"CallHistoryRecentsResult",
				codecCallHistory
			);
			CallHistory.ConvertCiscoCallHistoryToGeneric(codecCallHistory.Entry);
		}

		private void ParsePhonebookSearchResultResponse(
			JToken phonebookSearchResultResponseToken,
			string resultId
		)
		{
			try
			{
				var phonebookSearchResultResponseObject =
					new CiscoCodecExtendedPhonebook.PhonebookSearchResult();
				PopulateObjectWithToken(
					phonebookSearchResultResponseToken,
					"PhonebookSearchResult",
					phonebookSearchResultResponseObject
				);

				if (!PhonebookSyncState.InitialPhonebookFoldersWasReceived)
				{
					ParsePhonebookDirectoryFolders(phonebookSearchResultResponseObject);
					// Check if the phonebook has any folders
					return;
				}
				if (!PhonebookSyncState.NumberOfContactsWasReceived)
				{
					ParsePhonebookNumberOfContacts(phonebookSearchResultResponseObject);
					PhonebookSyncState.PhonebookRootEntriesReceived();
				}
				ParsePhonebookDirectoryResponseTypical(
					phonebookSearchResultResponseObject,
					resultId
				);
			}
			catch (Exception ex)
			{
				this.LogError(
					"Exception in ParsePhonebookSearchResultResponse : {message}", ex.Message);
				this.LogVerbose(ex, "Exception");
			}
		}

		private void ParseCommandResponseObject(JToken commandResponseToken, string resultId)
		{
			if (commandResponseToken == null || (commandResponseToken.Type == JTokenType.Object && !commandResponseToken.HasValues))
				return;
			var callHistoryRecentsResultResponse = commandResponseToken.SelectToken(
				"CallHistoryRecentsResult"
			);
			var callHistoryDeleteEntryResultResponse = commandResponseToken.SelectToken(
				"CallHistoryDeleteEntryResult"
			);
			var phonebookSearchResultResponse = commandResponseToken.SelectToken(
				"PhonebookSearchResult"
			);
			var bookingsListResultResponse = commandResponseToken.SelectToken("BookingsListResult");
			var presentationStatus = commandResponseToken.SelectToken(
				"PresentationStopResult.status"
			);
			var statusToken = commandResponseToken.SelectToken("status");
			var errorToken = JTokenValidInToken(statusToken, "Reason");

			if (errorToken != null)
			{
				this.LogError("Error in command response: {error}", errorToken.ToString());
				return;
			}

			if (statusToken != null)
			{
				if (statusToken.ToString().ToLower() == "error")
					//do Something
					return;
			}
			if (callHistoryRecentsResultResponse != null)
			{
				ParseCallHistoryResponseToken(commandResponseToken);
				return;
			}
			if (callHistoryDeleteEntryResultResponse != null)
			{
				GetCallHistory();
				return;
			}
			if (bookingsListResultResponse != null)
			{
				ParseBookingsListResultToken(commandResponseToken);
				return;
			}
			if (phonebookSearchResultResponse != null)
			{

				ParsePhonebookSearchResultResponse(commandResponseToken, resultId);
				return;
			}
			if (presentationStatus != null)
			{
				if (presentationStatus.ToString().ToLower() == "ok")
				{
					_presentationSource = 0;
					ClearLayouts();
				}
			}
		}

		private void ParseBookingsListResultToken(JToken bookingsResponseToken)
		{
			if (bookingsResponseToken == null)
				return;

			var codecBookings = new CiscoExtendedCodecBookings.BookingsListResult();

			PopulateObjectWithToken(bookingsResponseToken, "BookingsListResult", codecBookings);

			if (codecBookings.ResultInfo.TotalRows.Value != "0")
			{
				this.LogVerbose(
					"There are {meetingCount} meetings",
					codecBookings.ResultInfo.TotalRows.Value
				);
				CodecSchedule.Meetings =
					CiscoExtendedCodecBookings.GetGenericMeetingsFromBookingResult(
						codecBookings.BookingsListResultBooking,
						_joinableCooldownSeconds
					);
			}
			else
			{
				this.LogVerbose("There are No Meetings");
				CodecSchedule.Meetings = new List<Meeting>();
			}
			if (BookingsRefreshTimer == null)
			{
				BookingsRefreshTimer = new CTimer(GetBookings, 90000, 90000);
				this.LogVerbose("BookingsRefresh Was null");
			}
			BookingsRefreshTimer.Reset(90000, 90000);
		}

		private static string ParseResultId(JObject obj)
		{
			try
			{
				return obj["ResultId"].ToString();
			}
			catch
			{
				return Guid.Empty.ToString();
			}
		}

		private void DeserializeResponse(string response)
		{
			try
			{
				using (var sReader = new StringReader(response))
				using (var jReader = new JsonTextReader(sReader))
				{
					while (jReader.Read())
					{
						if (jReader.TokenType != JsonToken.StartObject)
							continue;
						var obj = JObject.Load(jReader);

						var resultId = ParseResultId(obj);

						ParseStatusObject(JTokenValidInObject(obj, "Status"));
						ParseConfigurationObject(JTokenValidInObject(obj, "Configuration"));
						ParseEventObject(JTokenValidInObject(obj, "Event"));
						ParseCommandResponseObject(
							JTokenValidInObject(obj, "CommandResponse"),
							resultId
						);
					}
				}
			}
			catch (JsonReaderException ex)
			{
				this.LogError("Json Error deserializing response from codec: {error} at line number:{lineNumber} line position:{linePosition}", ex.Message, ex.LineNumber, ex.LinePosition);
				this.LogVerbose(ex, "Exception");
			}
			catch (Exception ex)
			{
				this.LogError("Error deserializing feedback from codec: {error}", ex.Message);
				this.LogVerbose(ex, "Exception");
			}
		}

		private void OnDirectoryResultReturned(CodecDirectory result)
		{
			if (result == null)
			{
				return;
			}

			CurrentDirectoryResultIsNotDirectoryRoot.FireUpdate();

			// This will return the latest results to all UIs.  Multiple indendent UI Directory browsing will require a different methodology
			DirectoryResultReturned?.Invoke(
					this,
					new DirectoryEventArgs()
					{
						Directory = result,
						DirectoryIsOnRoot = !CurrentDirectoryResultIsNotDirectoryRoot.BoolValue
					}
				);

			PrintDirectory(result);
		}

		private void ComputeLegacyLayout()
		{
			if (EnhancedLayouts)
				return;
			_currentLegacyLayout = _legacyLayouts.FirstOrDefault(l =>
				l.Command.ToLower()
					.Equals(CodecStatus.Status.Video.Layout.LayoutFamily.Local.Value.ToLower())
			);

			if (_currentLegacyLayout != null)
				LocalLayoutFeedback.FireUpdate();
		}

		private void ComputeLegacyLayout(string layoutName)
		{
			if (EnhancedLayouts)
				return;
			_currentLegacyLayout = _legacyLayouts.FirstOrDefault(l =>
				l.Command.ToLower().Equals(layoutName.ToLower())
			);

			if (_currentLegacyLayout != null)
				LocalLayoutFeedback.FireUpdate();
		}

		private string UpdateLayoutsXSig(ICollection<CodecCommandWithLabel> layoutList)
		{
			//Debug.Console(0, this, "UPDATE LAYOUT XSIG!!!");
			var layouts = layoutList;
			var layoutIndex = 1;
			var tokenArray = new XSigToken[layouts.Count];
			if (layouts.Count == 0)
			{
				//Debug.Console(0, this, "NO LAYOUTS IN XSIG!!!");
				var clearBytes = XSigHelpers.ClearOutputs();
				return Encoding
					.GetEncoding(XSigEncoding)
					.GetString(clearBytes, 0, clearBytes.Length);
			}

			foreach (var layout in layouts)
			{
				var arrayIndex = layoutIndex - 1;
				this.LogVerbose("Layout Name : {0}", layout.Label);

				tokenArray[arrayIndex] = new XSigSerialToken(layoutIndex, layout.Label);
				layoutIndex++;
			}

			return GetXSigString(tokenArray);
		}

		private static string GetXSigString(XSigToken[] tokenArray)
		{
			string returnString;
			using (var s = new MemoryStream())
			{
				using (var tw = new XSigTokenStreamWriter(s, true))
				{
					tw.WriteXSigData(tokenArray);
				}

				var xSig = s.ToArray();

				returnString = Encoding.GetEncoding(XSigEncoding).GetString(xSig, 0, xSig.Length);
			}

			return returnString;
		}

		public void CodecPollLayouts()
		{
			EnqueueCommand(
				EnhancedLayouts
					? "xStatus Video Layout CurrentLayouts"
					: "xStatus Video Layout LayoutFamily"
			);
		}

		private void EvalutateDisconnectEvent(CiscoCodecEvents.EventObject eventReceived)
		{
			if (
				eventReceived == null
				|| eventReceived.CallDisconnect == null
				|| eventReceived.CallDisconnect.CallId == null
			)
				return;
			var tempActiveCall = ActiveCalls.FirstOrDefault(c =>
				c.Id.Equals(eventReceived.CallDisconnect.CallId.Value)
			);

			if (tempActiveCall == null)
			{
				return;
			}

			this.LogDebug("DISCONNECT CALL {0}!", tempActiveCall.Id);

			ActiveCalls.Remove(tempActiveCall);

			ListCalls();

			SetSelfViewMode();
			// Notify of the call disconnection
			SetNewCallStatusAndFireCallStatusChange(eCodecCallStatus.Disconnected, tempActiveCall);

			GetCallHistory();
		}

		public override void ExecuteSwitch(object selector)
		{
			if (selector as Action == null)
				return;
			(selector as Action)();
			_presentationSourceKey = selector.ToString();
		}

		public void ExecuteSwitch(
			object inputSelector,
			object outputSelector,
			eRoutingSignalType signalType
		)
		{
			ExecuteSwitch(inputSelector);
			_presentationSourceKey = inputSelector.ToString();
		}

		public string GetCallId()
		{
			string callId = null;

			if (ActiveCalls.Count > 1)
			{
				var lastCallIndex = ActiveCalls.Count - 1;
				callId = ActiveCalls[lastCallIndex].Id;
			}
			else if (ActiveCalls.Count == 1)
				callId = ActiveCalls[0].Id;

			return callId;
		}

		public void GetCallHistory()
		{
			EnqueueCommand("xCommand CallHistory Recents Limit: 20 Order: OccurrenceTime");
		}

		public void GetSchedule()
		{
			GetBookings(null);
		}

		public void GetBookings(object command)
		{
			EnqueueCommand("xCommand Bookings List Days: 1 DayOffset: 0");
		}

		public void CheckCurrentHour(object o)
		{
			if (DateTime.Now.Hour == 2)
			{
				GetPhonebook(null);
				PhonebookRefreshTimer.Reset(3600000, 3600000);
			}
		}

		public void GetPhonebook(string command)
		{
			PhonebookSyncState.CodecDisconnected();

			DirectoryRoot = new CodecDirectory();

			GetPhonebookFolders();
		}

		private void GetPhonebookFolders()
		{
			// Get Phonebook Folders (determine local/corporate from config, and set results limit)
			EnqueueCommand(
				string.Format(
					"xCommand Phonebook Search PhonebookType: {0} ContactType: Folder",
					_phonebookMode
				)
			);
		}

		private void GetPhonebookContacts()
		{
			// Get Phonebook Folders (determine local/corporate from config, and set results limit)
			EnqueueCommand(
				string.Format(
					"xCommand Phonebook Search PhonebookType: {0} ContactType: Contact Limit: {1}",
					_phonebookMode,
					_phonebookResultsLimit
				)
			);
		}

		private readonly CrestronQueue<string> _searches = new CrestronQueue<string>();
		private bool _searchInProgress;

		public void SearchDirectory(string searchString)
		{
			this.LogVerbose(
				"_phonebookAutoPopulate = {autoPopulate}, searchString = {searchString}, _lastSeached = {lastSearched}, _phonebookInitialSearch = {initialSearch}",
				_phonebookAutoPopulate ? "true" : "false",
				searchString,
				_lastSearched,
				_phonebookInitialSearch ? "true" : "false"
			);

			if (
				!_phonebookAutoPopulate
				&& searchString == _lastSearched
				&& !_phonebookInitialSearch
			)
				return;

			_searchInProgress = !string.IsNullOrEmpty(searchString);
			var tag = Guid.NewGuid();
			_searches.Enqueue(tag.ToString());
			EnqueueCommand(
				string.Format(
					"xCommand Phonebook Search SearchString: \"{0}\" PhonebookType: {1} ContactType: Contact Limit: {2} | resultId=\"{3}\"",
					searchString,
					_phonebookMode,
					_phonebookResultsLimit,
					tag
				)
			);
			_lastSearched = searchString;
			_phonebookInitialSearch = false;
			DirectorySearchInProgress.FireUpdate();
		}

		public void GetDirectoryFolderContents(string folderId)
		{
			EnqueueCommand(
				string.Format(
					"xCommand Phonebook Search FolderId: {0} PhonebookType: {1} ContactType: Any Limit: {2}",
					folderId,
					_phonebookMode,
					_phonebookResultsLimit
				)
			);
		}

		public void GetDirectoryParentFolderContents()
		{
			var currentDirectory = new CodecDirectory();

			if (DirectoryBrowseHistory.Count > 0)
			{
				var lastItemIndex = DirectoryBrowseHistory.Count - 1;
				var parentDirectoryContents = DirectoryBrowseHistory[lastItemIndex];

				DirectoryBrowseHistory.Remove(DirectoryBrowseHistory[lastItemIndex]);

				currentDirectory = parentDirectoryContents;
			}
			else
			{
				currentDirectory = DirectoryRoot;
			}

			OnDirectoryResultReturned(currentDirectory);
		}

		public void SetCurrentDirectoryToRoot()
		{
			DirectoryBrowseHistory.Clear();

			OnDirectoryResultReturned(DirectoryRoot);
		}

		private void PrintDirectory(CodecDirectory directory)
		{
			if (directory == null)
				return;

			foreach (var item in directory.CurrentDirectoryResults)
			{
				if (item is DirectoryFolder)
				{
					this.LogDebug("[+] {0}", item.Name);
				}
				else if (item is DirectoryContact)
				{
					this.LogDebug("{0}", item.Name);
				}
			}
		}

		public override void Dial(string number)
		{
			EnqueueCommand(string.Format("xCommand Dial Number: \"{0}\"", number));
		}

		public override void Dial(Meeting meeting)
		{
			if (EndAllCallsOnMeetingJoin)
				EndAllCalls();
			foreach (Call c in meeting.Calls)
			{
				Dial(c.Number, c.Protocol, c.CallRate, c.CallType, meeting.Id);
			}
		}

		public void Dial(
			string number,
			string protocol,
			string callRate,
			string callType,
			string meetingId
		)
		{
			EnqueueCommand(
				string.Format(
					"xCommand Dial Number: \"{0}\" Protocol: {1} CallRate: {2} CallType: {3} BookingId: {4}",
					number,
					protocol,
					callRate,
					callType,
					meetingId
				)
			);
		}

		public override void EndCall(CodecActiveCallItem activeCall)
		{
			EnqueueCommand(string.Format("xCommand Call Disconnect CallId: {0}", activeCall.Id));
		}


		public override void EndAllCalls()
		{
			foreach (CodecActiveCallItem activeCall in ActiveCalls)
			{
				EnqueueCommand(
					string.Format("xCommand Call Disconnect CallId: {0}", activeCall.Id)
				);
			}
		}

		public override void AcceptCall(CodecActiveCallItem item)
		{
			EnqueueCommand("xCommand Call Accept");
		}

		public override void RejectCall(CodecActiveCallItem item)
		{
			EnqueueCommand("xCommand Call Reject");
		}

		#region IHasCallHold Members

		public void HoldCall(CodecActiveCallItem activeCall)
		{
			EnqueueCommand(string.Format("xCommand Call Hold CallId: {0}", activeCall.Id));
		}

		public void ResumeCall(CodecActiveCallItem activeCall)
		{
			EnqueueCommand(string.Format("xCommand Call Resume CallId: {0}", activeCall.Id));
		}

		public void ResumeAllCalls()
		{
			foreach (
				var codecActiveCallItem in ActiveCalls.Where(codecActiveCallItem =>
					codecActiveCallItem.IsOnHold
				)
			)
			{
				ResumeCall(codecActiveCallItem);
			}
		}

		#endregion

		#region IJoinCalls

		public void JoinCall(CodecActiveCallItem activeCall)
		{
			EnqueueCommand(string.Format("xCommand Call Join CallId: {0}", activeCall.Id));
		}

		public void JoinAllCalls()
		{
			StringBuilder ids = new StringBuilder();

			foreach (var call in ActiveCalls)
			{
				if (call.IsActiveCall)
				{
					ids.Append(string.Format(" CallId: {0}", call.Id));
				}
			}

			if (ids.Length > 0)
			{
				EnqueueCommand(string.Format("xCommand Call Join {0}", ids.ToString()));
			}
		}

		#endregion

		public override void SendDtmf(string s)
		{
			EnqueueCommand(
				string.Format(
					"xCommand Call DTMFSend CallId: {0} DTMFString: \"{1}\"",
					GetCallId(),
					s
				)
			);
		}

		public override void SendDtmf(string s, CodecActiveCallItem activeCall)
		{
			EnqueueCommand(
				string.Format(
					"xCommand Call DTMFSend CallId: {0} DTMFString: \"{1}\"",
					activeCall.Id,
					s
				)
			);
		}

		public void SelectPresentationSource(int source)
		{
			_desiredPresentationSource = source;

			StartSharing();
		}

		public void SetRingtoneVolume(int volume)
		{
			if (volume < 0 || volume > 100)
			{
				this.LogWarning(
					"Cannot set ringtone volume to '{volume}'. Value must be between 0 - 100",
					volume
				);
				return;
			}

			if (volume % 5 != 0)
			{
				this.LogWarning(
					"Cannot set ringtone volume to '{volume}'. Value must be between 0 - 100 and a multiple of 5",
					volume
				);
				return;
			}

			EnqueueCommand(
				string.Format("xConfiguration Audio SoundsAndAlerts RingVolume: {0}", volume)
			);
		}

		public void SelectPresentationSource1()
		{
			SelectPresentationSource(2);
		}

		public void SelectPresentationSource2()
		{
			SelectPresentationSource(3);
		}


		public override void StartSharing()
		{
			if (_desiredPresentationSource > 0)
				EnqueueCommand(
					string.Format(
						"xCommand Presentation Start PresentationSource: {0} SendingMode: {1}",
						_desiredPresentationSource,
						PresentationStates.ToString()
					)
				);
		}

		public override void StopSharing()
		{
			_desiredPresentationSource = 0;

			EnqueueCommand("xCommand Presentation Stop");
		}

		public override void PrivacyModeOn()
		{
			EnqueueCommand("xCommand Audio Microphones Mute");
		}

		public override void PrivacyModeOff()
		{
			EnqueueCommand("xCommand Audio Microphones Unmute");
		}

		public override void PrivacyModeToggle()
		{
			EnqueueCommand("xCommand Audio Microphones ToggleMute");
		}

		public override void MuteOff()
		{
			EnqueueCommand("xCommand Audio Volume Unmute");
		}

		public override void MuteOn()
		{
			EnqueueCommand("xCommand Audio Volume Mute");
		}

		public override void MuteToggle()
		{
			EnqueueCommand("xCommand Audio Volume ToggleMute");
		}

		public override void VolumeUp(bool pressRelease)
		{
			EnqueueCommand("xCommand Audio Volume Increase");
		}

		public override void VolumeDown(bool pressRelease)
		{
			EnqueueCommand("xCommand Audio Volume Decrease");
		}

		public override void SetVolume(ushort level)
		{
			var scaledLevel = CrestronEnvironment.ScaleWithLimits(level, 65535, 0, 100, 0);
			EnqueueCommand(string.Format("xCommand Audio Volume Set Level: {0}", scaledLevel));
		}

		public void VolumeSetToDefault()
		{
			EnqueueCommand("xCommand Audio Volume SetToDefault");
		}

		public override void StandbyActivate()
		{
			EnqueueCommand("xCommand Standby Activate");
		}

		public override void StandbyDeactivate()
		{
			EnqueueCommand("xCommand Standby Deactivate");
		}

		public override void LinkToApi(
			BasicTriList trilist,
			uint joinStart,
			string joinMapKey,
			EiscApiAdvanced bridge
		)
		{
			var joinMap = new CiscoCodecJoinMap(joinStart);

			var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

			if (customJoins != null)
			{
				joinMap.SetCustomJoinData(customJoins);
			}

			bridge?.AddJoinMap(Key, joinMap);

			LinkVideoCodecToApi(this, trilist, joinMap);

			LinkCiscoCodecToApi(trilist, joinMap);

			WebexPinRequestHandler.LinkToApi(trilist, joinMap);

			LinkCiscoCodecWebex(trilist, joinMap);

			LinkCiscoCodecZoomConnector(trilist, joinMap);

			UIExtensionsHandler.LinkToApi(trilist, joinMap);
		}

		public void LinkCiscoCodecZoomConnector(BasicTriList trilist, CiscoCodecJoinMap joinMap)
		{
			trilist.SetStringSigAction(joinMap.ZoomMeetingId.JoinNumber, s => ZoomMeetingId = s);
			trilist.SetStringSigAction(
				joinMap.ZoomMeetingPasscode.JoinNumber,
				s => ZoomMeetingPasscode = s
			);
			trilist.SetStringSigAction(
				joinMap.ZoomMeetingCommand.JoinNumber,
				s => ZoomMeetingCommand = s
			);
			trilist.SetStringSigAction(
				joinMap.ZoomMeetingHostKey.JoinNumber,
				s => ZoomMeetingHostKey = s
			);
			trilist.SetStringSigAction(
				joinMap.ZoomMeetingReservedCode.JoinNumber,
				s => ZoomMeetingReservedCode = s
			);
			trilist.SetStringSigAction(
				joinMap.ZoomMeetingDialCode.JoinNumber,
				s => ZoomMeetingDialCode = s
			);
			trilist.SetStringSigAction(joinMap.ZoomMeetingIp.JoinNumber, s => ZoomMeetingIp = s);

			trilist.SetSigTrueAction(joinMap.ZoomMeetingDial.JoinNumber, DialZoom);

			trilist.SetSigTrueAction(
				joinMap.ZoomMeetingClear.JoinNumber,
				() =>
				{
					ZoomMeetingId = string.Empty;
					ZoomMeetingPasscode = string.Empty;
					ZoomMeetingCommand = string.Empty;
					ZoomMeetingHostKey = string.Empty;
					ZoomMeetingReservedCode = string.Empty;
					ZoomMeetingDialCode = string.Empty;
					ZoomMeetingIp = string.Empty;
				}
			);
		}

		private void LinkCiscoCodecWebex(BasicTriList trilist, CiscoCodecJoinMap joinMap)
		{
			trilist.SetStringSigAction(
				joinMap.WebexMeetingNumber.JoinNumber,
				s => WebexMeetingNumber = s
			);
			trilist.SetStringSigAction(
				joinMap.WebexMeetingRole.JoinNumber,
				s => WebexMeetingRole = s
			);
			trilist.SetStringSigAction(
				joinMap.WebexMeetingPin.JoinNumber,
				s => WebexMeetingPin = s
			);

			trilist.SetSigTrueAction(joinMap.WebexDial.JoinNumber, DialWebex);

			trilist.SetSigTrueAction(
				joinMap.WebexDialClear.JoinNumber,
				() =>
				{
					WebexMeetingNumber = string.Empty;
					WebexMeetingRole = string.Empty;
					WebexMeetingPin = string.Empty;
				}
			);
		}

		public void LinkCiscoCodecToApi(BasicTriList trilist, CiscoCodecJoinMap joinMap)
		{
			// Custom commands to codec
			trilist.SetStringSigAction(joinMap.CommandToDevice.JoinNumber, EnqueueCommand);

			var dndCodec = this as IHasDoNotDisturbMode;
			dndCodec.DoNotDisturbModeIsOnFeedback.LinkInputSig(
				trilist.BooleanInput[joinMap.ActivateDoNotDisturbMode.JoinNumber]
			);

			trilist.SetSigFalseAction(
				joinMap.ActivateDoNotDisturbMode.JoinNumber,
				dndCodec.ActivateDoNotDisturbMode
			);
			trilist.SetSigFalseAction(
				joinMap.DeactivateDoNotDisturbMode.JoinNumber,
				dndCodec.DeactivateDoNotDisturbMode
			);
			trilist.SetSigFalseAction(
				joinMap.ToggleDoNotDisturbMode.JoinNumber,
				dndCodec.ToggleDoNotDisturbMode
			);
			trilist.SetSigFalseAction(joinMap.CameraPresetRecall.JoinNumber, CiscoRoomPresetRecall);

			trilist.SetSigFalseAction(
				joinMap.DialActiveMeeting.JoinNumber,
				() =>
				{
					if (_currentMeeting == null)
						return;
					this.LogDebug(
						"Active Meeting Selected  > _Id: {id}, Title: {title}",
						_currentMeeting.Id,
						_currentMeeting.Title
					);
					Dial(_currentMeeting);
				}
			);

			var halfwakeCodec = this as IHasHalfWakeMode;
			halfwakeCodec.StandbyIsOnFeedback.LinkInputSig(
				trilist.BooleanInput[joinMap.ActivateStandby.JoinNumber]
			);
			halfwakeCodec.StandbyIsOnFeedback.LinkComplementInputSig(
				trilist.BooleanInput[joinMap.DeactivateStandby.JoinNumber]
			);
			halfwakeCodec.HalfWakeModeIsOnFeedback.LinkInputSig(
				trilist.BooleanInput[joinMap.ActivateHalfWakeMode.JoinNumber]
			);
			halfwakeCodec.EnteringStandbyModeFeedback.LinkInputSig(
				trilist.BooleanInput[joinMap.EnteringStandbyMode.JoinNumber]
			);

			trilist.SetSigFalseAction(
				joinMap.ActivateStandby.JoinNumber,
				halfwakeCodec.StandbyActivate
			);
			trilist.SetSigFalseAction(
				joinMap.DeactivateStandby.JoinNumber,
				halfwakeCodec.StandbyDeactivate
			);
			trilist.SetSigFalseAction(
				joinMap.ActivateHalfWakeMode.JoinNumber,
				halfwakeCodec.HalfwakeActivate
			);

			CameraTrackingCapabilitiesChanged += (sender, args) =>
			{
				switch (CameraTrackingCapabilities)
				{
					case eCameraTrackingCapabilities.None:
						{
							trilist.SetBool(joinMap.SpeakerTrackAvailable.JoinNumber, false);
							trilist.SetBool(joinMap.PresenterTrackAvailable.JoinNumber, false);
							trilist.SetBool(joinMap.CameraSupportsAutoMode.JoinNumber, false);
							break;
						}
					case eCameraTrackingCapabilities.PresenterTrack:
						{
							trilist.SetBool(joinMap.SpeakerTrackAvailable.JoinNumber, false);
							trilist.SetBool(joinMap.PresenterTrackAvailable.JoinNumber, true);
							trilist.SetBool(joinMap.CameraSupportsAutoMode.JoinNumber, true);
							break;
						}
					case eCameraTrackingCapabilities.SpeakerTrack:
						{
							trilist.SetBool(joinMap.SpeakerTrackAvailable.JoinNumber, true);
							trilist.SetBool(joinMap.PresenterTrackAvailable.JoinNumber, false);
							trilist.SetBool(joinMap.CameraSupportsAutoMode.JoinNumber, true);
							break;
						}
					case eCameraTrackingCapabilities.Both:
						{
							trilist.SetBool(joinMap.SpeakerTrackAvailable.JoinNumber, true);
							trilist.SetBool(joinMap.PresenterTrackAvailable.JoinNumber, true);
							trilist.SetBool(joinMap.CameraSupportsAutoMode.JoinNumber, true);
							break;
						}
				}
			};

			AvailableLayoutsChanged += (sender, args) =>
			{
				var layouts = args.AvailableLayouts;

				this.LogDebug("There are {count} layouts", layouts.Count);

				var clearBytes = XSigHelpers.ClearOutputs();

				trilist.SetString(
					joinMap.AvailableLayoutsFb.JoinNumber,
					Encoding.GetEncoding(XSigEncoding).GetString(clearBytes, 0, clearBytes.Length)
				);

				var availableLayoutsXSig = UpdateLayoutsXSig(layouts);

				trilist.SetString(joinMap.AvailableLayoutsFb.JoinNumber, availableLayoutsXSig);
			};

			CurrentLayoutChanged += (sender, args) =>
			{
				var currentLayout = args.CurrentLayout;

				this.LogVerbose(
					"CurrentLayout == {layout}",
					currentLayout == string.Empty ? "None" : currentLayout
				);

				trilist.SetString(joinMap.CurrentLayoutStringFb.JoinNumber, currentLayout);
			};

			CodecInfoChanged += (sender, args) =>
			{
				if (args.InfoChangeType == eCodecInfoChangeType.Unknown)
					return;
				switch (args.InfoChangeType)
				{
					case eCodecInfoChangeType.Network:
						trilist.SetString(joinMap.DeviceIpAddresss.JoinNumber, args.IpAddress);
						break;
					case eCodecInfoChangeType.Sip:
						trilist.SetString(joinMap.SipPhoneNumber.JoinNumber, args.SipPhoneNumber);
						trilist.SetString(joinMap.SipUri.JoinNumber, args.SipUri);
						break;
					case eCodecInfoChangeType.H323:
						trilist.SetString(joinMap.E164Alias.JoinNumber, args.E164Alias);
						trilist.SetString(joinMap.H323Id.JoinNumber, args.H323Id);
						break;
					case eCodecInfoChangeType.Multisite:
						trilist.SetBool(
							joinMap.MultiSiteOptionIsEnabled.JoinNumber,
							args.MultiSiteOptionIsEnabled
						);
						break;
					case eCodecInfoChangeType.AutoAnswer:
						trilist.SetBool(
							joinMap.AutoAnswerEnabled.JoinNumber,
							args.AutoAnswerEnabled
						);
						break;
				}
			};

			AvailableLayoutsFeedback.LinkInputSig(
				trilist.StringInput[joinMap.AvailableLayoutsFb.JoinNumber]
			);

			trilist.SetStringSigAction(joinMap.SelectLayout.JoinNumber, LayoutSet);

			trilist.SetSigTrueAction(joinMap.ResumeAllCalls.JoinNumber, ResumeAllCalls);

			// Ringtone volume
			trilist.SetUShortSigAction(
				joinMap.RingtoneVolume.JoinNumber,
				(u) => SetRingtoneVolume(u)
			);
			RingtoneVolumeFeedback.LinkInputSig(
				trilist.UShortInput[joinMap.RingtoneVolume.JoinNumber]
			);

			// Presentation SourceValueProperty
			trilist.SetUShortSigAction(
				joinMap.PresentationSource.JoinNumber,
				(u) => SelectPresentationSource(u)
			);
			PresentationSourceFeedback.LinkInputSig(
				trilist.UShortInput[joinMap.PresentationSource.JoinNumber]
			);
			ContentInputActiveFeedback.LinkInputSig(
				trilist.BooleanInput[joinMap.SourceShareStart.JoinNumber]
			);
			ContentInputActiveFeedback.LinkComplementInputSig(
				trilist.BooleanInput[joinMap.SourceShareEnd.JoinNumber]
			);

			trilist.SetSigTrueAction(
				joinMap.PresentationLocalOnly.JoinNumber,
				SetPresentationLocalOnly
			);
			trilist.SetSigTrueAction(
				joinMap.PresentationLocalRemote.JoinNumber,
				SetPresentationLocalRemote
			);
			trilist.SetSigTrueAction(
				joinMap.PresentationLocalRemoteToggle.JoinNumber,
				SetPresentationLocalRemoteToggle
			);

			PresentationViewDefaultFeedback.LinkInputSig(
				trilist.BooleanInput[joinMap.PresentationViewDefault.JoinNumber]
			);
			PresentationViewMinimizedFeedback.LinkInputSig(
				trilist.BooleanInput[joinMap.PresentationViewMinimized.JoinNumber]
			);
			PresentationViewMaximizedFeedback.LinkInputSig(
				trilist.BooleanInput[joinMap.PresentationViewMaximized.JoinNumber]
			);

			trilist.SetSigTrueAction(
				joinMap.PresentationViewDefault.JoinNumber,
				PresentationViewDefaultSet
			);
			trilist.SetSigTrueAction(
				joinMap.PresentationViewMinimized.JoinNumber,
				PresentationViewMinimizedzedSet
			);
			trilist.SetSigTrueAction(
				joinMap.PresentationViewMaximized.JoinNumber,
				PresentationViewMaximizedSet
			);

			PresentationSendingLocalOnlyFeedback.LinkInputSig(
				trilist.BooleanInput[joinMap.PresentationLocalOnly.JoinNumber]
			);
			PresentationSendingLocalRemoteFeedback.LinkInputSig(
				trilist.BooleanInput[joinMap.PresentationLocalRemote.JoinNumber]
			);

			//PresenterTrackAvailableFeedback.LinkInputSig(trilist.BooleanInput[joinMap.PresenterTrackEnabled.JoinNumber]);

			PresenterTrackStatusOffFeedback.LinkInputSig(
				trilist.BooleanInput[joinMap.PresenterTrackOff.JoinNumber]
			);
			PresenterTrackStatusFollowFeedback.LinkInputSig(
				trilist.BooleanInput[joinMap.PresenterTrackFollow.JoinNumber]
			);
			PresenterTrackStatusBackgroundFeedback.LinkInputSig(
				trilist.BooleanInput[joinMap.PresenterTrackBackground.JoinNumber]
			);
			PresenterTrackStatusPersistentFeedback.LinkInputSig(
				trilist.BooleanInput[joinMap.PresenterTrackPersistent.JoinNumber]
			);

			trilist.SetSigTrueAction(joinMap.PresenterTrackOff.JoinNumber, PresenterTrackOff);
			trilist.SetSigTrueAction(joinMap.PresenterTrackFollow.JoinNumber, PresenterTrackFollow);
			trilist.SetSigTrueAction(
				joinMap.PresenterTrackBackground.JoinNumber,
				PresenterTrackBackground
			);
			trilist.SetSigTrueAction(
				joinMap.PresenterTrackPersistent.JoinNumber,
				PresenterTrackPersistent
			);

			SpeakerTrackStatusOnFeedback.LinkInputSig(
				trilist.BooleanInput[joinMap.SpeakerTrackOn.JoinNumber]
			);
			SpeakerTrackStatusOnFeedback.LinkComplementInputSig(
				trilist.BooleanInput[joinMap.SpeakerTrackOff.JoinNumber]
			);

			trilist.SetSigTrueAction(joinMap.SpeakerTrackOn.JoinNumber, SpeakerTrackOn);
			trilist.SetSigTrueAction(joinMap.SpeakerTrackOff.JoinNumber, SpeakerTrackOff);
			trilist.SetSigTrueAction(
				joinMap.SpeakerTrackToggle.JoinNumber,
				() =>
				{
					if (SpeakerTrackStatusOnFeedback.BoolValue)
					{
						SpeakerTrackOff();
					}
					else
					{
						SpeakerTrackOn();
					}
				}
			);

			DirectorySearchInProgress.LinkInputSig(
				trilist.BooleanInput[joinMap.DirectorySearchBusy.JoinNumber]
			);
			PresentationActiveFeedback.LinkInputSig(
				trilist.BooleanInput[joinMap.PresentationActive.JoinNumber]
			);

			CodecSchedule.MeetingEventChange += (sender, args) =>
			{
				if (args.ChangeType != eMeetingEventChangeType.Unknown)
				{
					UpdateMeetingsListEnhanced(this, trilist, joinMap);
				}
			};

			MinuteChanged += (sender, args) =>
			{
				if (args.EventTime == DateTime.MinValue)
					return;
				_scheduleCheckLast = args.EventTime;
				UpdateMeetingsListEnhanced(this, trilist, joinMap);
			};
		}

		private void UpdateMeetingsListEnhanced(
			IHasScheduleAwareness codec,
			BasicTriList trilist,
			CiscoCodecJoinMap joinMap
		)
		{
			var currentTime = DateTime.Now;
			const string boilerplate1 = "Available for ";
			const string boilerplate2 = "Next meeting in ";

			_currentMeetings = codec
				.CodecSchedule.Meetings.Where(m =>
					m.StartTime >= currentTime || m.EndTime >= currentTime
				)
				.ToList();

			trilist.SetUshort(joinMap.MeetingCount.JoinNumber, (ushort)_currentMeetings.Count);

			if (_currentMeetings.Count == 0)
			{
				trilist.SetBool(joinMap.CodecAvailable.JoinNumber, true);
				trilist.SetBool(joinMap.CodecMeetingBannerActive.JoinNumber, false);
				trilist.SetBool(joinMap.CodecMeetingBannerWarning.JoinNumber, false);
				trilist.SetString(
					joinMap.AvailableTimeRemaining.JoinNumber,
					boilerplate1 + "the rest of the day."
				);
				trilist.SetString(joinMap.TimeToNextMeeting.JoinNumber, "No Meetings Scheduled");
				trilist.SetString(
					joinMap.ActiveMeetingDataXSig.JoinNumber,
					UpdateActiveMeetingXSig(null)
				);
				trilist.SetUshort(joinMap.TotalMinutesUntilMeeting.JoinNumber, 0);
				trilist.SetUshort(joinMap.HoursUntilMeeting.JoinNumber, 0);
				trilist.SetUshort(joinMap.MinutesUntilMeeting.JoinNumber, 0);
				return;
			}

			var upcomingMeeting = _currentMeetings.FirstOrDefault(x =>
				x.StartTime >= currentTime && x.EndTime >= currentTime
			);
			var currentMeeting = _currentMeetings.FirstOrDefault(x =>
				x.StartTime - x.MeetingWarningMinutes <= currentTime && x.EndTime >= currentTime
			);
			var warningBanner =
				upcomingMeeting != null
				&& upcomingMeeting.StartTime - currentTime <= upcomingMeeting.MeetingWarningMinutes;

			_currentMeeting = currentMeeting;
			trilist.SetBool(joinMap.CodecAvailable.JoinNumber, currentMeeting == null);
			trilist.SetBool(
				joinMap.CodecMeetingBannerActive.JoinNumber,
				currentMeeting != null && currentMeeting.Joinable
			);
			trilist.SetBool(joinMap.CodecMeetingBannerWarning.JoinNumber, warningBanner);
			var availabilityMessage = string.Empty;
			var nextMeetingMessage = string.Empty;

			double totalMinutesRemainingAvailable = 0;
			var hoursRemainingAvailable = 0;
			var minutesRemainingAvailable = 0;

			if (upcomingMeeting != null)
			{
				this.LogDebug("Upcoming Meeting StartTime = {0}", upcomingMeeting.StartTime.ToString());
				this.LogDebug("Upcoming Meeting EndTime = {0}", upcomingMeeting.EndTime.ToString());
				var timeRemainingAvailable = upcomingMeeting.StartTime - currentTime;
				hoursRemainingAvailable = timeRemainingAvailable.Hours;
				minutesRemainingAvailable = timeRemainingAvailable.Minutes;
				totalMinutesRemainingAvailable = timeRemainingAvailable.TotalMinutes;
				var hoursPlural = hoursRemainingAvailable > 1;
				var hoursPresent = hoursRemainingAvailable > 0;
				var minutesPlural = minutesRemainingAvailable > 1;
				var minutesPresent = minutesRemainingAvailable > 0;
				var hourString = string.Format(
					"{0} {1}",
					hoursRemainingAvailable,
					hoursPlural ? "hours" : "hour"
				);
				var minuteString = string.Format(
					"{0}{1} {2}",
					hoursPresent ? " and " : string.Empty,
					minutesRemainingAvailable,
					minutesPlural ? "minutes" : "minute"
				);
				var messageBase = string.Format(
					"{0}{1}",
					hoursPresent ? hourString : string.Empty,
					minutesPresent ? minuteString : string.Empty
				);

				if (totalMinutesRemainingAvailable > 0)
				{
					availabilityMessage = string.Format("{0}{1}.", boilerplate1, messageBase);
					nextMeetingMessage = string.Format("{0}{1}.", boilerplate2, messageBase);
				}
				else
				{
					availabilityMessage = "Unavailable";
					nextMeetingMessage = "Next meeting starts soon.";
				}
			}

			trilist.SetString(
				joinMap.ActiveMeetingDataXSig.JoinNumber,
				UpdateActiveMeetingXSig(currentMeeting)
			);
			trilist.SetUshort(
				joinMap.TotalMinutesUntilMeeting.JoinNumber,
				(ushort)(totalMinutesRemainingAvailable > 0 ? totalMinutesRemainingAvailable : 0)
			);
			trilist.SetUshort(
				joinMap.HoursUntilMeeting.JoinNumber,
				(ushort)hoursRemainingAvailable
			);
			trilist.SetUshort(
				joinMap.MinutesUntilMeeting.JoinNumber,
				(ushort)minutesRemainingAvailable
			);
			trilist.SetString(joinMap.AvailableTimeRemaining.JoinNumber, availabilityMessage);
			trilist.SetString(joinMap.TimeToNextMeeting.JoinNumber, nextMeetingMessage);
		}

		public void SetPresentationLocalOnly()
		{
			PresentationStates = eCodecPresentationStates.LocalOnly;
			if (_presentationActive)
				StartSharing();
		}

		public void SetPresentationLocalRemote()
		{
			PresentationStates = eCodecPresentationStates.LocalRemote;
			if (_presentationActive)
				StartSharing();
		}

		public void SetPresentationLocalRemoteToggle()
		{
			if (PresentationStates == eCodecPresentationStates.LocalRemote)
			{
				SetPresentationLocalOnly();
				return;
			}
			SetPresentationLocalRemote();
		}

		public void Reboot()
		{
			EnqueueCommand("xCommand SystemUnit Boot Action: Restart");
		}

		private void SetSelfViewMode()
		{
			if (!IsInCall)
			{
				SelfViewModeOff();
			}
			else
			{
				if (ShowSelfViewByDefault)
					SelfViewModeOn();
				else
					SelfViewModeOff();
			}
		}

		private void OnAvailableLayoutsChanged(List<CodecCommandWithLabel> availableLayouts)
		{
			if (availableLayouts == null)
				return;
			var handler = AvailableLayoutsChanged;
			if (handler == null)
				return;
			handler(
				this,
				new AvailableLayoutsChangedEventArgs() { AvailableLayouts = availableLayouts }
			);
		}

		private void OnCameraTrackingCapabilitiesChanged()
		{
			var handler = CameraTrackingCapabilitiesChanged;
			if (handler == null)
				return;
			handler(
				this,
				new CameraTrackingCapabilitiesArgs(
					SpeakerTrackAvailableFeedbackFunc,
					PresenterTrackAvailableFeedbackFunc
				)
			);
		}

		private void OnCurrentLayoutChanged(string currentLayout)
		{
			if (string.IsNullOrEmpty(currentLayout))
				return;
			var handler = CurrentLayoutChanged;
			if (handler == null)
				return;
			handler(this, new CurrentLayoutChangedEventArgs() { CurrentLayout = currentLayout });
		}

		public void SelfViewModeOn()
		{
			EnqueueCommand("xCommand Video Selfview Set Mode: On");
		}

		public void SelfViewModeOnForMonitor(string monitorRole, bool fullScreen)
		{
			EnqueueCommand($"xCommand Video Selfview Set Mode: On OnMonitorRole: {monitorRole} FullscreenMode: {(fullScreen ? "On" : "Off")}");
		}

		public void SelfViewModeOff()
		{
			var monitorRole = _config.SelfViewDefaultMonitorRole != null ?
				$" MonitorRole: {_config.SelfViewDefaultMonitorRole}" : string.Empty;
			EnqueueCommand($"xCommand Video Selfview Set Mode: Off{monitorRole}");
		}

		public void SelfViewModeToggle()
		{
			var mode = string.Empty;

			mode = CodecStatus.Status.Video.Selfview.SelfViewMode.BoolValue ? "Off" : "On";

			EnqueueCommand(string.Format("xCommand Video Selfview Set Mode: {0}", mode));
		}

		public void SelfviewPipPositionSet(CodecCommandWithLabel position)
		{
			EnqueueCommand(
				string.Format(
					"xCommand Video Selfview Set Mode: On PIPPosition: {0}",
					position.Command
				)
			);
		}

		public void SelfviewPipPositionToggle()
		{
			if (_currentSelfviewPipPosition != null)
			{
				var nextPipPositionIndex =
					SelfviewPipPositions.IndexOf(_currentSelfviewPipPosition) + 1;

				if (nextPipPositionIndex >= SelfviewPipPositions.Count)
					// Check if we need to loop back to the first item in the list
					nextPipPositionIndex = 0;

				SelfviewPipPositionSet(SelfviewPipPositions[nextPipPositionIndex]);
			}
		}

		public void LayoutSet(CodecCommandWithLabel layout)
		{
			if (layout == null)
			{
				this.LogWarning("Unable to Recall Layout - Null CodecCommandWithLabel Object Sent");
				return;
			}
			EnqueueCommand(
				string.Format("xCommand Video Layout SetLayout LayoutName: \"{0}\"", layout.Command)
			);
			if (!EnhancedLayouts)
			{
				OnCurrentLayoutChanged(layout.Label);
			}
		}

		public void LayoutSet(string layout)
		{
			if (string.IsNullOrEmpty(layout))
			{
				this.LogWarning("Unable to Recall Layout - Null string Sent");
				return;
			}
			EnqueueCommand(
				string.Format("xCommand Video Layout SetLayout LayoutName: \"{0}\"", layout)
			);

			if (!EnhancedLayouts)
			{
				OnCurrentLayoutChanged(layout);
			}
		}

		public void LocalLayoutToggle()
		{
			if (CurrentLayout == null)
				return;
			var nextLocalLayoutIndex =
				AvailableLayouts.IndexOf(
					AvailableLayouts.FirstOrDefault(l => l.Label.Equals(CurrentLayout))
				) + 1;

			if (nextLocalLayoutIndex >= AvailableLayouts.Count)
				// Check if we need to loop back to the first item in the list
				nextLocalLayoutIndex = 0;
			if (AvailableLayouts[nextLocalLayoutIndex] == null)
				return;
			LayoutSet(AvailableLayouts[nextLocalLayoutIndex]);
		}

		public void LocalLayoutToggleSingleProminent()
		{
			if (string.IsNullOrEmpty(CurrentLayout))
				return;
			if (CurrentLayout != "Prominent")
				LayoutSet(AvailableLayouts.FirstOrDefault(l => l.Label.Equals("Prominent")));
			else
				LayoutSet(AvailableLayouts.FirstOrDefault(l => l.Label.Equals("Single")));
		}

		public void MinMaxLayoutToggle()
		{
			if (PresentationViewMaximizedFeedback.BoolValue)
			{
				PresentationViewMinimizedzedSet();
				return;
			}
			PresentationViewMaximizedSet();
		}

		public void PresentationViewDefaultSet()
		{
			_currentPresentationView = "Default";

			EnqueueCommand(
				string.Format(
					"xCommand Video PresentationView Set View: {0}",
					_currentPresentationView
				)
			);
			PresentationViewFeedbackGroup.FireUpdate();
		}

		public void PresentationViewMinimizedzedSet()
		{
			_currentPresentationView = "Minimized";

			EnqueueCommand(
				string.Format(
					"xCommand Video PresentationView Set View: {0}",
					_currentPresentationView
				)
			);
			PresentationViewFeedbackGroup.FireUpdate();
		}

		public void PresentationViewMaximizedSet()
		{
			_currentPresentationView = "Maximized";

			EnqueueCommand(
				string.Format(
					"xCommand Video PresentationView Set View: {0}",
					_currentPresentationView
				)
			);
			PresentationViewFeedbackGroup.FireUpdate();
		}

		private void ComputeSelfviewPipStatus()
		{
			_currentSelfviewPipPosition = SelfviewPipPositions.FirstOrDefault(p =>
				p.Command.ToLower()
					.Equals(CodecStatus.Status.Video.Selfview.PipPosition.Value.ToLower())
			);

			if (_currentSelfviewPipPosition != null)
				SelfviewIsOnFeedback.FireUpdate();
		}

		public void RemoveCallHistoryEntry(CodecCallHistory.CallHistoryEntry entry)
		{
			EnqueueCommand(
				string.Format(
					"xCommand CallHistory DeleteEntry CallHistoryId: {0} AcknowledgeConsecutiveDuplicates: True",
					entry.OccurrenceHistoryId
				)
			);
		}

		#region IHasCameraSpeakerTrack

		public void CameraAutoModeToggle()
		{
			if (!CameraAutoModeIsOnFeedback.BoolValue)
			{
				CameraAutoModeOn();
				return;
			}
			CameraAutoModeOff();
		}

		public void CameraAutoModeOn()
		{
			switch (CameraTrackingCapabilities)
			{
				case eCameraTrackingCapabilities.None:
					{
						this.LogInformation("Camera Auto Mode Unavailable");
						break;
					}
				case eCameraTrackingCapabilities.PresenterTrack:
					{
						PresenterTrackFollow();
						break;
					}
				case eCameraTrackingCapabilities.SpeakerTrack:
					{
						SpeakerTrackOn();
						break;
					}
				case eCameraTrackingCapabilities.Both:
					{
						if (PreferredTrackingMode == eCameraTrackingCapabilities.SpeakerTrack)
						{
							SpeakerTrackOn();
							break;
						}
						PresenterTrackFollow();
						break;
					}
			}
		}

		public void CameraAutoModeOff()
		{
			switch (CameraTrackingCapabilities)
			{
				case eCameraTrackingCapabilities.None:
					{
						this.LogInformation("Camera Auto Mode Unavailable");
						break;
					}
				case eCameraTrackingCapabilities.PresenterTrack:
					{
						PresenterTrackOff();
						break;
					}
				case eCameraTrackingCapabilities.SpeakerTrack:
					{
						SpeakerTrackOff();
						break;
					}
				case eCameraTrackingCapabilities.Both:
					{
						if (PreferredTrackingMode == eCameraTrackingCapabilities.SpeakerTrack)
						{
							SpeakerTrackOff();
							break;
						}
						PresenterTrackOff();
						break;
					}
			}
		}

		public void SpeakerTrackOn()
		{
			if (CameraIsOffFeedback.BoolValue)
			{
				CameraMuteOff();
			}

			EnqueueCommand("xCommand Cameras SpeakerTrack Activate");
		}

		public void SpeakerTrackOff()
		{
			if (CameraIsOffFeedback.BoolValue)
			{
				CameraMuteOff();
			}

			EnqueueCommand("xCommand Cameras SpeakerTrack Deactivate");
		}

		#endregion

		public void PresenterTrackOff()
		{
			if (!PresenterTrackAvailability)
			{
				this.LogInformation("Presenter Track is Unavailable on this Codec");
				return;
			}
			if (CameraIsOffFeedback.BoolValue)
			{
				CameraMuteOff();
			}

			EnqueueCommand("xCommand Cameras PresenterTrack Set Mode: Off");
		}

		public void PresenterTrackFollow()
		{
			if (!PresenterTrackAvailability)
			{
				this.LogInformation("Presenter Track is Unavailable on this Codec");
				return;
			}
			if (CameraIsOffFeedback.BoolValue)
			{
				CameraMuteOff();
			}

			EnqueueCommand("xCommand Cameras PresenterTrack Set Mode: Follow");
		}

		public void PresenterTrackBackground()
		{
			if (!PresenterTrackAvailability)
			{
				this.LogInformation("Presenter Track is Unavailable on this Codec");
				return;
			}

			if (CameraIsOffFeedback.BoolValue)
			{
				CameraMuteOff();
			}

			EnqueueCommand("xCommand Cameras PresenterTrack Set Mode: Background");
		}

		public void PresenterTrackPersistent()
		{
			if (!PresenterTrackAvailability)
			{
				this.LogInformation("Presenter Track is Unavailable on this Codec");
				return;
			}
			if (CameraIsOffFeedback.BoolValue)
			{
				CameraMuteOff();
			}

			EnqueueCommand("xCommand Cameras PresenterTrack Set Mode: Persistent");
		}

		private void SetUpCameras(List<CameraInfo> cameraInfo)
		{
			// Add the internal camera
			Cameras = new List<CameraBase>();

			var camCount = cameraInfo.Count;
			this.LogDebug("Setting up cameras from info: {info}",
				JsonConvert.SerializeObject(cameraInfo, Formatting.Indented));

			// Deal with the case of 1 or no reported cameras
			if (camCount <= 1)
			{
				var internalCamera = new CiscoCamera(Key + "-camera1", "Near End", this, 1);

				if (camCount > 0)
				{
					// Try to get the capabilities from the codec
					if (
						CodecStatus.Status.Cameras.CameraList[0] != null
						&& CodecStatus.Status.Cameras.CameraList[0].Capabilities != null
					)
					{
						internalCamera.SetCapabilites(
							CodecStatus.Status.Cameras.CameraList[0].Capabilities.Options.Value
						);
					}
				}

				Cameras.Add(internalCamera);
				this.LogDebug("Adding camera to camera list: {key}", internalCamera.Key);

				var existingInternalCamera = DeviceManager.GetDeviceForKey(internalCamera.Key);

				if (existingInternalCamera == null)
				{
					DeviceManager.AddDevice(internalCamera);
				}
			}
			else
			{
				if (CodecStatus.Status.Cameras.CameraList == null)
					return;
				foreach (var item in CodecStatus.Status.Cameras.CameraList)
				{
					var cam = item;
					if (cam.Connected.Value.ToLower() == "false")
					{
						this.LogDebug("Camera {id} is Disconnected", cam.CameraId);
						continue;
					}
					this.LogDebug("Camera {id} is Connected", cam.CameraId);

					var camId = uint.Parse(item.CameraId);
					var camInfo = cameraInfo.FirstOrDefault(c => c.CameraNumber == camId);
					var name = string.Format("Camera {0}", camId);
					var sourceId =
						(camInfo != null && camInfo.SourceId > 0) ? (uint)camInfo.SourceId : camId;
					if (camInfo != null)
					{
						name = camInfo.Name;
					}

					var existingCameras = DeviceManager.AllDevices.OfType<CiscoCamera>();

					var existingCamera = existingCameras.FirstOrDefault(c => c.SerialNumber == item.SerialNumber.Value);

					if (existingCamera != null)
					{
						existingCamera.SetParentCodec(this);
						if (uint.TryParse(item.CameraId, out var id))
						{
							existingCamera.SetCameraId(Convert.ToUInt16(id));
						}
						Cameras.Add(existingCamera);
						continue;
					}

					var key = string.Format("{0}-camera{1}", Key, camId);
					var camera = new CiscoCamera(key, name, this, camId, sourceId);

					if (cam.Capabilities != null)
					{
						camera.SetCapabilites(cam.Capabilities.Options.Value);
					}

					this.LogDebug("Adding Camera {id}", camera.CameraId);
					Cameras.Add(camera);

					if (existingCamera == null)
					{
						DeviceManager.AddDevice(camera);
					}
				}
			}

			// Add the far end camera
			var farEndCamera = new CiscoFarEndCamera(Key + "-cameraFar", "Far End", this);
			this.LogDebug("Adding camera to camera list: {key}", farEndCamera.Key);

			Cameras.Add(farEndCamera);

			var existingFarEndCamera = DeviceManager.GetDeviceForKey(farEndCamera.Key);

			if (existingFarEndCamera == null)
			{
				DeviceManager.AddDevice(farEndCamera);
			}

			SelectedCameraFeedback = new StringFeedback(() => SelectedCamera.Key);

			ControllingFarEndCameraFeedback = new BoolFeedback(
				() => SelectedCamera is IAmFarEndCamera
			);

			NearEndPresets = new List<CodecRoomPreset>(15);

			FarEndRoomPresets = new List<CodecRoomPreset>(15);

			// Add the far end presets
			for (var i = 1; i <= FarEndRoomPresets.Capacity; i++)
			{
				var label = string.Format("Far End Preset {0}", i);
				FarEndRoomPresets.Add(new CodecRoomPreset(i, label, true, false));
			}

			SelectedCamera = Cameras.First();
			SelectCamera(SelectedCamera.Key); // call the method to select the camera and ensure the feedbacks get updated.
		}

		private void SetUpCamerasFromConfig(List<CameraInfo> cameraInfo)
		{
			if (cameraInfo == null)
				throw new ArgumentNullException("cameraInfo");

			// Add the internal camera
			Cameras = new List<CameraBase>();

			var camCount = cameraInfo.Count;
			this.LogDebug("THERE ARE {count} CAMERAS", camCount);

			if (camCount == 0)
			{
				var internalCamera = new CiscoCamera(Key + "-camera1", "Near End", this, 1);

				if (camCount > 0 && CodecStatus.Status.Cameras.CameraList.Count > 0)
				{
					// Try to get the capabilities from the codec
					if (
						CodecStatus.Status.Cameras.CameraList[0] != null
						&& CodecStatus.Status.Cameras.CameraList[0].Capabilities != null
					)
					{
						internalCamera.SetCapabilites(
							CodecStatus.Status.Cameras.CameraList[0].Capabilities.Options.Value
						);
					}
				}

				Cameras.Add(internalCamera);
				//DeviceManager.AddDevice(internalCamera);
			}
			else
			{
				foreach (var item in cameraInfo)
				{
					var cam = item;
					var sourceId = (cam.SourceId > 0) ? (uint)cam.SourceId : (uint)cam.CameraNumber;
					var key = string.Format("{0}-camera{1}", Key, cam.CameraNumber);
					var camera = new CiscoCamera(
						key,
						cam.Name ?? string.Empty,
						this,
						(uint)cam.CameraNumber,
						sourceId
					);
					this.LogDebug("Adding Camera {id}", camera.CameraId);
					Cameras.Add(camera);
				}
			}

			// Add the far end camera
			var farEndCamera = new CiscoFarEndCamera(Key + "-cameraFar", "Far End", this);
			Cameras.Add(farEndCamera);

			SelectedCameraFeedback = new StringFeedback(() => SelectedCamera.Key);

			ControllingFarEndCameraFeedback = new BoolFeedback(
				() => SelectedCamera is IAmFarEndCamera
			);

			NearEndPresets = new List<CodecRoomPreset>(15);

			FarEndRoomPresets = new List<CodecRoomPreset>(15);

			// Add the far end presets
			for (var i = 1; i <= FarEndRoomPresets.Capacity; i++)
			{
				var label = string.Format("Far End Preset {0}", i);
				FarEndRoomPresets.Add(new CodecRoomPreset(i, label, true, false));
			}

			SelectedCamera = Cameras.First();
			SelectCamera(SelectedCamera.Key); // call the method to select the camera and ensure the feedbacks get updated.
		}

		#region ICiscoCodecCameraConfig Members

		public void SetCameraAssignedSerialNumber(uint cameraId, string serialNumber)
		{
			this.LogDebug("Setting the serial number of camera {id} to {serialNumber}", cameraId, serialNumber);
			EnqueueCommand($"xConfiguration Cameras Camera[{cameraId}] AssignedSerialNumber: {serialNumber}");
		}

		public void SetCameraName(uint videoConnectorId, string name)
		{
			this.LogDebug("Setting the name of video connector {id} to {name}", videoConnectorId, name);
			EnqueueCommand($"xConfiguration Video Input Connector[{videoConnectorId}]  Name: \"{name}\"");
		}

		public void SetInputCameraId(uint videoConnectorId, uint inputCameraId)
		{
			this.LogDebug("Setting the camera id of video connector {id} to {inputCameraId}", videoConnectorId, inputCameraId);
			EnqueueCommand($"xConfiguration Video Input Connector[{videoConnectorId}] CameraControl CameraId: {inputCameraId}");
		}

		public void SetInputSourceType(uint videoConnectorId, eCiscoCodecInputSourceType sourceType)
		{
			this.LogDebug("Setting the source type of video connector {id} to {sourceType}", videoConnectorId, sourceType);
			EnqueueCommand($"xConfiguration Video Input Connector[{videoConnectorId}]  InputSourceType: {sourceType}");
		}

		#endregion

		#region IHasCodecCameras Members

		public event EventHandler<CameraSelectedEventArgs> CameraSelected;

		public List<CameraBase> Cameras { get; private set; }

		public StringFeedback SelectedCameraFeedback { get; private set; }

		private CameraBase _selectedCamera;

		public CameraBase SelectedCamera
		{
			get { return _selectedCamera; }
			private set
			{
				_selectedCamera = value;
				SelectedCameraFeedback.FireUpdate();
				ControllingFarEndCameraFeedback.FireUpdate();
				if (CameraIsOffFeedback.BoolValue)
					CameraMuteOff();
				CameraSelected?.Invoke(this, new CameraSelectedEventArgs(SelectedCamera));
			}
		}

		public void SelectCamera(string key)
		{
			var camera = Cameras.FirstOrDefault(c =>
				c.Key.IndexOf(key, StringComparison.OrdinalIgnoreCase) > -1
			);
			if (camera != null)
			{
				this.LogDebug("Selected Camera with key: '{key}'", camera.Key);
				SelectedCamera = camera;
			}
			else
				this.LogDebug("Unable to select camera with key: '{key}'", key);

			if (camera is CiscoCamera ciscoCam)
			{
				EnqueueCommand(
					string.Format(
						"xCommand Video Input SetMainVideoSource SourceId: {0}",
						ciscoCam.SourceId
					)
				);
			}
		}

		public CameraBase FarEndCamera { get; private set; }

		public BoolFeedback ControllingFarEndCameraFeedback { get; private set; }

		#endregion

		#region IHasCameraPresets Members

		public event EventHandler<EventArgs> CodecRoomPresetsListHasChanged;

		public List<CodecRoomPreset> NearEndPresets { get; private set; }

		public List<CodecRoomPreset> FarEndRoomPresets { get; private set; }

		public void CodecRoomPresetSelect(int preset)
		{
			Debug.LogInformation(
				this,
				"Selecting Preset: {0}",
				preset
			);
			if (SelectedCamera is IAmFarEndCamera)
				SelectFarEndPreset(preset);
			else
			{
				_selectedPreset = preset;
				CiscoRoomPresetRecall();

			}
		}

		public void CiscoRoomPresetRecall()
		{
			if (_selectedPreset < 1)
				return;
			EnqueueCommand(
				string.Format("xCommand RoomPreset Activate PresetId: {0}", _selectedPreset)
			);
			_selectedPreset = 0;
		}

		public void CodecRoomPresetStore(int preset, string description)
		{
			if (_selectedPreset < 1)
				return;
			EnqueueCommand(
				string.Format(
					"xCommand RoomPreset Store PresetId: {0} Description: \"{1}\" Type: All",
					preset,
					description
				)
			);
			_selectedPreset = 0;
		}

		#endregion

		public void SelectFarEndPreset(int preset)
		{
			EnqueueCommand(
				string.Format(
					"xCommand Call FarEndControl RoomPreset Activate CallId: {0} PresetId: {1}",
					GetCallId(),
					preset
				)
			);
		}

		#region IHasExternalSourceSwitching Members

		public bool ExternalSourceListEnabled { get; private set; }

		public string ExternalSourceInputPort { get; private set; }

		public bool BrandingEnabled { get; private set; }
		private string _brandingUrl;
		private bool _sendMcUrl;

		public void AddExternalSource(
			string connectorId,
			string key,
			string name,
			Devices.Common.VideoCodec.Cisco.eExternalSourceType type
		)
		{
			int id;
			if (string.Equals(connectorId, "hdmiin1", StringComparison.OrdinalIgnoreCase))
			{
				id = 1;
			}
			else if (string.Equals(connectorId, "hdmiin2", StringComparison.OrdinalIgnoreCase))
			{
				id = 2;
			}
			else if (string.Equals(connectorId, "hdmiin3", StringComparison.OrdinalIgnoreCase))
			{
				id = 3;
			}
			else if (string.Equals(connectorId, "hdmiin4", StringComparison.OrdinalIgnoreCase))
			{
				id = 4;
			}
			else if (string.Equals(connectorId, "hdmiin5", StringComparison.OrdinalIgnoreCase))
			{
				id = 5;
			}
			else
			{
				id = 2;
			}
			EnqueueCommand(
				string.Format(
					"xCommand UserInterface Presentation ExternalSource Add ConnectorId: {0} SourceIdentifier: \"{1}\" Name: \"{2}\" Type: {3}",
					id,
					key,
					name,
					type.ToString()
				)
			);
			// SendText(string.Format("xCommand UserInterface Presentation ExternalSource State Set SourceIdentifier: \"{0}\" State: Ready", key));
			this.LogVerbose("Adding ExternalSource {connectorId} {name}", connectorId, name);
		}

		public void SetExternalSourceState(
			string key,
			Devices.Common.VideoCodec.Cisco.eExternalSourceMode mode
		)
		{
			EnqueueCommand(
				string.Format(
					"xCommand UserInterface Presentation ExternalSource State Set SourceIdentifier: \"{0}\" State: {1}",
					key,
					mode.ToString()
				)
			);
		}

		public void ClearExternalSources()
		{
			EnqueueCommand("xCommand UserInterface Presentation ExternalSource RemoveAll");
		}

		public void SetSelectedSource(string key)
		{
			EnqueueCommand(
				string.Format(
					"xCommand UserInterface Presentation ExternalSource Select SourceIdentifier: {0}",
					key
				)
			);
			_externalSourceChangeRequested = true;
		}

		public Action<string, string> RunRouteAction { private get; set; }

		#endregion

		#region ExternalDevices



		#endregion

		#region IHasCameraOff Members

		public BoolFeedback CameraIsOffFeedback { get; private set; }

		public void CameraOff()
		{
			CameraMuteOn();
		}

		#endregion

		public BoolFeedback CameraIsMutedFeedback { get; private set; }

		public void CameraMuteOn()
		{
			EnqueueCommand("xCommand Video Input MainVideo Mute");
		}

		public void CameraMuteOff()
		{
			EnqueueCommand("xCommand Video Input MainVideo Unmute");
		}

		public void CameraMuteToggle()
		{
			if (CameraIsMutedFeedback.BoolValue)
				CameraMuteOff();
			else
				CameraMuteOn();
		}

		#region IHasDoNotDisturbMode Members


		#endregion

		#region IHasHalfWakeMode Members

		public BoolFeedback HalfWakeModeIsOnFeedback { get; private set; }

		public BoolFeedback EnteringStandbyModeFeedback { get; private set; }

		public void HalfwakeActivate()
		{
			EnqueueCommand("xCommand Standby Halfwake");
		}

		#endregion


		public DeviceInfo DeviceInfo { get; private set; }

		public event DeviceInfoChangeHandler DeviceInfoChanged;
		public event SourceInfoChangeHandler CurrentSourceChange;

		public void UpdateDeviceInfo()
		{
			var args = new DeviceInfoEventArgs(DeviceInfo);

			var raiseEvent = DeviceInfoChanged;

			if (raiseEvent == null)
				return;
			raiseEvent(this, args);
		}

		public void OnCodecInfoChanged(CodecInfoChangedEventArgs args)
		{
			var handler = CodecInfoChanged;
			if (handler == null)
				return;
			handler(this, args);
		}

		public void ActivateDoNotDisturbMode()
		{
			DoNotDisturbHandler.ActivateDoNotDisturbMode();
		}

		public void DeactivateDoNotDisturbMode()
		{
			DoNotDisturbHandler.DeactivateDoNotDisturbMode();
		}

		public void ToggleDoNotDisturbMode()
		{
			DoNotDisturbHandler.ToggleDoNotDisturbMode();
		}

		public BoolFeedback DoNotDisturbModeIsOnFeedback
		{
			get { return DoNotDisturbHandler.DoNotDisturbModeIsOnFeedback; }
		}

		public StringFeedback WidgetEventFeedback
		{
			get { return UIExtensionsHandler.WidgetEventFeedback; }
		}

		#region IHasPhoneDialing Members

		public StringFeedback CallerIdNameFeedback { get; private set; }

		public StringFeedback CallerIdNumberFeedback { get; private set; }

		public void DialPhoneCall(string number)
		{
			EnqueueCommand(string.Format("xCommand Dial Number: \"{0}\" CallType: Audio", number));
		}

		public void EndPhoneCall()
		{
			var phoneCall = ActiveCalls.FirstOrDefault(o => o.Type == eCodecCallType.Audio);
			if (phoneCall != null)
				EndCall(phoneCall);
		}

		public BoolFeedback PhoneOffHookFeedback { get; private set; }
		/// <summary>
		/// Gets or sets the CurrentSourceInfoKey
		/// </summary>
		public string CurrentSourceInfoKey { get; set; }

		/// <summary>
		/// Gets or sets the current source information for the display.
		/// </summary>
		public SourceListItem CurrentSourceInfo
		{
			get
			{
				return currentSourceInfo;
			}
			set
			{
				if (value == currentSourceInfo) return;

				var handler = CurrentSourceChange;

				handler?.Invoke(currentSourceInfo, ChangeType.WillChange);

				currentSourceInfo = value;

				handler?.Invoke(currentSourceInfo, ChangeType.DidChange);
			}
		}
		private SourceListItem currentSourceInfo;
		public void SendDtmfToPhone(string digit)
		{
			var phoneCall = ActiveCalls.FirstOrDefault(o => o.Type == eCodecCallType.Audio);
			if (phoneCall != null)
				SendDtmf(digit, phoneCall);
		}

		#endregion

		public void ShowWebView(string url, string mode, string title, string target)
		{
			// target is OSD. Doesn't need any special handling;
			if (target == "OSD")
			{
				var uriSuccess = Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri);

				if (!uri.IsAbsoluteUri)
				{
					this.LogError("Invalid URL provided for WebView: {url}. Must be absolute URL, IE https://roomos.cisco.com", url);
					return;
				}
				var uwvd = new WebViewDisplay { Url = url, Mode = mode, Title = title, Target = target };
				EnqueueCommand(uwvd.xCommand());

				return;
			}

			var navigator = DeviceManager.AllDevices.OfType<NavigatorController>().Where(n => n.Parent.Key == Key).FirstOrDefault();

			if (navigator == null)
			{
				this.LogError("No NavigatorController found for key: {key}", Key);
				return;
			}

			var config = new WebViewDisplayConfig
			{
				Url = url,
				Mode = mode,
				Title = title,
				Target = target
			};

			navigator.ShowWebViewOsd(url, config);
		}

		public void HideWebView()
		{
			EnqueueCommand($"xCommand UserInterface WebView Clear Target:OSD{CiscoCodec.Delimiter}");
		}
		public void ShowEmergencyMessage(string url)
		{
			string mode = _config.Emergency.UiWebViewDisplay.Mode;
			string title = _config.Emergency.UiWebViewDisplay.Title;
			string target = _config.Emergency.UiWebViewDisplay.Target;
			string urlPath = url + _config.Emergency.MobileControlPath;
			WebViewDisplay uwvd = new WebViewDisplay { Url = urlPath, Mode = mode, Title = title, Target = target };
			EnqueueCommand(uwvd.xCommand());
		}

		public void HideEmergencyMessage()
		{
			EnqueueCommand($"xCommand UserInterface WebView Clear Target:OSD{CiscoCodec.Delimiter}");
		}
	}
}
