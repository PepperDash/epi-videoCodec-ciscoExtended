using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Crestron.SimplSharp.CrestronIO;
using PepperDash.Core;
using PepperDash.Core.Intersystem;
using PepperDash.Core.Intersystem.Tokens;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.Codec;
using PepperDash.Essentials.Devices.Common.VideoCodec;

namespace epi_videoCodec_ciscoExtended.V2
{
    public class CiscoCallStatus : CiscoRoomOsFeature, IJoinCalls, IHasCallHold, IHasDialer, IHasPolls,
        IHasEventSubscriptions, IHandlesResponses
    {
        private readonly IDictionary<string, CodecActiveCallItem> activeCallItems;

        private readonly CiscoRoomOsDevice parent;
        public readonly StringFeedback CallStatusXSig;

        public IEnumerable<CodecActiveCallItem> ActiveCalls
        {
            get { return activeCallItems.Values.ToList(); }
        }

        private static readonly List<string> PollStrings = new List<string>
        {
            "xStatus Call"
        };

        private static readonly List<string> EventSubscriptions = new List<string>
        {
            "Status/Call"
        };

        public CiscoCallStatus(CiscoRoomOsDevice parent)
            : base(parent.Key + "-calls")
        {
            this.parent = parent;
            activeCallItems = new Dictionary<string, CodecActiveCallItem>();

            CallStatusXSig = new StringFeedback(UpdateCallStatusXSig);
            NumberOfActiveCalls = new IntFeedback("NumberOfCalls", () => activeCallItems.Count);
            CallIsConnectedOrConnecting = new BoolFeedback("CallIsConnected/Connecting", () => activeCallItems.Any());
            CallIsIncoming = new BoolFeedback("CallIncoming", () => activeCallItems.Any(item => item.Value.Direction == eCodecCallDirection.Incoming));
            IncomingCallName = new StringFeedback("IncomingCallName", () =>
            {
                var incoming =
                    activeCallItems.Values.FirstOrDefault(item => item.Direction == eCodecCallDirection.Incoming);

                return incoming == null ? string.Empty : incoming.Name;
            });

            IncomingCallNumber = new StringFeedback("IncomingCallNumber", () =>
            {
                var incoming =
                    activeCallItems.Values.FirstOrDefault(item => item.Direction == eCodecCallDirection.Incoming);

                return incoming == null ? string.Empty : incoming.Number;
            });

            CallIsIncoming.RegisterForDebug(parent);
            NumberOfActiveCalls.RegisterForDebug(parent);
            CallIsConnectedOrConnecting.RegisterForDebug(parent);
            IncomingCallName.RegisterForDebug(parent);
            IncomingCallNumber.RegisterForDebug(parent);

            CallStatusChange +=
                (sender, args) =>
                    Debug.Console(1, parent, "Call Status Change:{0} {1}", args.CallItem.Name, args.CallItem.Status);
        }

        public readonly BoolFeedback CallIsIncoming;
        public readonly IntFeedback NumberOfActiveCalls;
        public readonly BoolFeedback CallIsConnectedOrConnecting;
        public readonly StringFeedback IncomingCallName;
        public readonly StringFeedback IncomingCallNumber;

        public IEnumerable<string> Polls
        {
            get { return PollStrings; }
        }

        public IEnumerable<string> Subscriptions
        {
            get { return EventSubscriptions; }
        }

        public bool HandlesResponse(string response)
        {
            return response.IndexOf("*s Call", StringComparison.Ordinal) > -1;
        }

        public void HandleResponse(string response)
        {
            const string pattern = @"\*s Call (\d+) (\w+ ?\w*): (.*)";
            const string ghostPattern = @"\*s Call (\d+) \(ghost=(True|False)\):";

            var changedCallItems = new Dictionary<string, CodecActiveCallItem>();

            foreach (var line in response.Split('|'))
            {
                var ghostMatch = Regex.Match(line, ghostPattern);
                if (ghostMatch.Success)
                {
                    var callId = ghostMatch.Groups[1].Value;
                    var isGhost = bool.Parse(ghostMatch.Groups[2].Value);

                    CodecActiveCallItem activeCall;

                    if (isGhost && activeCallItems.TryGetValue(callId, out activeCall))
                    {
                        activeCallItems.Remove(callId);
                        activeCall.Status = eCodecCallStatus.Disconnected;

                        if (!changedCallItems.ContainsKey(callId))
                        {
                            changedCallItems.Add(callId, activeCall);
                        }
                    }

                    continue;
                }

                var match = Regex.Match(line, pattern);
                if (match.Success)
                {
                    var callId = match.Groups[1].Value;
                    var property = match.Groups[2].Value.Trim(new[] { ' ', '\"' });
                    var value = match.Groups[3].Value;

                    CodecActiveCallItem activeCall;

                    if (!activeCallItems.TryGetValue(callId, out activeCall))
                    {
                        activeCall = new CodecActiveCallItem
                        {
                            Id = callId
                        };

                        activeCallItems.Add(callId, activeCall);
                    }

                    try
                    {
                        switch (property)
                        {
                            case "DisplayName":
                                activeCallItems[callId].Name = value;
                                break;
                            case "RemoteNumber":
                                activeCallItems[callId].Number = value;
                                break;
                            case "CallType":
                                activeCallItems[callId].Type =
                                    (eCodecCallType)Enum.Parse(typeof(eCodecCallType), value, true);
                                break;
                            case "Status":
                                activeCallItems[callId].Status =
                                    value == "Dialling"
                                        ? eCodecCallStatus.Dialing
                                        : (eCodecCallStatus)Enum.Parse(typeof(eCodecCallStatus), value, true);
                                break;
                            case "Direction":
                                activeCallItems[callId].Direction =
                                    (eCodecCallDirection)Enum.Parse(typeof(eCodecCallDirection), value, true);
                                break;
                            case "PlacedOnHold":
                                activeCallItems[callId].IsOnHold = bool.Parse(value);
                                break;
                            default:
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Console(0, this, "Failed parsing the call status:{0}", ex);
                    }

                    if (!changedCallItems.ContainsKey(callId))
                    {
                        changedCallItems.Add(callId, activeCall);
                    }
                }
            }

            CallIsIncoming.FireUpdate();
            CallIsConnectedOrConnecting.FireUpdate();
            NumberOfActiveCalls.FireUpdate();
            IncomingCallName.FireUpdate();
            IncomingCallNumber.FireUpdate();
            CallStatusXSig.FireUpdate();

            var handler = CallStatusChange;
            if (handler == null) return;

            foreach (var callItem in changedCallItems.Values)
            {
                handler(parent, new CodecCallStatusItemChangeEventArgs(callItem));
            }
        }

        public void JoinCall(CodecActiveCallItem activeCall)
        {
            var command = "xCommand Call Join CallId:" + activeCall.Id;

            var ids = activeCallItems
                .Values
                .Aggregate(new StringBuilder(), (builder, item) =>
            {
                if (item.IsActiveCall)
                    builder.Append(" CallId:{0}" + item.Id);

                return builder;
            });

            parent.SendText(command + ids);
        }

        public void JoinAllCalls()
        {
            var ids = new StringBuilder();

            foreach (var call in activeCallItems.Values)
            {
                ids.Append(string.Format(" CallId:{0}", call.Id));
            }

            if (ids.Length <= 0) return;

            var command = "xCommand Call Join" + ids;
            parent.SendText(command);
        }

        public void HoldCall(CodecActiveCallItem activeCall)
        {
            var command = "xCommand Call Hold CallId: " + activeCall.Id;
            parent.SendText(command);
        }

        public void HoldAllCalls()
        {
            var ids = new StringBuilder();

            foreach (var call in activeCallItems.Values)
            {
                ids.Append(string.Format(" CallId:{0}", call.Id));
            }

            if (ids.Length <= 0) return;

            var command = "xCommand Call Hold" + ids;
            parent.SendText(command);
        }

        public void ResumeCall(CodecActiveCallItem activeCall)
        {
            var command = "xCommand Call Resume CallId: " + activeCall.Id;
            parent.SendText(command);
        }

        public void ResumeAllCalls()
        {
            var ids = new StringBuilder();

            foreach (var call in activeCallItems.Values)
            {
                ids.Append(string.Format(" CallId:{0}", call.Id));
            }

            if (ids.Length <= 0) return;

            var command = "xCommand Call Resume" + ids;
            parent.SendText(command);
        }

        private string UpdateCallStatusXSig()
        {
            const int maxCalls = 8;
            const int maxStrings = 6;
            const int maxDigitals = 2;
            const int offset = maxStrings + maxDigitals;
            var stringIndex = 0;
            var digitalIndex = maxStrings*maxCalls;
            var arrayIndex = 0;

            var tokenArray = new XSigToken[maxCalls*offset]; //set array size for number of calls * pieces of info

            foreach (var call in activeCallItems.Values)
            {
                if (arrayIndex >= maxCalls*offset)
                    break;

                //digitals
                tokenArray[digitalIndex] = new XSigDigitalToken(digitalIndex + 1, call.IsActiveCall);
                tokenArray[digitalIndex + 1] = new XSigDigitalToken(digitalIndex + 2, call.IsOnHold);

                //serials
                tokenArray[stringIndex] = new XSigSerialToken(stringIndex + 1, call.Name ?? String.Empty);
                tokenArray[stringIndex + 1] = new XSigSerialToken(stringIndex + 2, call.Number ?? String.Empty);
                tokenArray[stringIndex + 2] = new XSigSerialToken(stringIndex + 3, call.Direction.ToString());
                tokenArray[stringIndex + 3] = new XSigSerialToken(stringIndex + 4, call.Type.ToString());
                tokenArray[stringIndex + 4] = new XSigSerialToken(stringIndex + 5, call.Status.ToString());
                // May need to verify correct string format here
                var dur = string.Format("{0:c}", call.Duration);
                tokenArray[arrayIndex + 6] = new XSigSerialToken(stringIndex + 6, dur);

                arrayIndex += offset;
                stringIndex += maxStrings;
                digitalIndex += maxDigitals;
            }

            while (arrayIndex < maxCalls*offset)
            {
                //digitals
                tokenArray[digitalIndex] = new XSigDigitalToken(digitalIndex + 1, false);
                tokenArray[digitalIndex + 1] = new XSigDigitalToken(digitalIndex + 2, false);

                //serials
                tokenArray[stringIndex] = new XSigSerialToken(stringIndex + 1, String.Empty);
                tokenArray[stringIndex + 1] = new XSigSerialToken(stringIndex + 2, String.Empty);
                tokenArray[stringIndex + 2] = new XSigSerialToken(stringIndex + 3, String.Empty);
                tokenArray[stringIndex + 3] = new XSigSerialToken(stringIndex + 4, String.Empty);
                tokenArray[stringIndex + 4] = new XSigSerialToken(stringIndex + 5, String.Empty);
                tokenArray[stringIndex + 5] = new XSigSerialToken(stringIndex + 6, String.Empty);

                arrayIndex += offset;
                stringIndex += maxStrings;
                digitalIndex += maxDigitals;
            }

            return GetXSigString(tokenArray);
        }

        private const int XSigEncoding = 28591;

        private static string GetXSigString(XSigToken[] tokenArray)
        {
            using (var s = new MemoryStream())
            using (var tw = new XSigTokenStreamWriter(s, false))
            {
                tw.WriteXSigData(tokenArray);
                var xSig = s.ToArray();
                return Encoding.GetEncoding(XSigEncoding).GetString(xSig, 0, xSig.Length);
            }
        }

        public void Dial(string number)
        {
            const string format = "xCommand Dial Number: \"{0}\"";
            var command = string.Format(format, number);
            parent.SendText(command);
        }

        public void EndCall(CodecActiveCallItem activeCall)
        {
            var command = "xCommand Call Disconnect CallId: " + activeCall.Id;
            parent.SendText(command);
        }

        public void EndAllCalls()
        {
            foreach (var callItem in activeCallItems.Values)
            {
                EndCall(callItem);
            }
        }

        public void AcceptCall(CodecActiveCallItem item)
        {
            parent.SendText("xCommand Call Accept CallId: " + item.Id);
        }

        public void AcceptCall()
        {
            parent.SendText("xCommand Call Accept");
        }

        public void RejectCall(CodecActiveCallItem item)
        {
            parent.SendText("xCommand Call Reject CallId:" + item.Id);
        }

        public void RejectCall()
        {
            parent.SendText("xCommand Call Reject");
        }

        public void SendDtmf(string digit)
        {
            var activeCall = GetActiveCallId();
            const string format = "xCommand Call DTMFSend CallId: {0} DTMFString: \"{1}\"";
            var command = string.Format(format, activeCall, digit);
            parent.SendText(command);
        }

        public string GetActiveCallId()
        {
            var calls = ActiveCalls.ToList();
            if (calls.Count <= 1) 
                return calls.Count == 1 ? calls[0].Id : string.Empty;

            var lastCallIndex = calls.Count - 1;
            return calls[lastCallIndex].Id;
        }

        public bool IsInCall
        {
            get { return activeCallItems.Any(); }
        }

        public event EventHandler<CodecCallStatusItemChangeEventArgs> CallStatusChange;
    }
}