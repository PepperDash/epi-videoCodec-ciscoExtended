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
	public partial class CiscoCodec
	{
		/// <summary>
		/// Call history functionality
		/// </summary>
		public CodecCallHistory CallHistory { get; private set; }

		/// <summary>
		/// Gets the call history from the codec
		/// </summary>
		public void GetCallHistory()
		{
			EnqueueCommand("xCommand CallHistory Recents Limit: 20 Order: OccurrenceTime");
		}

		/// <summary>
		/// Removes a call history entry
		/// </summary>
		/// <param name="entry">The call history entry to remove</param>
		public void RemoveCallHistoryEntry(CodecCallHistory.CallHistoryEntry entry)
		{
			EnqueueCommand(
				string.Format(
					"xCommand CallHistory DeleteEntry CallHistoryId: {0} AcknowledgeConsecutiveDuplicates: True",
					entry.OccurrenceHistoryId
				)
			);
		}

		/// <summary>
		/// Parses call history response from the codec
		/// </summary>
		/// <param name="callHistoryResponseToken">The JSON token containing call history data</param>
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
	}
}