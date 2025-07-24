using System;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions
{
    public interface ICiscoCodecUiExtensionsClickedEvent
    {
        event EventHandler<UiExtensionsClickedEventArgs> UiExtensionsClickedEvent;
	}
	public interface ICiscoCodecUiExtensionsPanelClickedEventHandler
	{
		/// <summary>
		/// Called by receive parser to parse event feedback
		/// </summary>
		/// <param name="panel"></param>
		void ParseStatus(Panels.CiscoCodecEvents.Panel panel);
	}

	public class UiExtensionsClickedEventArgs : EventArgs
    {
        public bool Clicked { get; set; }
        public string Id { get; set; }

        public UiExtensionsClickedEventArgs(bool clicked, string id)
        {
            Clicked = clicked;
            Id = id;
        }

        public UiExtensionsClickedEventArgs()
        {
        }
    }
}
