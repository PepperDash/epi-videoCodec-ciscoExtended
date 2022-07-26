using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDashRoomOs.Core.Events;

namespace PepperDashRoomOs.Core
{
    public class RoomOsAddressBook
    {
        private readonly CTimer _requestDebounce;
        private readonly List<PhonebookSearchResult> _searchResults = new List<PhonebookSearchResult>();

        private bool _searchResponseActive;
        private bool _searchRequested;

        public event EventHandler<StringTransmitRequestedArgs> StringTransmitRequested;
        public event EventHandler<SearchResultReceivedArgs> SearchResultReceived;
        public event EventHandler SearchInProgress;
        public event EventHandler SearchStopped;

        public ushort MaxNumberOfSearchResults { get; set; }

        public int TotalRows
        {
            get { return CurrentSearchResults.Count(); }
        }

        public uint DebugLevel { get; set; }

        public IEnumerable<PhonebookSearchResult> CurrentSearchResults { get { return _searchResults.ToList(); }}

        public RoomOsAddressBook()
        {
            PhonebookType = PhonebookType.Corporate;
            MaxNumberOfSearchResults = 20;
            _requestDebounce = new CTimer(_ =>
            {
                var progressHandler = SearchStopped;
                if (progressHandler != null)
                    progressHandler(this, EventArgs.Empty);

                _searchResponseActive = false;
                _searchRequested = false;
            }, Timeout.Infinite);
        }

        public PhonebookType PhonebookType { get; set; }

        public const string PhonebookContactResultStart = "*r PhonebookSearchResult Contact ";
        public const string PhonebookSearchResultStart = "*r PhonebookSearchResult (status=ok)";
        public const string PhonebookSearchResultError = "*r PhonebookSearchResult (status=error)";
        public const string PhonebookSearchResultComplete = "** end";

        public string GetSearchResultNameByIndex(ushort resultIndex)
        {
            var result = CurrentSearchResults.FirstOrDefault(x => x.Index == resultIndex);
            return result == null ? String.Empty : result.Name;
        }

        public string GetCallMethodNumberByIndex(ushort resultIndex, ushort methodIndex)
        {
            var result = CurrentSearchResults.FirstOrDefault(x => x.Index == resultIndex);

            if (result == null)
                return String.Empty;

            var method = result.ContactMethods.FirstOrDefault(x => x.Index == methodIndex);
            return method == null ? String.Empty : method.Number;
        }

        public void ProcessCliResponse(string response)
        {
            try
            {
                if (String.IsNullOrEmpty(response) || !_searchRequested)
                    return;

                response = response.Trim();

                if (ResponseIsSearchResultStart(response))
                {
                    _searchResults.Clear();
                    _searchResponseActive = true;
                    return;
                }

                if (ResponseIsSearchResultError(response))
                {
                    _searchResults.Clear();
                    _searchResponseActive = true;
                    return;
                }

                if (ResponseIsSearchResultComplete(response) && _searchResponseActive)
                {
                    _searchResponseActive = false;
                    OnPhonebookSearchComplete();
                    _requestDebounce.Reset();
                    return;
                }
 
                if (!_searchResponseActive || !ResponseIsSearchContactResultStart(response))
                    return;

                var index = ParseContactIndex(response);
                if (index == 0)
                    throw new ArgumentOutOfRangeException("index", "Index is 0 so something is incorrect");

                var contact = _searchResults.FirstOrDefault(s => s.Index == index);
                if (contact == null)
                {
                    contact = new PhonebookSearchResult { Index = index };
                    _searchResults.Add(contact);
                }
                              
                if (response.Contains(" Name:"))
                {
                    contact.ParseContactName(response);
                }
                else if (response.Contains(" FirstName:"))
                {
                }
                else if (response.Contains(" LastName:"))
                {
                }
                else if (response.Contains(" ContactId:"))
                {
                    contact.ParseContactId(response);
                }
                else if (response.Contains(" ContactMethod"))
                {
                    contact.ParseContactMethod(response, DebugLevel);
                }
                else
                {
                    Debug.Console(DebugLevel, "Not sure how to parse this Contact String:{0}", response);
                }
            }
            catch (Exception e)
            {
                Debug.Console(DebugLevel, "Caught an error processing the string:{0}", e.Message);
            }
        }

        protected void OnPhonebookSearchComplete()
        {
            var handler = SearchResultReceived;
            if (handler != null)
            {
                for (var i = 1; i <= MaxNumberOfSearchResults; ++i)
                {
                    var index = i;
                    var result = CurrentSearchResults.FirstOrDefault(x => x.Index == index) ??
                                 new PhonebookSearchResult {Index = index};

                    handler(this, new SearchResultReceivedArgs { Index = result.Index, Name = result.Name });
                }
            }

            _requestDebounce.Reset();
        }

        public static bool ResponseIsSearchResultStart(string response)
        {
            return response.StartsWith(PhonebookSearchResultStart, StringComparison.OrdinalIgnoreCase);
        }

        public static bool ResponseIsSearchResultError(string response)
        {
            return response.StartsWith(PhonebookSearchResultError, StringComparison.OrdinalIgnoreCase);
        }

        public static bool ResponseIsSearchContactResultStart(string response)
        {
            return response.StartsWith(PhonebookContactResultStart, StringComparison.OrdinalIgnoreCase);
        }

        public static bool ResponseIsSearchResultComplete(string response)
        {
            return response.Contains(PhonebookSearchResultComplete);
        }

        public static int ParseContactIndex(string response)
        {
            try
            {
                var result = response.Remove(0, PhonebookContactResultStart.Length).Split(new[] { ' ' })[0];
                return Int32.Parse(result);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        
        
        public void Search(string searchString)
        {
            if (searchString.Length < 3 || _searchRequested)
                return;

            const string command = "xCommand Phonebook Search PhonebookType: {0} SearchString: \"{1}\" Limit: {2}\r\n";
            _searchRequested = true;

            var progressHandler = SearchInProgress;
            if (progressHandler != null)
                progressHandler(this, EventArgs.Empty);

            var handler = StringTransmitRequested;
            if (handler != null)
            {
                var txString = String.Format(
                    command,
                    PhonebookType.ToString(),
                    searchString,
                    MaxNumberOfSearchResults);

                handler(this, new StringTransmitRequestedArgs { TxString = txString });
            }

            _requestDebounce.Reset(30000);
        }

        public void ClearSearch()
        {
            _requestDebounce.Reset();
            _searchResults.Clear();
            OnPhonebookSearchComplete();
        }
    }
}