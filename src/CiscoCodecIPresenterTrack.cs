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
	/// Partial class implementation for IPresenterTrack
	/// </summary>
	public partial class CiscoCodec
	{
		#region IPresenterTrack Implementation

		public bool PresenterTrackAvailability { get; private set; }
		public bool PresenterTrackStatus { get; private set; }
		
		public string PresenterTrackStatusName { get; private set; }

		public BoolFeedback PresenterTrackStatusOnFeedback { get; private set; }
		
		public StringFeedback PresenterTrackStatusNameFeedback { get; private set; }
		public BoolFeedback PresenterTrackStatusOffFeedback { get; private set; }
		public BoolFeedback PresenterTrackStatusFollowFeedback { get; private set; }
		public BoolFeedback PresenterTrackStatusBackgroundFeedback { get; private set; }
		public BoolFeedback PresenterTrackStatusPersistentFeedback { get; private set; }

		public BoolFeedback PresenterTrackAvailableFeedback { get; private set; }

		public FeedbackGroup PresenterTrackFeedbackGroup { get; private set; }

		protected Func<bool> PresenterTrackAvailableFeedbackFunc
		{
			get { return () => PresenterTrackAvailability; }
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
					((PresenterTrackStatus) || (String.IsNullOrEmpty(PresenterTrackStatusName)));
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

		/// <summary>
		/// Initializes PresenterTrack feedbacks. Called from main constructor.
		/// </summary>
		private void InitializePresenterTrackFeedbacks()
		{
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

			PresenterTrackAvailableFeedback = new BoolFeedback(PresenterTrackAvailableFeedbackFunc);

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
		}

		public void PollPresenterTrack()
		{
			EnqueueCommand("xStatus Cameras PresenterTrack");
		}

		public void PresenterTrackOff()
		{
			if (!PresenterTrackAvailability)
			{
				Debug.Console(0, this, "Presenter Track is Unavailable on this Codec");
				return;
			}
			if (CameraIsOffFeedback.BoolValue)
			{
				CameraMuteOff();
			}

			Debug.Console(1, this, "PresenterTrackOff");

			EnqueueCommand("xCommand Cameras PresenterTrack Set Mode: Off");
		}

		public void PresenterTrackFollow()
		{
			if (!PresenterTrackAvailability)
			{
                Debug.Console(0, this, "Presenter Track is Unavailable on this Codec");
                return;
            }
			if (CameraIsOffFeedback.BoolValue)
			{
				CameraMuteOff();
			}

			Debug.Console(1, this, "PresenterTrackFollow");

			EnqueueCommand("xCommand Cameras PresenterTrack Set Mode: Follow");
		}

		public void PresenterTrackBackground()
		{
			if (!PresenterTrackAvailability)
			{
				Debug.Console(0, this, "Presenter Track is Unavailable on this Codec");
				return;
			}
			if (CameraIsOffFeedback.BoolValue)
			{
				CameraMuteOff();
			}

			Debug.Console(1, this, "PresenterTrackBackground");

			EnqueueCommand("xCommand Cameras PresenterTrack Set Mode: Background");
		}

		public void PresenterTrackPersistent()
		{
			if (!PresenterTrackAvailability)
			{
				Debug.Console(0, this, "Presenter Track is Unavailable on this Codec");
				return;
			}
			if (CameraIsOffFeedback.BoolValue)
			{
				CameraMuteOff();
			}

			Debug.Console(1, this, "PresenterTrackPersistent");

			EnqueueCommand("xCommand Cameras PresenterTrack Set Mode: Persistent");
		}

		private void ParsePresenterTrackToken(JToken presenterTrackToken)
		{
			try
			{
				if (String.IsNullOrEmpty(presenterTrackToken.ToString()))
					return;
				var presenterTrackObject = presenterTrackToken as JObject;
                if (presenterTrackObject == null)
                    return;
				var availabilityToken = presenterTrackObject.SelectToken("Availability.Value");
				var statusToken = presenterTrackObject.SelectToken("Status.Value");
				if (availabilityToken != null)
					PresenterTrackAvailability =
						availabilityToken.Value<string>().ToLower() == "available" ? true : false;
				if (statusToken != null)
				{
					var status = statusToken.Value<string>().ToLower();
					if (!String.IsNullOrEmpty(status))
					{
						PresenterTrackStatusName = status;
						switch (status)
						{
							case "follow":
								PresenterTrackStatus = true;
								break;
							case "background":
								PresenterTrackStatus = true;
								break;
							case "persistent":
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
			catch (Exception ex)
			{
				Debug.Console(1, "Unable to parse PresenterTrackToken. \n{0}", ex);
			}
		}

		#endregion
	}
}