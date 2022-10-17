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
        private PhonebokSearchResult _phonebokSearchResult = new PhonebokSearchResult();

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

        public IEnumerable<PhonebookSearchContactResult> CurrentSearchResults { get { return _phonebokSearchResult.Contacts.ToList(); } }

        public RoomOsAddressBook()
        {
            PhonebookType = PhonebookType.Corporate;
            MaxNumberOfSearchResults = 20;
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
                    _phonebokSearchResult = new PhonebokSearchResult();
                    _searchResponseActive = true;
                    return;
                }

                if (ResponseIsSearchResultError(response))
                {
                    _phonebokSearchResult = new PhonebokSearchResult();
                    _searchResponseActive = true;
                    return;
                }

                if (ResponseIsSearchResultComplete(response) && _searchResponseActive)
                {
                    _waithHandle.Set();
                    return;
                }
 
                if (!_searchResponseActive || !ResponseIsSearchContactResultStart(response))
                    return;

                var index = ParseContactIndex(response);
                if (index == 0)
                    throw new ArgumentOutOfRangeException("index", "Index is 0 so something is incorrect");

                var contact = _phonebokSearchResult.Contacts.FirstOrDefault(s => s.Index == index);
                if (contact == null)
                {
                    contact = new PhonebookSearchContactResult { Index = index };
                    _phonebokSearchResult.Contacts.Add(contact);
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
                else if (response.Contains("** resultId:"))
                {
                    _phonebokSearchResult.Id = ParseRequestId(response);
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
                                 new PhonebookSearchContactResult {Index = index};

                    handler(this, new SearchResultReceivedArgs { Index = result.Index, Name = result.Name });
                }
            }
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

        public static string ParseRequestId(string response)
        {
            try
            {
                var result = response.Split(new[] {' '})[1].TrimStart(new[] {'"'}).TrimEnd(new[] {'"'});
                return result;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private readonly CrestronQueue<string> _searchTags = new CrestronQueue<string>();
        private readonly CEvent _waithHandle = new CEvent();

        public void Search(string searchString)
        {
            const string command = "xCommand Phonebook Search PhonebookType: {0} SearchString: \"{1}\" Limit: {2} Tag: {3}\r\n";

            if (searchString.Length < 3 || _searchRequested)
                return;

            var searchTag = Guid.NewGuid().ToString();

            _searchTags.Enqueue(searchTag);
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
                    MaxNumberOfSearchResults,
                    searchTag);

                handler(this, new StringTransmitRequestedArgs { TxString = txString });
            }

            CrestronInvoke.BeginInvoke(_ =>
            {
                var success = false;
                _waithHandle.Wait(5000);
                while (!_searchTags.IsEmpty)
                {
                    var tag = _searchTags.Dequeue();
                    if (_phonebokSearchResult.Id != tag)
                        continue;

                    OnPhonebookSearchComplete();
                    success = true;
                }

                var searchStopped = SearchStopped;
                if (searchStopped != null)
                    searchStopped(this, EventArgs.Empty);

                if (!success)
                {
                    // TODO fire off a failed result
                }

                _searchResponseActive = false;
                _searchRequested = false;
            });
        }
    }
}