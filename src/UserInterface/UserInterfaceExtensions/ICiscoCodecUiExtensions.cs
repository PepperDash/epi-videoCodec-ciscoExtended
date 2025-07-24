using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions.Panels;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceWebViewDisplay;
using PepperDash.Core;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions
{
	public interface ICiscoCodecUiExtensionsHandler :
		ICiscoCodecUiExtensionsWebViewDisplayHandler,
		ICiscoCodecUiExtensionsClickedEvent,
		ICiscoCodecUiExtensionsPanelClickedEventHandler
	{
	}

	public interface ICiscoCodecUiExtensionsController
	{
		ICiscoCodecUiExtensionsHandler CiscoCodecUiExtensionsHandler { get; set; }
	}

	public interface ICiscoCodecUiExtensions
	{
		List<Panel> Panels { get; }
		//other extensions later

		PanelsHandler PanelsHandler { get; }

		void Initialize(IKeyed parent, Action<string> enqueueCommand);

		void Update(Action<string> enqueueCommand);

		string xCommand();
	}

}
