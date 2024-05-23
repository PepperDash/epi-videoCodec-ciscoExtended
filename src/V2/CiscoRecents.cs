using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.Codec;
using PepperDash.Essentials.Devices.Common.VideoCodec;

namespace epi_videoCodec_ciscoExtended.V2
{
    public class CiscoRecents : CiscoRoomOsFeature, IHasEventSubscriptions, IHandlesResponses, IHasCallHistory, IHasPolls
    {
        private readonly CiscoRoomOsDevice parent;
        private readonly CodecCallHistory callhistory;

        private int selectedRecent;
        public int SelectedRecent
        {
            get { return selectedRecent; }
            set
            {
                selectedRecent = value;
                SelectedRecentName.FireUpdate();
                SelectedRecentNumber.FireUpdate();
            }
        }

        internal readonly List<StringFeedback> Feedbacks;
        internal readonly StringFeedback SelectedRecentName;
        internal readonly StringFeedback SelectedRecentNumber;

        public CiscoRecents(CiscoRoomOsDevice parent) : base (parent.Key + "-Recents")
        {
            this.parent = parent;

            this.parent.CallStatus.CallIsConnectedOrConnecting.OutputChange += (sender, args) =>
            {
                if (args.BoolValue)
                {
                    return;
                }

                parent.SendText("xCommand CallHistory Get Limit:10");
            };

            Polls = new List<string>
            {
                "xCommand CallHistory Get Limit:10"
            };

            Subscriptions = new List<string>();
            callhistory = new CodecCallHistory();

            Feedbacks = new List<StringFeedback>();
            for (var x = 0; x < 10; ++x)
            {
                var index = x;
                Feedbacks.Add(new StringFeedback("Recent:" + index, () =>
                {
                    var recent = callhistory.RecentCalls.ElementAt(index);
                    return recent == null ? string.Empty : recent.Name;
                }));
            }

            callhistory.RecentCallsListHasChanged += (sender, args) =>
            {
                foreach (var feedback in Feedbacks)
                {
                    feedback.FireUpdate();
                }
            };

            SelectedRecentName = new StringFeedback("SelectedRecentName", () =>
            {
                if (SelectedRecent == 0)
                {
                    return string.Empty;
                }

                var result = callhistory.RecentCalls.ElementAtOrDefault(SelectedRecent - 1);
                return result == null ? string.Empty : result.Name;
            });

            SelectedRecentNumber = new StringFeedback("SelectedRecentName", () =>
            {
                if (SelectedRecent == 0)
                {
                    return string.Empty;
                }

                var result = callhistory.RecentCalls.ElementAtOrDefault(SelectedRecent - 1);
                return result == null ? string.Empty : result.Number;
            });

            SelectedRecentName.RegisterForDebug(this);
            SelectedRecentNumber.RegisterForDebug(this);
        }

        public IEnumerable<string> Subscriptions { get; private set; }

        public bool HandlesResponse(string response)
        {
            return response.IndexOf("*r CallHistoryGetResult", StringComparison.Ordinal) > -1;
        }

        public void HandleResponse(string response)
        {
            var historyItems = new Dictionary<string, CiscoCallHistory.Entry>();
            foreach (var line in response.Split('|'))
            {
                const string pattern = @"\*r CallHistoryGetResult Entry (\d+) (.*?): (.+)";

                var match = Regex.Match(line, pattern);
                if (!match.Success)
                {
                    continue;
                }

                var itemIndex = match.Groups[1].Value;
                var property = match.Groups[2].Value;
                var value = match.Groups[3].Value.Trim(new []{ ' ', '\"' });

                CiscoCallHistory.Entry currentItem;
                if (!historyItems.TryGetValue(itemIndex, out currentItem))
                {
                    currentItem = new CiscoCallHistory.Entry { id = itemIndex };
                    historyItems.Add(itemIndex, currentItem);
                }

                /**r CallHistoryGetResult (status=OK): 
*r CallHistoryGetResult Entry 0 CallHistoryId: 73
*r CallHistoryGetResult Entry 0 CallbackNumber: ""
*r CallHistoryGetResult Entry 0 DisplayName: ""
*r CallHistoryGetResult Entry 0 StartTime: "2024-01-20T20:14:03"
*r CallHistoryGetResult Entry 0 DaysAgo: 0
*r CallHistoryGetResult Entry 0 OccurrenceType: Placed
*r CallHistoryGetResult Entry 0 IsAcknowledged: Acknowledged
*r CallHistoryGetResult Entry 1 CallHistoryId: 72
*r CallHistoryGetResult Entry 1 CallbackNumber: ""
*r CallHistoryGetResult Entry 1 DisplayName: ""
*r CallHistoryGetResult Entry 1 StartTime: ""
*r CallHistoryGetResult Entry 1 DaysAgo: 0*/

                if (property.Equals("CallbackNumber"))
                {
                    currentItem.CallbackNumber = new CiscoCallHistory.CallbackNumber { Value = value };
                }
                else if (property.Contains("DisplayName"))
                {
                    currentItem.DisplayName = new CiscoCallHistory.DisplayName { Value = value };
                }
                else if (property.Contains("StartTime"))
                {
                    currentItem.LastOccurrenceStartTime = new CiscoCallHistory.LastOccurrenceStartTime { Value = DateTime.Parse(value) };
                }
                else if (property.Contains("DaysAgo"))
                {
                    currentItem.LastOccurrenceDaysAgo = new CiscoCallHistory.LastOccurrenceDaysAgo { Value = value };
                }
                else if (property.Contains("IsAcknowledged"))
                {
                    currentItem.IsAcknowledged = new CiscoCallHistory.IsAcknowledged { Value = value };
                }
                else if (property.Contains("OccurrenceType"))
                {
                    currentItem.OccurrenceType = new CiscoCallHistory.OccurrenceType { Value = value };
                }
                else
                {
                    currentItem.LastOccurrenceHistoryId = new CiscoCallHistory.LastOccurrenceHistoryId {Value = itemIndex};
                }
            }

            callhistory.ConvertCiscoCallHistoryToGeneric(historyItems.Values.ToList());
        }

        public void RemoveCallHistoryEntry(CodecCallHistory.CallHistoryEntry entry)
        {
            var command = string.Format(
                "xCommand CallHistory DeleteEntry CallHistoryId: {0} AcknowledgeConsecutiveDuplicates: True",
                entry.OccurrenceHistoryId);

            parent.SendText(command);
        }

        public CodecCallHistory CallHistory
        {
            get
            {
                return callhistory;
            }
        }

        public IEnumerable<string> Polls { get; private set; }
    }
}