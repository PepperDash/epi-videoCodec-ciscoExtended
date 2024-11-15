using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using PepperDash.Core;
using PepperDash.Core.Intersystem;
using PepperDash.Core.Intersystem.Tokens;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.Codec;
using PepperDash.Essentials.Devices.Common.VideoCodec;

namespace epi_videoCodec_ciscoExtended.V2
{
    public class CiscoDirectory : CiscoRoomOsFeature, IHasDirectory
    {
        private int searchinProgress;

        private uint phonebookResultsLimit = 10;
        private bool phoneBookIsLocal;
        private bool currentDirectoryIsNotRoot;

        private readonly CTimer searchTimeout;
        private readonly CiscoRoomOsDevice parent;

        private int selectedEntry;
        private int selectedEntryCallMethod;

        public CiscoDirectory(CiscoRoomOsDevice parent) : base(parent.Key + "-directory")
        {
            
            this.parent = parent;


            if (parent.phoneBookLimit != 0)
                phonebookResultsLimit = parent.phoneBookLimit;

            SearchIsInProgress = new BoolFeedback("SearchInProgress", () => searchinProgress > 0);

            searchTimeout = new CTimer(_ =>
            {
                Interlocked.Exchange(ref searchinProgress, 0);
                SearchIsInProgress.FireUpdate();
            }, Timeout.Infinite);

            DirectoryBrowseHistory = new List<CodecDirectory>();
            PhonebookSyncState = new CodecPhonebookSyncState(parent.Key + "-phonebook-sync");
            DirectoryRoot = new CodecDirectory();

            CurrentDirectoryResultIsNotDirectoryRoot = new BoolFeedback(() => currentDirectoryIsNotRoot);
        }

        public void SearchDirectory(string searchString)
        {
            if (Interlocked.CompareExchange(ref searchinProgress, 1, 0) == 0)
            {
                var nameToSearch = string.IsNullOrEmpty(searchString)
                    ? "\"\""
                    : "\"" + searchString + "\"";

                var phonebookMode = phoneBookIsLocal ? "Local" : "Corporate";
                
                var command = string.Format(
                    "xCommand Phonebook Search SearchString: {0} PhonebookType: {1} Limit: {2}",
                    nameToSearch, phonebookMode, phonebookResultsLimit);

                parent.SendTaggedRequest(command, result =>
                {
                    try
                    {
                        var newPhonebook = ParsePhonebookSearchResult(result);
                        CurrentDirectoryResult = newPhonebook;
                        CurrentDirectoryResultIsNotDirectoryRoot.FireUpdate();

                        var handler = DirectoryResultReturned;
                        if (handler == null)
                            return;

                        handler(this, new DirectoryEventArgs { Directory = newPhonebook, DirectoryIsOnRoot = !currentDirectoryIsNotRoot });
                    }
                    catch (Exception ex)
                    {
                        Debug.Console(0, parent, "Caught an exception parsing a phonebook result: {0}", ex);
                    }
                    finally
                    {
                        searchTimeout.Reset();
                    }
                });

                searchTimeout.Reset(30000);
                SearchIsInProgress.FireUpdate();
            }
        }

        public void SearchDirectory(string searchString, Action<CodecDirectory> onResult)
        {
            var nameToSearch = string.IsNullOrEmpty(searchString)
                    ? ""
                    : "\"" + searchString + "\"";

            var phonebookMode = phoneBookIsLocal ? "Local" : "Corporate";

            var command = string.Format(
                "xCommand Phonebook Search SearchString: {0} PhonebookType: {1} Limit: {2}",
                nameToSearch, phonebookMode, phonebookResultsLimit);

            parent.SendTaggedRequest(command, result =>
            {
                try
                {
                    var newPhonebook = ParsePhonebookSearchResult(result);
                    onResult(newPhonebook);
                }
                catch (Exception ex)
                {
                    Debug.Console(0, parent, "Caught an exception parsing a phonebook result: {0}", ex);
                }
            });
        }

        private CodecDirectory ParsePhonebookSearchResult(string result)
        {
            // *r PhonebookSearchResult Contact 1 Name: "Nick"
            var directoryResults = new CodecDirectory();
            var items = new Dictionary<string, DirectoryContact>();
            var folders = new Dictionary<string, DirectoryFolder>();

            foreach (var line in result.Split('|'))
            {
                const string pattern = @"\*r PhonebookSearchResult Contact (\d+) (.*?): ""([^""]+)""";
                var match = Regex.Match(line, pattern);
                if (!match.Success)
                {
                    continue;
                }

                var itemIndex = match.Groups[1].Value;
                var property = match.Groups[2].Value;
                var value = match.Groups[3].Value;

                DirectoryContact currentItem;
                if (!items.TryGetValue(itemIndex, out currentItem))
                {
                    currentItem = new DirectoryContact { ParentFolderId = "root", FolderId = "", Name = "", Title = "", ContactId = "" };
                    items.Add(itemIndex, currentItem);
                }

                if (property.Equals("Name"))
                {
                    currentItem.Name = value.Trim(new []{ ' ', '\"' });
                    Debug.Console(1, parent, "Directory Item:{0} | Name {1}", itemIndex, value);
                }
                else if (property.Contains("ContactMethod"))
                {
                    ParseContactMethods(property, value, currentItem);
                }
            }

            directoryResults.AddContactsToDirectory(items.Values.Cast<DirectoryItem>().ToList());
            directoryResults.AddFoldersToDirectory(folders.Values.Cast<DirectoryItem>().ToList());

            return directoryResults;
        }

