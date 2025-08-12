using System;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions
{
    public class UiExtensionsClickedEventArgs : EventArgs
    {
        public readonly bool Clicked;
        public readonly string Id;

        public UiExtensionsClickedEventArgs(bool clicked, string id)
        {
            Clicked = clicked;
            Id = id;
        }
    }
}
