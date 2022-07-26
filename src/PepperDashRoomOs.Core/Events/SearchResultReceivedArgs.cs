using System;

namespace PepperDashRoomOs.Core.Events
{
    public class SearchResultReceivedArgs : EventArgs
    {
        public int Index { get; set; }
        public string Name { get; set; }
    }
}