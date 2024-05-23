using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.VideoCodec;

namespace epi_videoCodec_ciscoExtended.V2
{
    public class CiscoStandby : CiscoRoomOsFeature, IHasHalfWakeMode, IHasPolls, IHasEventSubscriptions, IHandlesResponses 
    {
        private string standbyState;
        private readonly CiscoRoomOsDevice parent;

        private static readonly List<string> PollStrings = new List<string>
        {
            "xStatus Standby"            
        };

        private static readonly List<string> EventSubscriptions = new List<string>
        {
            "Status/Standby/State"            
        };

        public CiscoStandby(CiscoRoomOsDevice parent)
            : base(parent.Key + "-standby")
        {
            this.parent = parent;

            StandbyIsOnFeedback = new BoolFeedback("Standby", () => standbyState != null && standbyState != "Off");
            HalfWakeModeIsOnFeedback = new BoolFeedback("HalfWake", () => standbyState == "Halfwake");
            EnteringStandbyModeFeedback = new BoolFeedback("Entering Standby", () => standbyState == "EnteringStandby");

            StandbyIsOnFeedback.RegisterForDebug(parent);
            HalfWakeModeIsOnFeedback.RegisterForDebug(parent);
            EnteringStandbyModeFeedback.RegisterForDebug(parent);
        }

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
            return response.StartsWith("*s Standby State");
        }

        public void HandleResponse(string response)
        {
            const string pattern = @"Standby State:\s*(?<state>\w+)";
            var match = Regex.Match(response, pattern);

            if (match.Success)
            {
                standbyState = match.Groups["state"].Value;

                StandbyIsOnFeedback.FireUpdate();
                HalfWakeModeIsOnFeedback.FireUpdate();
                EnteringStandbyModeFeedback.FireUpdate();
            }
        }

        public void StandbyActivate()
        {
            parent.SendText("xCommand Standby Activate");
        }

        public void StandbyDeactivate()
        {
            parent.SendText("xCommand Standby Deactivate");
        }

        public void HalfwakeActivate()
        {
            parent.SendText("xCommand Standby Halfwake");
        }

        public BoolFeedback StandbyIsOnFeedback { get; private set; }

        public BoolFeedback HalfWakeModeIsOnFeedback { get; private set; }

        public BoolFeedback EnteringStandbyModeFeedback { get; private set; }
    }
}