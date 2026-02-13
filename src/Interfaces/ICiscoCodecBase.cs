using System;
using PepperDash.Core;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Interfaces
{
    /// <summary>
    /// Minimal interface for Cisco codec implementations that defines only the essential functionality
    /// needed for Navigator-only scenarios. This interface provides a lighter alternative to 
    /// the full VideoCodecBase implementation.
    /// </summary>
    public interface ICiscoCodecBase : IKeyed
    {
        /// <summary>
        /// Queues a command to be sent to the codec
        /// </summary>
        /// <param name="command">The command to enqueue</param>
        void EnqueueCommand(string command);

        /// <summary>
        /// UI Extensions configuration for the codec
        /// </summary>
        UiExtensions UiExtensions { get; set; }

        /// <summary>
        /// UI Extensions handler for managing UI extensions
        /// </summary>
        ExtensionsHandler UiExtensionsHandler { get; set; }

        /// <summary>
        /// Event fired when the device is ready to use
        /// </summary>
        event EventHandler<EventArgs> IsReadyChange;

        /// <summary>
        /// Gets whether the device is ready for operation
        /// </summary>
        bool IsReady { get; }
    }
}