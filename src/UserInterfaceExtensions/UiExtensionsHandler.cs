using epi_videoCodec_ciscoExtended.UserInterfaceWebViewDisplay;
using PepperDash.Core;
using PepperDash.Essentials.Devices.Common.VideoCodec.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace epi_videoCodec_ciscoExtended.UserInterfaceExtensions
{
	public class UiExtensionsHandler : ICiscoCodecUiExtensionsHandler, IVideoCodecUiExtensionsHandler
	{
		private readonly IKeyed _parent;
		private readonly IBasicCommunication _coms;

		public Action<UiWebViewDisplayActionArgs> UiWebViewDisplayAction { get; set; }

		public event EventHandler<UiExtensionsClickedEventArgs> UiExtensionsClickedEvent;

		public UiExtensionsHandler(IKeyed parent, IBasicCommunication coms)

		{
			_parent = parent;
			_coms = coms;

			//set the action that will run when called with args from elsewhere via interface
			UiWebViewDisplayAction =
				new Action<UiWebViewDisplayActionArgs>((UiWebViewDisplayActionArgs args) =>
				{
					UiWebViewDisplay uwvd = new UiWebViewDisplay { Header = args.Header, Url = args.Url, Mode = args.Mode, Title = args.Title };
					coms.SendText(uwvd.xCommand());
				});

		}

		public void ParseStatus(Panels.CiscoCodecEvents.Panel panel)
		{
			if (panel.Clicked != null && panel.Clicked.PanelId != null && panel.Clicked.PanelId.Value != null)
			{
				UiExtensionsClickedEvent?.Invoke(this, new UiExtensionsClickedEventArgs(true, panel.Clicked.PanelId.Value));
			}
		}
	}
}
