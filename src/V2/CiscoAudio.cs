using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace epi_videoCodec_ciscoExtended.V2
{
    /*xstatus audio
*s Audio Input Connectors HDMI 1 Mute: Off
*s Audio Input Connectors HDMI 2 Mute: Off
*s Audio Input Connectors HDMI 3 Mute: On
*s Audio Input Connectors Microphone 1 ConnectionStatus: Connected
*s Audio Input Connectors Microphone 1 EcReferenceDelay: 40
*s Audio Input Connectors Microphone 1 Mute: Off
*s Audio Input Connectors Microphone 2 ConnectionStatus: NotConnected
*s Audio Input Connectors Microphone 2 EcReferenceDelay: 0
*s Audio Input Connectors Microphone 2 Mute: Off
*s Audio Input Connectors Microphone 3 ConnectionStatus: NotConnected
*s Audio Input Connectors Microphone 3 EcReferenceDelay: 0
*s Audio Input Connectors Microphone 3 Mute: Off
*s Audio Microphones MusicMode: Off
*s Audio Microphones Mute: Off
*s Audio Microphones NoiseRemoval: On
*s Audio Output Connectors ARC 1 DelayMs: 30
*s Audio Output Connectors ARC 1 Mode: On
*s Audio Output Connectors HDMI 1 DelayMs: 0
*s Audio Output Connectors HDMI 1 MicPassthrough: Off
*s Audio Output Connectors HDMI 1 Mode: Off
*s Audio Output Connectors HDMI 2 DelayMs: 0
*s Audio Output Connectors HDMI 2 MicPassthrough: Off
*s Audio Output Connectors HDMI 2 Mode: Off
*s Audio Output Connectors Line 1 ConnectionStatus: NotConnected
*s Audio Output Connectors Line 1 DelayMs: 30
*s Audio Output MeasuredHdmiArcDelay: 0
*s Audio Output MeasuredHdmiDelay: 30
*s Audio Output ReportedHdmiCecDelay: 0
*s Audio Ultrasound Volume: 70
*s Audio Volume: 0
*s Audio VolumeMute: Off
** end
     * *s Audio Microphones Mute: Off
*/

    public class CiscoAudio : CiscoRoomOsFeature, IHasPolls, IHasEventSubscriptions, IHandlesResponses, IBasicVolumeWithFeedback, IPrivacy
    {
        private readonly CiscoRoomOsDevice parent;
        private bool privacyIsOn;
        private bool muteIsOn;
        private int level;

        public CiscoAudio(CiscoRoomOsDevice parent) : base(parent.Key + "-audio")
        {
            this.parent = parent;

            Subscriptions = new List<string>
            {
                "Status/Audio/Volume",
                "Status/Audio/VolumeMute",
                "Status/Audio/Microphones"                
            };

            Polls = new List<string>
            {
                "xStatus Audio VolumeMute",
                "xStatus Audio Volume"
            };

            MuteFeedback = new BoolFeedback("MuteIsOn", () => muteIsOn);
            VolumeLevelFeedback = new IntFeedback("CurrentVolume", () => CrestronEnvironment.ScaleWithLimits(level, 100, 0, 65535, 0));
            PrivacyModeIsOnFeedback = new BoolFeedback("Privacy", () => privacyIsOn);

            MuteFeedback.RegisterForDebug(parent);
            VolumeLevelFeedback.RegisterForDebug(parent); ;
            PrivacyModeIsOnFeedback.RegisterForDebug(parent);
        }

        public IEnumerable<string> Polls { get; private set; }
        public IEnumerable<string> Subscriptions { get; private set; }

        public bool HandlesResponse(string response)
        {
            return response.IndexOf("*s Audio", StringComparison.Ordinal) > -1;
        }

        public void HandleResponse(string response)
        {
            foreach (var part in response.Split('|').Where(p => p.Length > 0))
            {
                if (part == "*s Audio VolumeMute: Off")
                {
                    muteIsOn = false;
                    MuteFeedback.FireUpdate();
                }
                else if (part == "*s Audio VolumeMute: On")
                {
                    muteIsOn = true;
                    MuteFeedback.FireUpdate();
                }
                else if (part.StartsWith("*s Audio Volume:"))
                {
                    var result = part.Replace("*s Audio Volume:", "").Trim();
                    level = Convert.ToInt32(result);
                    VolumeLevelFeedback.FireUpdate();
                }
                else if (part == "*s Audio Microphones Mute: Off")
                {
                    privacyIsOn = false;
                    PrivacyModeIsOnFeedback.FireUpdate();
                }
                else if (part == "*s Audio Microphones Mute: On")
                {
                    privacyIsOn = true;
                    PrivacyModeIsOnFeedback.FireUpdate();
                }
                else
                {
                    Debug.Console(2, parent, "Unknown audio response:{0}", part);
                }
            }
        }

        public void VolumeUp(bool pressRelease)
        {
            if (pressRelease)
                parent.SendText("xCommand Audio Volume Increase");
        }

        public void VolumeDown(bool pressRelease)
        {
            if (pressRelease)
                parent.SendText("xCommand Audio Volume Decrease");
        }

        public void MuteToggle()
        {
            parent.SendText("xCommand Audio Volume ToggleMute");
        }

        public void MuteOn()
        {
            parent.SendText("xCommand Audio Volume Mute");
        }

        public void MuteOff()
        {
            parent.SendText("xCommand Audio Volume Unmute");
        }

        public void SetVolume(ushort level)
        {
            if (level == 0)
                return;

            var scaledLevel = CrestronEnvironment.ScaleWithLimits(level, 65535, 0, 100, 0);
            var command = "xCommand Audio Volume Set Level: " + scaledLevel;
            parent.SendText(command);
        }

        public BoolFeedback MuteFeedback { get; private set; }
        public IntFeedback VolumeLevelFeedback { get; private set; }

        public void PrivacyModeOn()
        {
            const string command = "xCommand Audio Microphones Mute";
            parent.SendText(command);
        }

        public void PrivacyModeOff()
        {
            const string command = "xCommand Audio Microphones Unmute";
            parent.SendText(command);
        }

        public void PrivacyModeToggle()
        {
            const string command = "xCommand Audio Microphones ToggleMute";
            parent.SendText(command);
        }

        public BoolFeedback PrivacyModeIsOnFeedback { get; private set; }
    }
}