        private void ParseContactMethods(string property, string value, DirectoryContact item)
        {
            try
            {
                // *r PhonebookSearchResult Contact 13 ContactMethod 1 ContactMethodId: "1"

                Debug.Console(1, parent, "Parsing contact method:{0}", property);

                var index = property.Split(' ')[1];

                var contactMethod = item.ContactMethods.Find(m => m.ContactMethodId == value);
                if (contactMethod == null)
                {
                    contactMethod = new ContactMethod {CallType = eContactMethodCallType.Video, ContactMethodId = index};
                    item.ContactMethods.Add(contactMethod);
                }

                if (property.Contains("Number"))
                {
                    contactMethod.Number = value;
                }
            }
            catch (Exception ex)
            {
                Debug.Console(0, parent, "Caught and exception parsing the contact methods:{0} {1}", value, ex);
            }
        }

        public void GetDirectoryFolderContents(string folderId)
        {
            // throw new NotImplementedException();
        }

        public void SetCurrentDirectoryToRoot()
        {
            // throw new NotImplementedException();
        }

        public void GetDirectoryParentFolderContents()
        {
            // throw new NotImplementedException();
        }

        public BoolFeedback SearchIsInProgress { get; private set; }

        public CodecDirectory DirectoryRoot { get; private set; }
        public CodecDirectory CurrentDirectoryResult { get; private set; }

        public CodecPhonebookSyncState PhonebookSyncState { get; private set; }
        public BoolFeedback CurrentDirectoryResultIsNotDirectoryRoot { get; private set; }
        public List<CodecDirectory> DirectoryBrowseHistory { get; private set; }

        public event EventHandler<DirectoryEventArgs> DirectoryResultReturned;

        public static string UpdateDirectoryXSig(CodecDirectory directory, bool isRoot)
		{
			const int xSigMaxIndex = 1023;
			var tokenArray = new XSigToken[directory.CurrentDirectoryResults.Count > xSigMaxIndex
				? xSigMaxIndex
				: directory.CurrentDirectoryResults.Count];

			var contacts = directory.CurrentDirectoryResults.Count > xSigMaxIndex
				? directory.CurrentDirectoryResults.Take(xSigMaxIndex)
				: directory.CurrentDirectoryResults;

			var contactsToDisplay = isRoot
				? contacts.Where(c => c.ParentFolderId == "root")
				: contacts.Where(c => c.ParentFolderId != "root");

			var counterIndex = 1;
			foreach (var entry in contactsToDisplay)
			{
				var arrayIndex = counterIndex - 1;
				var entryIndex = counterIndex;

				if (entry is DirectoryFolder)
				{
					tokenArray[arrayIndex] = new XSigSerialToken(entryIndex, String.Format("[+] {0}", entry.Name));
					counterIndex++;
					continue;
				}

				tokenArray[arrayIndex] = new XSigSerialToken(entryIndex, entry.Name);
				counterIndex++;
			}

			return GetXSigString(tokenArray);
		}

        public string UpdateContactMethodsXSig(DirectoryContact contact)
        {
            const int maxMethods = 10;
            const int maxStrings = 3;
            const int offset = maxStrings;
            var stringIndex = 0;
            var arrayIndex = 0;
            // Create a new token array and set the size to the number of methods times the total number of signals
            var tokenArray = new XSigToken[maxMethods * offset];

            Debug.Console(2, this, "Creating XSIG token array with size {0}", maxMethods * offset);

            // TODO: Add code to generate XSig data
            foreach (var method in contact.ContactMethods)
            {
                if (arrayIndex >= maxMethods * offset)
                    break;

                //serials
                tokenArray[arrayIndex + 1] = new XSigSerialToken(stringIndex + 1, method.Number);
                tokenArray[arrayIndex + 2] = new XSigSerialToken(stringIndex + 2, method.ContactMethodId.ToString());
                tokenArray[arrayIndex + 3] = new XSigSerialToken(stringIndex + 3, method.Device.ToString());

                arrayIndex += offset;
                stringIndex += maxStrings;
            }

            while (arrayIndex < maxMethods)
            {
                tokenArray[arrayIndex + 1] = new XSigSerialToken(stringIndex + 1, String.Empty);
                tokenArray[arrayIndex + 2] = new XSigSerialToken(stringIndex + 2, String.Empty);
                tokenArray[arrayIndex + 3] = new XSigSerialToken(stringIndex + 3, String.Empty);

                arrayIndex += offset;
                stringIndex += maxStrings;
            }

            return GetXSigString(tokenArray);
        }

        private static string GetXSigString(XSigToken[] tokenArray)
        {
            const int xSigEncoding = 28591;

            using (var s = new MemoryStream())
            using (var tw = new XSigTokenStreamWriter(s, false))
            {
                tw.WriteXSigData(tokenArray);
                var xSig = s.ToArray();
                return Encoding.GetEncoding(xSigEncoding).GetString(xSig, 0, xSig.Length);
            }
        }
    }
}