using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using PDT.Plugins.Cisco.RoomOs.V2;
using PepperDash.Core.Intersystem;
using PepperDash.Core.Intersystem.Tokens;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.Codec;
using PepperDash.Essentials.Devices.Common.VideoCodec;

namespace epi_videoCodec_ciscoExtended.V2
{
    public class CiscoCallStatusJoinMap : JoinMapBaseAdvanced
    {


        public CiscoCallStatusJoinMap(uint joinStart)
            : base(joinStart, typeof(CiscoCallStatusJoinMap))
        {
        }
    }
    public class CiscoCallStatus : CiscoRoomOsFeature, IJoinCalls, IHasCallHold, IHasDialer
    {
        private readonly CCriticalSection activeCallItemsSync = new CCriticalSection();

        private readonly IDictionary<string, CodecActiveCallItem> activeCallItems =
            new Dictionary<string, CodecActiveCallItem>();

        private readonly CiscoRoomOsDevice parent;

        private StringFeedback callStatusXsig;

        private static readonly List<string> PollStrings = new List<string>
        {
            "xStatus Call"
        };

        private static readonly List<string> EventSubscriptions = new List<string>
        {
            "/Status/Call"
        };

        public CiscoCallStatus(CiscoRoomOsDevice parent)
            : base(parent.Key + "-calls")
        {
            this.parent = parent;
            callStatusXsig = new StringFeedback(UpdateCallStatusXSig);
        }

        public override IEnumerable<string> Polls
        {
            get { return PollStrings; }
        }

        public override IEnumerable<string> Subscriptions
        {
            get { return EventSubscriptions; }
        }

        public override bool HandlesResponse(string response)
        {
            return response.IndexOf("*s Call", StringComparison.Ordinal) > -1;
        }

        public override void HandleResponse(string response)
        {
            const string pattern = @"\*s Call (\d+) (\w+ ?\w*): (.*)";
            const string ghostPattern = @"\*s Call (\d+) \(ghost=(True|False)\):";

            var changedCallItems = new Dictionary<string, CodecActiveCallItem>();

            activeCallItemsSync.Enter();
            try
            {
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
                        var property = match.Groups[2].Value;
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
                                    : (eCodecCallStatus)Enum.Parse(typeof (eCodecCallStatus), value, true);
                                break;
                            case "Direction":
                                activeCallItems[callId].Direction =
                                    (eCodecCallDirection)Enum.Parse(typeof(eCodecCallDirection), value, true);
                                break;
                            case "PlacedOnHold":
                                activeCallItems[callId].IsOnHold = bool.Parse(value);
                                break;
                        }

                        if (!changedCallItems.ContainsKey(callId))
                        {
                            changedCallItems.Add(callId, activeCall);
                        }
                    }
                }

                callStatusXsig.FireUpdate();
            }
            finally
            {
                activeCallItemsSync.Leave();
            }

            var handler = CallStatusChange;
            if (handler != null)
            {
                foreach (var callItem in changedCallItems.Values)
                {  
                    handler(parent, new CodecCallStatusItemChangeEventArgs(callItem));
                }
            }
        }

        public void JoinCall(CodecActiveCallItem activeCall)
        {
            throw new NotImplementedException();
        }

        public void JoinAllCalls()
        {
            throw new NotImplementedException();
        }

        public void HoldCall(CodecActiveCallItem activeCall)
        {
            throw new NotImplementedException();
        }

        public void ResumeCall(CodecActiveCallItem activeCall)
        {
            throw new NotImplementedException();
        }

        private string UpdateCallStatusXSig()
        {
            const int maxCalls = 8;
            const int maxStrings = 6;
            const int maxDigitals = 2;
            const int offset = maxStrings + maxDigitals;
            var stringIndex = 0;
            var digitalIndex = maxStrings * maxCalls;
            var arrayIndex = 0;

            var tokenArray = new XSigToken[maxCalls * offset]; //set array size for number of calls * pieces of info

            foreach (var call in activeCallItems.Values)
            {
                if (arrayIndex >= maxCalls * offset)
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

            while (arrayIndex < maxCalls * offset)
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
            using (var tw = new XSigTokenStreamWriter(s, true))
            {
                tw.WriteXSigData(tokenArray);
                var xSig = s.ToArray();
                return Encoding.GetEncoding(XSigEncoding).GetString(xSig, 0, xSig.Length);
            }
        }

        public void Dial(string number)
        {
            throw new NotImplementedException();
        }

        public void EndCall(CodecActiveCallItem activeCall)
        {
            throw new NotImplementedException();
        }

        public void EndAllCalls()
        {
            throw new NotImplementedException();
        }

        public void AcceptCall(CodecActiveCallItem item)
        {
            throw new NotImplementedException();
        }

        public void RejectCall(CodecActiveCallItem item)
        {
            throw new NotImplementedException();
        }

        public void SendDtmf(string digit)
        {
            throw new NotImplementedException();
        }

        public bool IsInCall
        {
            get { return activeCallItems.Any(); }
        }

        public event EventHandler<CodecCallStatusItemChangeEventArgs> CallStatusChange;
    }
}