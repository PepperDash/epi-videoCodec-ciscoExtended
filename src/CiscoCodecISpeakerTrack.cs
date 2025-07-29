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
	/// Partial class implementation for ISpeakerTrack
	/// </summary>
	public partial class CiscoCodec
	{
		/// <summary>
		/// Gets whether SpeakerTrack is available
		/// </summary>
		public bool SpeakerTrackAvailability { get; private set; }

		/// <summary>
		/// Gets the current SpeakerTrack status
		/// </summary>
		public bool SpeakerTrackStatus { get; private set; }

		/// <summary>
		/// Feedback for SpeakerTrack status being on
		/// </summary>
		public BoolFeedback SpeakerTrackStatusOnFeedback { get; private set; }

		/// <summary>
		/// Feedback for SpeakerTrack availability
		/// </summary>
		public BoolFeedback SpeakerTrackAvailableFeedback { get; private set; }

		/// <summary>
		/// Function to provide SpeakerTrack availability feedback
		/// </summary>
		protected Func<bool> SpeakerTrackAvailableFeedbackFunc
		{
			get { return () => SpeakerTrackAvailability; }
		}

		/// <summary>
		/// Function to provide SpeakerTrack status on feedback
		/// </summary>
		protected Func<bool> SpeakerTrackStatusOnFeedbackFunc
		{
			get { return () => SpeakerTrackStatus; }
		}

		/// <summary>
		/// Polls the SpeakerTrack status from the codec
		/// </summary>
		public void PollSpeakerTrack()
		{
			EnqueueCommand("xStatus Cameras SpeakerTrack");
		}

		/// <summary>
		/// Activates SpeakerTrack
		/// </summary>
		public void SpeakerTrackOn()
		{
			if (CameraIsOffFeedback.BoolValue)
			{
				CameraMuteOff();
			}

			EnqueueCommand("xCommand Cameras SpeakerTrack Activate");
		}

		/// <summary>
		/// Deactivates SpeakerTrack
		/// </summary>
		public void SpeakerTrackOff()
		{
			if (CameraIsOffFeedback.BoolValue)
			{
				CameraMuteOff();
			}

			EnqueueCommand("xCommand Cameras SpeakerTrack Deactivate");
		}

		/// <summary>
		/// Parses SpeakerTrack status from the codec response
		/// </summary>
		/// <param name="speakerTrackToken">The JSON token containing SpeakerTrack data</param>
		private void ParseSpeakerTrackToken(JToken speakerTrackToken)
		{
			try
			{
				if (String.IsNullOrEmpty(speakerTrackToken.ToString()))
					return;
				var speakerTrackObject = speakerTrackToken as JObject;
				if (speakerTrackObject == null)
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
				Debug.Console(0, this, "Exception in ParseSpeakerTrackToken : ");
				Debug.Console(0, this, "{0}", e.Message);
			}
		}
	}
}