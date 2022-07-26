using System.Collections.Generic;

namespace PepperDashRoomOs.Core
{
    public class PhonebookSearchResult
    {
        public int Index { get; set; }
        public string ContactId { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public List<PhonebookSearchResultContactMethod> ContactMethods { get; set; }

        public PhonebookSearchResult()
        {
            ContactMethods = new List<PhonebookSearchResultContactMethod>();
        }
    }
}