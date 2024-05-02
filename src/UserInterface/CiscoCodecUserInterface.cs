using epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceExtensions;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Devices.Common.VideoCodec.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace epi_videoCodec_ciscoExtended.UserInterface
{
	public interface ICiscoCodecUserInterface 
	{
		CiscoCodec CiscoCodec { get; }
		CiscoCodecUserInterfaceConfig ConfigProps { get; }
		IVideoCodecUiExtensionsHandler VideoCodecUiExtensionsHandler { get; }
		ICiscoCodecUiExtensions UiExtensions { get; }
	}

	public class CiscoCodecUserInterface : ReconfigurableDevice, ICiscoCodecUserInterface, IReconfigurableDevice
	{
		public CiscoCodec CiscoCodec { get; }
		public CiscoCodecUserInterfaceConfig ConfigProps { get; }
		public IVideoCodecUiExtensionsHandler VideoCodecUiExtensionsHandler { get; }

		public ICiscoCodecUiExtensions UiExtensions { get; }

		public T ParseConfigProps<T>(DeviceConfig config)
		{
			return JsonConvert.DeserializeObject<T>(config.Properties.ToString());
		}

		public CiscoCodecUserInterface(DeviceConfig config): base(config)
		{
			ParseConfigProps< CiscoCodecUserInterfaceConfig>(config);
			CiscoCodec = DeviceManager.GetDeviceForKey(ConfigProps.VideoCodecKey) as CiscoCodec;

			if(CiscoCodec == null)
			{
				var msg = $"Video codec UserInterface could not find codec with key '{ConfigProps.VideoCodecKey}'.";
				Debug.LogMessage(new NullReferenceException(msg), msg, this);
				return;
			}

			UiExtensions = ConfigProps.Extensions;
			VideoCodecUiExtensionsHandler = new UiExtensionsHandler(this, CiscoCodec.EnqueueCommand);
		}
	}
}
