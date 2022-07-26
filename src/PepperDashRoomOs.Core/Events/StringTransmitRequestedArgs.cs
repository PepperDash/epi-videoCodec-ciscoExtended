using System;

namespace PepperDashRoomOs.Core.Events
{
    public class StringTransmitRequestedArgs : EventArgs
    {
        public string TxString { get; set; }

        public StringTransmitRequestedArgs()
        {
            TxString = String.Empty;
        }
    }
}