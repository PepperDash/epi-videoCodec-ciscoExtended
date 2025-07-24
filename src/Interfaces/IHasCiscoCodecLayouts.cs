using System;
using System.Collections.Generic;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.VideoCodec;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Interfaces
{
    /// <summary>
    /// Defines the required elements for layout control with direct layout selection
    /// </summary>
    public interface IHasCodecLayoutsAvailable : IHasCodecLayouts
    {

        event EventHandler<AvailableLayoutsChangedEventArgs> AvailableLayoutsChanged;
        event EventHandler<CurrentLayoutChangedEventArgs> CurrentLayoutChanged;

        StringFeedback AvailableLayoutsFeedback { get; }
        List<CodecCommandWithLabel> AvailableLayouts { get; }
        void LayoutSet(string layout);
        void LayoutSet(CodecCommandWithLabel layout);

    }

    public class AvailableLayoutsChangedEventArgs : EventArgs
    {
        public List<CodecCommandWithLabel> AvailableLayouts { get; set; }
    }

    public class CurrentLayoutChangedEventArgs : EventArgs
    {
        public string CurrentLayout { get; set; }
    }
}
