using epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceExtensions.Panels;
using epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceWebViewDisplay;
using PepperDash.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceExtensions
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

        string xCommand();
    }

}
