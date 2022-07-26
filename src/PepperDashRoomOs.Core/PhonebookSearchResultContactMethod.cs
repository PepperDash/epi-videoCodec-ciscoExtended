using System;
using System.Text;
// For Basic SIMPL# Classes
using PepperDash.Core;

namespace PepperDashRoomOs.Core
{
    public class PhonebookSearchResultContactMethod
    {
        public int Index { get; set; }
        public string ContactMethodId { get; set; }
        public string Number { get; set; }

        public PhonebookSearchResultContactMethod()
        {
            ContactMethodId = String.Empty;
            Number = String.Empty;
        }
    }
   
}
