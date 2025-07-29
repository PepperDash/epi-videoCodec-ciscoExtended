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
		/// Timer for branding operations
		/// </summary>
		private CTimer _brandingTimer;

		/// <summary>
		/// Branding URL for UI customization
		/// </summary>
		private string _brandingUrl;

		/// <summary>
		/// Flag to control mobile control URL sending
		/// </summary>
		private bool _sendMcUrl;

		/// <summary>
		/// Gets whether branding is enabled
		/// </summary>
		public bool BrandingEnabled { get; private set; }

		/// <summary>
		/// Initializes branding for the specified room
		/// </summary>
		/// <param name="roomKey">The room key for branding initialization</param>
		public void InitializeBranding(string roomKey)
		{
			Debug.Console(1, this, "Initializing Branding for room {0}", roomKey);

			if (!BrandingEnabled)
			{
				return;
			}

			var mcBridgeKey = String.Format("mobileControlBridge-{0}", roomKey);

#if SERIES4
			var mcBridge = DeviceManager.GetDeviceForKey(mcBridgeKey) as IMobileControlRoomMessenger;

#else
			var mcBridge = DeviceManager.GetDeviceForKey(mcBridgeKey) as IMobileControlRoomBridge;

#endif
			if (!String.IsNullOrEmpty(_brandingUrl))
			{
				Debug.Console(1, this, "Branding URL found: {0}", _brandingUrl);
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
			else if (String.IsNullOrEmpty(_brandingUrl))
			{
				Debug.Console(1, this, "No Branding URL found");
				if (mcBridge == null)
					return;

				Debug.Console(2, this, "Setting QR code URL: {0}", mcBridge.QrCodeUrl);

				mcBridge.UserCodeChanged += (o, a) => SendMcBrandingUrl(mcBridge);
				mcBridge.UserPromptedForCode += (o, a) => DisplayUserCode(mcBridge.UserCode);

				SendMcBrandingUrl(mcBridge);
			}
		}

#if SERIES4
		/// <summary>
		/// Sends mobile control branding URL
		/// </summary>
		/// <param name="roomMessenger">The mobile control room messenger</param>
		private void SendMcBrandingUrl(IMobileControlRoomMessenger roomMessenger)
#else
		/// <summary>
		/// Sends mobile control branding URL
		/// </summary>
		/// <param name="roomMessenger">The mobile control room bridge</param>
		private void SendMcBrandingUrl(IMobileControlRoomBridge roomMessenger)
#endif
		{
			if (roomMessenger == null)
			{
				return;
			}

			Debug.Console(1, this, "Sending url: {0}", roomMessenger.QrCodeUrl);

			EnqueueCommand(
				"xconfiguration userinterface custommessage: \"Scan the QR code with a mobile phone to get started\""
			);
			EnqueueCommand(
				"xconfiguration userinterface osd halfwakemessage: \"Tap the touch panel or scan the QR code with a mobile phone to get started\""
			);

			var checksum = !String.IsNullOrEmpty(roomMessenger.QrCodeChecksum)
				? String.Format("checksum: {0} ", roomMessenger.QrCodeChecksum)
				: String.Empty;

			EnqueueCommand(
				String.Format(
					"xcommand userinterface branding fetch {1}type: branding url: {0}",
					roomMessenger.QrCodeUrl,
					checksum
				)
			);
			EnqueueCommand(
				String.Format(
					"xcommand userinterface branding fetch {1}type: halfwakebranding url: {0}",
					roomMessenger.QrCodeUrl,
					checksum
				)
			);
		}

		/// <summary>
		/// Sends the branding URL to the codec
		/// </summary>
		private void SendBrandingUrl()
		{
			Debug.Console(1, this, "Sending url: {0}", _brandingUrl);

			EnqueueCommand(
				String.Format(
					"xcommand userinterface branding fetch type: branding url: {0}",
					_brandingUrl
				)
			);
			EnqueueCommand(
				String.Format(
					"xcommand userinterface branding fetch type: halfwakebranding url: {0}",
					_brandingUrl
				)
			);
		}
	}
}