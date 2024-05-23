using System;
using System.Collections.Generic;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.Codec;

namespace epi_videoCodec_ciscoExtended.V2
{
    public class CiscoDoNotDisturb : CiscoRoomOsFeature, IHasPolls, IHasEventSubscriptions, IHandlesResponses, IHasDoNotDisturbMode
    {
        private readonly CiscoRoomOsDevice parent;

        public CiscoDoNotDisturb(CiscoRoomOsDevice parent) : base(parent.Key + "-DND")
        {
            this.parent = parent;

            Polls = new List<string>
            {
                "xStatus Conference DoNotDisturb"
            };

            Subscriptions = new List<string>
            {
                "Status/Conference/DoNotDisturb"
            };

            DoNotDisturbModeIsOnFeedback = new BoolFeedback("DND", () => doNotDisturbIsOn);
            DoNotDisturbModeIsOnFeedback.RegisterForDebug(parent);
        }

        private bool doNotDisturbIsOn;

        public IEnumerable<string> Polls { get; private set; }
        public IEnumerable<string> Subscriptions { get; private set; }

        public bool HandlesResponse(string response)
        {
            return response.IndexOf("*s Conference DoNotDisturb", StringComparison.Ordinal) > -1;
        }

        public void HandleResponse(string response)
        {
            var parts = response.Split('|');
            foreach (var line in parts)
            {
                switch (line)
                {
                    case "*s Conference DoNotDisturb: Inactive":
                        doNotDisturbIsOn = false;
                        DoNotDisturbModeIsOnFeedback.FireUpdate();
                        break;
                    case "*s Conference DoNotDisturb: Active":
                        doNotDisturbIsOn = true;
                        DoNotDisturbModeIsOnFeedback.FireUpdate();
                        break;
                }
            }
        }

        public void ActivateDoNotDisturbMode()
        {
            parent.SendText("xCommand Conference DoNotDisturb Activate");
        }

        public void DeactivateDoNotDisturbMode()
        {
            parent.SendText("xCommand Conference DoNotDisturb Deactivate");
        }

        public void ToggleDoNotDisturbMode()
        {
            if (doNotDisturbIsOn)
            {
                DeactivateDoNotDisturbMode();
            }
            else
            {
                ActivateDoNotDisturbMode();
            }
        }

        public BoolFeedback DoNotDisturbModeIsOnFeedback { get; private set; }
    }
}