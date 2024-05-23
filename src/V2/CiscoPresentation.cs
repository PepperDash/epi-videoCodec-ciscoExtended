using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Devices.Common.Codec;

namespace epi_videoCodec_ciscoExtended.V2
{
    /*
     * 
     *  *e PresentationStarted Cause: userRequested
        *e PresentationStarted ConferenceId: 7
        *e PresentationStarted Mode: Sending
        *e PresentationStarted CallId: 1
        *e PresentationStarted LocalInstance: 1
        *e PresentationStarted LocalSource: 2
        ** end
        *e PresentationStopped Cause: userRequested
        *e PresentationStopped ConferenceId: 7
        *e PresentationStopped Mode: Sending
        *e PresentationStopped CallId: 1
        *e PresentationStopped LocalInstance: 1
        ** end
     *  *s Conference Presentation LocalInstance 1 SendingMode: LocalRemote
        *s Conference Presentation LocalInstance 1 Source: 2
        *s Conference Presentation LocalInstance 1 (ghost=True):
     */

    public class CiscoPresentation : CiscoRoomOsFeature, IHasContentSharing, IHasPolls, IHasEventSubscriptions, IHandlesResponses, IHasFarEndContentStatus
    {
        public class CiscoPresentationInfo
        {
            public string SendingMode { get; set; }
            public int LocalInstance { get; set; }
            public string LocalSource { get; set; }
        }

        public enum CiscoPresentationMode
        {
            Off, Sending, Receiving
        }

        private readonly IDictionary<int, CiscoPresentationInfo> presentations;
 
        private readonly CiscoRoomOsDevice parent;

        private readonly bool defaultToLocalOnly;

        private string shareSourceId;

        private bool isReceivingContent;

        public CiscoPresentation(CiscoRoomOsDevice parent, bool defaultToLocalOnly) : this(parent)
        {
            this.defaultToLocalOnly = defaultToLocalOnly;
        }

        public CiscoPresentation(CiscoRoomOsDevice parent) : base(parent + "-Presentation")
        {
            this.parent = parent;
            
            presentations = new Dictionary<int, CiscoPresentationInfo>();

            Polls = new List<string>
            {
                "xStatus Conference Presentation",    
                "xConfiguration Video Presentation DefaultSource"                
            };

            Subscriptions = new List<string>
            {
                //"Event/PresentationStarted",
                //"Event/PresentationStopped",
                "Status/Conference/Presentation",             
            };

            SharingContentIsOnFeedback = new BoolFeedback("SharingActive", () => presentations.Any());
            SharingSourceFeedback = new StringFeedback("SharingSource", () =>
            {
                var firstPresentation = presentations.Values.FirstOrDefault();
                return firstPresentation != null
                    ? firstPresentation.LocalSource
                    : "None";
            });
            ReceivingContent = new BoolFeedback("IsReceivingContent", () => isReceivingContent);

            SharingSourceIntFeedback = new IntFeedback(() =>
            {
                if (presentations.DefaultIfEmpty() == null)
                {
                    return 0;
                }

                switch (SharingSourceFeedback.StringValue)
                {
                    case "2":
                        return 2;
                    case "3":
                        return 3;
                    case "Airplay":
                        return 4;
                    default:
                        return 0;
                }
            });

            SharingSourceFeedback.RegisterForDebug(parent);
            SharingContentIsOnFeedback.RegisterForDebug(parent);
            ReceivingContent.RegisterForDebug(parent);

            AutoShareContentWhileInCall = false;
        }

        public IEnumerable<string> Polls { get; private set; }

        public IEnumerable<string> Subscriptions { get; private set; }

        public bool HandlesResponse(string response)
        {
            // *s Conference Presentation Mode: Sending
            return response.IndexOf("*s Conference Presentation ", StringComparison.Ordinal) > -1;
        }

        public void HandleResponse(string response)
        {
            if (response.StartsWith("*s Conference Presentation LocalInstance "))
            {
                isReceivingContent = false;
                ParseLocalInstanceResponse(response);
            }
            else if (response == "*s Conference Presentation Mode: Receiving")
            {
                isReceivingContent = true;
                presentations.Clear();
            }
            else if (response == "*s Conference Presentation Mode: Off")
            {
                isReceivingContent = false;
                presentations.Clear();
            }
            else
            {

            }

            ReceivingContent.FireUpdate();
            SharingContentIsOnFeedback.FireUpdate();
            SharingSourceFeedback.FireUpdate();
            SharingSourceIntFeedback.FireUpdate();
        }

        private void ParseLocalInstanceResponse(string response)
        {
            const string pattern = @"LocalInstance (\d+)";
            var parts = response.Split('|');
            var firstLine = parts[0];

            var match = Regex.Match(firstLine, pattern);
            if (match.Success)
            {
                var localInstance = Convert.ToInt32(match.Groups[1].Value);
                if (firstLine.Contains("(ghost=True)"))
                {
                    presentations.Remove(localInstance);
                }
                else
                {
                    CiscoPresentationInfo info;
                    if (!presentations.TryGetValue(localInstance, out info))
                    {
                        info = new CiscoPresentationInfo {LocalInstance = localInstance};
                        presentations.Add(localInstance, info);
                    }

                    foreach (var line in parts)
                    {
                        if (line.Contains("SendingMode: LocalRemote"))
                        {
                            info.SendingMode = "LocalRemote";
                        }
                        else if (line.Contains("SendingMode: Local"))
                        {
                            info.SendingMode = "Local";
                        }
                        else if (line.Contains("Source: 2"))
                        {
                            info.LocalSource = "2";
                        }
                        else if (line.Contains("Source: 3"))
                        {
                            info.LocalSource = "3";
                        }
                    }
                }
            }
        }

        public void StartSharing()
        {
            // xCommand Presentation Start ConnectorId: value Instance: value Layout: value PresentationSource: value SendingMode: value
            // SendingMode
            // LocalRemote, LocalOnly Default: LocalRemote

            var command = "";

            if (string.IsNullOrEmpty(shareSourceId))
            {
                command = string.Format("xCommand Presentation Start SendingMode: {0}",
                    defaultToLocalOnly ? "LocalOnly" : "LocalRemote");
            }
            else
            {
                command = string.Format("xCommand Presentation Start PresentationSource: {0} SendingMode: {1}",
                    shareSourceId, defaultToLocalOnly ? "LocalOnly" : "LocalRemote");
            }

            parent.SendText(command);
        }

        public void StopSharing()
        {
            const string command = "xCommand Presentation Stop";
            parent.SendText(command);
        }

        public void SetShareSource(int sourceId)
        {
            Debug.Console(1, parent, "Attempting to set share source to:{0}", sourceId);

            if (sourceId > 0 && sourceId < 4)
            {
                shareSourceId = Convert.ToString(sourceId);
            }
            else
            {
                Debug.Console(1, parent, "Invalid share source id:{0} || for now valid choices are 2 and 3", sourceId);
            }
        }

        public BoolFeedback SharingContentIsOnFeedback { get; private set; }
        public IntFeedback SharingSourceIntFeedback { get; private set; }
        public StringFeedback SharingSourceFeedback { get; private set; }
        public bool AutoShareContentWhileInCall { get; private set; }
        public BoolFeedback ReceivingContent { get; private set; }
    }
}