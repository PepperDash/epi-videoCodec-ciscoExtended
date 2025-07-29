using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Interfaces;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Core.Intersystem;
using PepperDash.Core.Intersystem.Tokens;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.DeviceInfo;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Core.Queues;
using PepperDash.Essentials.Devices.Common.Cameras;
using PepperDash.Essentials.Devices.Common.Codec;
using PepperDash.Essentials.Devices.Common.Codec.Cisco;
using PepperDash.Essentials.Devices.Common.VideoCodec;
using Serilog.Events;
using Feedback = PepperDash.Essentials.Core.Feedback;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceWebViewDisplay;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
	/// <summary>
	/// Partial class implementation for IHasDirectory
	/// </summary>
	public partial class CiscoCodec
	{
		#region IHasDirectory Implementation

		public event EventHandler<DirectoryEventArgs> DirectoryResultReturned;

		public CodecDirectory DirectoryRoot { get; private set; }

		public CodecDirectory CurrentDirectoryResult
		{
			get
			{
				if (DirectoryBrowseHistory.Count > 0)
					return DirectoryBrowseHistory[DirectoryBrowseHistory.Count - 1];
				else
					return DirectoryRoot;
			}
		}

		public BoolFeedback CurrentDirectoryResultIsNotDirectoryRoot { get; private set; }

		public List<CodecDirectory> DirectoryBrowseHistory { get; private set; }

		public BoolFeedback DirectorySearchInProgress { get; private set; }

		public CodecPhonebookSyncState PhonebookSyncState { get; private set; }

		// Private fields related to directory/phonebook functionality  
		private CTimer PhonebookRefreshTimer;
		private readonly CrestronQueue<string> _searches = new CrestronQueue<string>();
		private bool _searchInProgress;
		private string _lastSearched;

		/// <summary>
		/// Initializes Directory-related properties and handlers. Called from main constructor.
		/// </summary>
		private void InitializeDirectoryFeedbacks()
		{
			DirectoryRoot = new CodecDirectory();
			DirectoryBrowseHistory = new List<CodecDirectory>();

			CurrentDirectoryResultIsNotDirectoryRoot = new BoolFeedback(
				() => DirectoryBrowseHistory.Count > 0
			);

			CurrentDirectoryResultIsNotDirectoryRoot.FireUpdate();

			DirectorySearchInProgress = new BoolFeedback(() => _searchInProgress);

			PhonebookSyncState = new CodecPhonebookSyncState(Key + "--PhonebookSync");
			PhonebookSyncState.InitialSyncCompleted += PhonebookSyncState_InitialSyncCompleted;

			_lastSearched = string.Empty;
		}

		public void SearchDirectory(string searchString)
		{
			Debug.Console(
				2,
				this,
				"_phonebookAutoPopulate = {0}, searchString = {1}, _lastSeached = {2}, _phonebookInitialSearch = {3}",
				_phonebookAutoPopulate,
				searchString,
				_lastSearched,
				_phonebookInitialSearch
			);
			if (_phonebookAutoPopulate || !String.IsNullOrEmpty(searchString))
			{
				// No need to search for the same thing twice in a row
				// But keep the initial search result to avoid repeat search during initial sync
				if (String.Equals(searchString, _lastSearched, StringComparison.OrdinalIgnoreCase)
				    && !_phonebookInitialSearch)
				{
					Debug.Console(
						1,
						this,
						"Skipping search because search term '{0}' matches last search term '{1}'",
						searchString,
						_lastSearched
					);
					return;
				}

				_lastSearched = searchString;

				_phonebookInitialSearch = false;

				if (_searchInProgress)
				{
					Debug.Console(1, this, "Search request queued. '{0}' items in search queue.", _searches.Count);
					_searches.Enqueue(searchString);
					return;
				}

				_searchInProgress = true;
				DirectorySearchInProgress.FireUpdate();

				if (_phonebookMode.Equals("Corporate", StringComparison.OrdinalIgnoreCase))
				{
					// Use Corporate mode search with limit
					EnqueueCommand(string.Format("xCommand Phonebook Search Phonebook: Corporate SearchString: \"{0}\" Limit: {1}", searchString, _phonebookResultsLimit));
				}
				else
				{
					// Use Local mode search
					EnqueueCommand(string.Format("xCommand Phonebook Search Phonebook: Local SearchString: \"{0}\"", searchString));
				}
			}
		}

		public void GetDirectoryParentFolderContents()
		{
			var currentDirectory = new CodecDirectory();

			if (DirectoryBrowseHistory.Count > 0)
			{
				var lastItemIndex = DirectoryBrowseHistory.Count - 1;
				var parentDirectoryContents = DirectoryBrowseHistory[lastItemIndex];

				DirectoryBrowseHistory.Remove(DirectoryBrowseHistory[lastItemIndex]);

				currentDirectory = parentDirectoryContents;
			}
			else
			{
				currentDirectory = DirectoryRoot;
			}

			OnDirectoryResultReturned(currentDirectory);
		}

		public void SetCurrentDirectoryToRoot()
		{
			DirectoryBrowseHistory.Clear();

			OnDirectoryResultReturned(DirectoryRoot);
		}

		public void GetDirectoryFolderContents(string folderId)
		{
			EnqueueCommand(
				string.Format(
					"xCommand Phonebook Search FolderId: {0} PhonebookType: {1} ContactType: Any Limit: {2}",
					folderId,
					_phonebookMode,
					_phonebookResultsLimit
				)
			);
		}

		private void OnDirectoryResultReturned(CodecDirectory result)
		{
			if (result == null)
			{
				Debug.Console(1, this, "OnDirectoryResultReturned - result is null");
				return;
			}
			Debug.Console(1, this, "OnDirectoryResultReturned");
			CurrentDirectoryResultIsNotDirectoryRoot.FireUpdate();

			// This will return the latest results to all UIs.  Multiple indendent UI Directory browsing will require a different methodology
			var handler = DirectoryResultReturned;
			if (handler != null)
			{
				Debug.Console(1, this, "Directory result returned");
				handler(
					this,
					new DirectoryEventArgs()
					{
						Directory = result,
						DirectoryIsOnRoot = !CurrentDirectoryResultIsNotDirectoryRoot.BoolValue
					}
				);
			}

			PrintDirectory(result);
		}

		private void PrintDirectory(CodecDirectory directory)
		{
			Debug.Console(0, this, "Attempting to Print Directory");
			if (directory == null)
				return;
			Debug.Console(0, this, "Directory Results:\n");

			foreach (var item in directory.CurrentDirectoryResults)
			{
				if (item is DirectoryFolder)
				{
					Debug.Console(1, this, "[+] {0}", item.Name);
				}
				else if (item is DirectoryContact)
				{
					Debug.Console(1, this, " -  {0}", item.Name);
				}
			}
			Debug.Console(
				1,
				this,
				"Directory is on Root Level: {0}",
				!CurrentDirectoryResultIsNotDirectoryRoot.BoolValue
			);
		}

		private void PhonebookSyncState_InitialSyncCompleted(object sender, EventArgs e)
		{
			Debug.Console(0, this, "PhonebookSyncState_InitialSyncCompleted");
			if (DirectoryRoot == null)
				return;
			OnDirectoryResultReturned(DirectoryRoot);
		}

		#endregion
	}
}