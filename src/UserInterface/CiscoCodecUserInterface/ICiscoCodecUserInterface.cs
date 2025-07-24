using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions;
using PepperDash.Core;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Devices.Common.VideoCodec.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.CiscoCodecUserInterface
{
    public interface ICiscoCodecUserInterface : IKeyed, IReconfigurableDevice, ICiscoCodecUiExtensionsController
	{
        CiscoCodec UisCiscoCodec { get; }
        CiscoCodecUserInterfaceConfig ConfigProps { get; }
        ICiscoCodecUiExtensions UiExtensions { get; }

		RoomCombiner.IRoomCombinerHandler RoomCombinerHandler { get; }

		void AddCustomActivationAction(Action a);
		bool EnableLockoutPoll { get; set; }
		bool LockedOut { get; set; }
	}

    public interface ICiscoCodecUserInterfaceConfig
	{
		Extensions Extensions { get; set; }
		string VideoCodecKey { get; set; }
	}
}
