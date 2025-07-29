using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Interfaces;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Core.Intersystem;
using PepperDash.Core.Intersystem.Tokens;
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
using Serilog.Events;
using Feedback = PepperDash.Essentials.Core.Feedback;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceWebViewDisplay;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
	/// <summary>
	/// Partial class implementation for IHasCameraAutoMode
	/// </summary>
	public partial class CiscoCodec
	{
		#region IHasCameraAutoMode Implementation

		public BoolFeedback CameraAutoModeIsOnFeedback { get; private set; }
		public BoolFeedback CameraAutoModeAvailableFeedback { get; private set; }

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

		/// <summary>
		/// Initializes CameraAutoMode feedbacks. Called from main constructor.
		/// </summary>
		private void InitializeCameraAutoModeFeedbacks()
		{
			CameraAutoModeIsOnFeedback = new BoolFeedback(CameraTrackingOnFeedbackFunc);
			CameraAutoModeAvailableFeedback = new BoolFeedback(CameraTrackingAvailableFeedbackFunc);
		}

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
						Debug.Console(0, this, "Camera Auto Mode Unavailable");
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
						Debug.Console(0, this, "Camera Auto Mode Unavailable");
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

		#endregion
	}
}