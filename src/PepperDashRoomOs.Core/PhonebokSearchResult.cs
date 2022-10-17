using System;
using System.Collections.Generic;

namespace PepperDashRoomOs.Core
{
    public class PhonebokSearchResult
    {
        public string Id { get; set; }

        public List<PhonebookSearchContactResult> Contacts { get; set; }

        public PhonebokSearchResult()
        {
            Contacts = new List<PhonebookSearchContactResult>();
        }
    }
}