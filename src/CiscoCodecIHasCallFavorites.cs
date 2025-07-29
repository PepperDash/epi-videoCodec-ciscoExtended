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
		/// Call favorites functionality
		/// </summary>
		public CodecCallFavorites CallFavorites { get; private set; }
	}
}