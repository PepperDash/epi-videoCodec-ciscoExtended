using System;
using System.Collections.Generic;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Devices.Common.VideoCodec;

namespace epi_videoCodec_ciscoExtended.V2
{
    /*
     *  *s Video Selfview FullscreenMode: Off
        *s Video Selfview Mode: Off
        *s Video Selfview OnMonitorRole: First
        *s Video Selfview PIPPosition: CenterRight
     */
    public class CiscoSelfView : CiscoRoomOsFeature, IHasPolls, IHasEventSubscriptions, IHandlesResponses, IHasSelfviewPosition, IHasCodecSelfView
    {
        private readonly CiscoRoomOsDevice parent;

        public CiscoSelfView(CiscoRoomOsDevice parent)
            : base(parent.Key + "-DND")
        {
            this.parent = parent;

            Polls = new List<string>
            {
                "xStatus Video Selfview"
            };

            Subscriptions = new List<string>
            {
                "Status/Video/Selfview"
            };

            SelfviewIsOnFeedback = new BoolFeedback("SelfView", () => selfViewIsOn);
            SelfviewIsFullscreen = new BoolFeedback("SelfViewFullscreen", () => selfViewIsFullScreen);
            SelfviewPipPositionFeedback = new StringFeedback("SelfViewPipPosition", () => "Not implemented");

            SelfviewIsOnFeedback.RegisterForDebug(parent);
            SelfviewIsFullscreen.RegisterForDebug(parent);

            parent.CallStatus.CallIsConnectedOrConnecting.OutputChange += (sender, args) =>
            {
                if (args.BoolValue && ShowSelfViewByDefault)
                {
                    SelfViewModeOn();
                }
            };
        }

        private bool selfViewIsOn;
        private bool selfViewIsFullScreen;

        public IEnumerable<string> Polls { get; private set; }
        public IEnumerable<string> Subscriptions { get; private set; }

        public bool HandlesResponse(string response)
        {
            return response.IndexOf("*s Video Selfview", StringComparison.Ordinal) > -1;
        }

        public void HandleResponse(string response)
        {
            var parts = response.Split('|');
            foreach (var line in parts)
            {
                switch (line)
                {
                    case "*s Video Selfview Mode: Off":
                        selfViewIsOn = false;
                        SelfviewIsOnFeedback.FireUpdate();
                        break;
                    case "*s Video Selfview Mode: On":
                        selfViewIsOn = true;
                        SelfviewIsOnFeedback.FireUpdate();
                        break;
                    case "*s Video Selfview FullscreenMode: Off":
                        selfViewIsFullScreen = false;
                        SelfviewIsFullscreen.FireUpdate();
                        break;
                    case "*s Video Selfview FullscreenMode: On":
                        selfViewIsFullScreen = true;
                        SelfviewIsFullscreen.FireUpdate();
                        break;
                }
            }
        }

        public void SelfviewPipPositionSet(CodecCommandWithLabel position)
        {
           
        }

        public void SelfviewPipPositionToggle()
        {
            // throw new NotImplementedException();
        }

        public StringFeedback SelfviewPipPositionFeedback { get; private set; }

        public void SelfviewFullSreenOn()
        {
            parent.SendText("xCommand Video Selfview Set FullscreenMode: On");
        }

        public void SelfviewFullSreenOff()
        {
            parent.SendText("xCommand Video Selfview Set FullscreenMode: Off");
        }

        public void SelfviewFullSreenToggle()
        {
            if (selfViewIsFullScreen)
            {
                SelfviewFullSreenOff();
            }
            else
            {
                SelfviewFullSreenOn();
            }
        }

        public BoolFeedback SelfviewIsFullscreen { get; private set; }

        public void SelfViewModeOn()
        {
            parent.SendText("xCommand Video Selfview Set Mode: On");
        }

        public void SelfViewModeOff()
        {
            parent.SendText("xCommand Video Selfview Set Mode: Off");
        }

        public void SelfViewModeToggle()
        {
            if (selfViewIsOn)
            {
                SelfViewModeOff();
            }
            else
            {
                SelfViewModeOn();
            }
        }

        public BoolFeedback SelfviewIsOnFeedback { get; private set; }

        public bool ShowSelfViewByDefault { get; private set; }
    }
}