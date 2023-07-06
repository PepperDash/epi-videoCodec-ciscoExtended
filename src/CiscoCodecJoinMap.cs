using System;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges.JoinMaps;


namespace epi_videoCodec_ciscoExtended
{
    public class CiscoCodecJoinMap : VideoCodecControllerJoinMap
    {
        #region Digital


        [JoinName("PresenterTrackOff")]
        public JoinDataComplete PresenterTrackOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 134,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Presenter Track Off Get/Set",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("PresenterTrackFollow")]
        public JoinDataComplete PresenterTrackFollow = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 135,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Presenter Track Follow Get/Set",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("PresenterTrackBackground")]
        public JoinDataComplete PresenterTrackBackground = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 136,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Presenter Track Background Get/Set",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("PresenterTrackPersistent")]
        public JoinDataComplete PresenterTrackPersistent = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 137,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Presenter Track Persistent Get/Set",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("SpeakerTrackAvailable")]
        public JoinDataComplete SpeakerTrackAvailable = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 145,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Speaker Track Available",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("PresenterTrackAvailable")]
        public JoinDataComplete PresenterTrackAvailable = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 146,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Presenter Track Availble",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("DialActiveMeeting")]
        public JoinDataComplete DialActiveMeeting = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 159,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Presenter Track Availble",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });


        [JoinName("PresentationActive")]
        public JoinDataComplete PresentationActive = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 200,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Presentation Active",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });


        [JoinName("PresentationLocalOnly")]
        public JoinDataComplete PresentationLocalOnly = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 205,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Presentation Local Only Feedback",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("PresentationLocalRemote")]
        public JoinDataComplete PresentationLocalRemote = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 206,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Presentation Local and Remote Feedback",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("ResumeAllCalls")]
        public JoinDataComplete ResumeAllCalls = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 230,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Resume all held calls",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("PresentationLocalRemoteToggle")]
        public JoinDataComplete PresentationLocalRemoteToggle = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 207,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Presentation Local and Remote Feedback",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });



        [JoinName("ActivateDoNotDisturbMode")]
        public JoinDataComplete ActivateDoNotDisturbMode = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 241,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Activates Do Not Disturb Mode.  FB High if active.",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("DeactivateDoNotDisturbMode")]
        public JoinDataComplete DeactivateDoNotDisturbMode = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 242,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Deactivates Do Not Disturb Mode.  FB High if deactivated.",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("ToggleDoNotDisturbMode")]
        public JoinDataComplete ToggleDoNotDisturbMode = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 243,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Toggles Do Not Disturb Mode.",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("ActivateStandby")]
        public JoinDataComplete ActivateStandby = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 246,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Activates Standby Mode.  FB High if active.",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("DeactivateStandby")]
        public JoinDataComplete DeactivateStandby = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 247,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Deactivates Standby Mode.  FB High if deactivated.",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("ActivateHalfWakeMode")]
        public JoinDataComplete ActivateHalfWakeMode = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 248,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Activates Half Wake Mode.  FB High if active.",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("EnteringStandbyMode")]
        public JoinDataComplete EnteringStandbyMode = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 249,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "High to indicate that the codec is entering standby mode",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("CodecAvailable")]
        public JoinDataComplete CodecAvailable = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 251,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "High to indicate that the codec does not have any meetings currently active",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("CodecMeetingBannerActive")]
        public JoinDataComplete CodecMeetingBannerActive = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 252,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "High to indicate that the codec has currently active meetings",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("CodecMeetingBannerWarning")]
        public JoinDataComplete CodecMeetingBannerWarning = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 253,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "High to indicate that the codec has an impending meeting",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("PresentationViewDefault")]
        public JoinDataComplete PresentationViewDefault = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 261,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Set / Get PresentationView Default mode",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("PresentationViewMaximized")]
        public JoinDataComplete PresentationViewMaximized = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 262,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Set / Get PresentationView Maximized mode",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("PresentationViewMinimized")]
        public JoinDataComplete PresentationViewMinimized = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 263,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Set / Get PresentationView Minimized mode",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("WebexPinRequested")]
        public JoinDataComplete WebexPinRequested = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 311,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Webex Pin Requested",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("WebexJoinedAsHost")]
        public JoinDataComplete WebexJoinedAsHost = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 312,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "WebexJoinedAsHost",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("WebexJoinedAsGuest")]
        public JoinDataComplete WebexJoinedAsGuest = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 313,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "WebexJoinedAsGuest",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("WebexPinError")]
        public JoinDataComplete WebexPinError = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 314,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "WebexPinError",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("WebexSendPin")]
        public JoinDataComplete WebexSendPin = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 311,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "WebexSendPin",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.DigitalSerial
            });

        [JoinName("WebexJoinAsGuest")]
        public JoinDataComplete WebexJoinAsGuest = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 312,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "WebexJoinAsGuest",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("WebexPinClear")]
        public JoinDataComplete WebexPinClear = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 313,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "WebexPinClear",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        #endregion


        #region Analog

        [JoinName("RingtoneVolume")]
        public JoinDataComplete RingtoneVolume = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 21,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Ringtone volume set/FB.  Valid values are 0 - 100 in increments of 5 (5, 10, 15, 20, etc.)",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("TotalMinutesUntilMeeting")]
        public JoinDataComplete TotalMinutesUntilMeeting = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 42,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Total minutes until next meeting",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });


        [JoinName("HoursUntilMeeting")]
        public JoinDataComplete HoursUntilMeeting = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 43,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Hours until next meeting",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });
        [JoinName("MinutesUntilMeeting")]
        public JoinDataComplete MinutesUntilMeeting = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 44,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Minutes until next meeting",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("PresentationSource")]
        public JoinDataComplete PresentationSource = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 201,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Presentation set/FB.  Valid values are 0 - 6 depending on the codec model.",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("ZoomMeetingID")]
        public JoinDataComplete ZoomMeetingId = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 401,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Zoom Meeting ID for Room Connector",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("ZoomMeetingPasscode")]
        public JoinDataComplete ZoomMeetingPasscode = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 402,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Zoom Meeting Passcode for Room Connector",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("ZoomMeetingCommand")]
        public JoinDataComplete ZoomMeetingCommand = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 403,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Zoom Meeting Command for Room Connector",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("ZoomMeetingHostKey")]
        public JoinDataComplete ZoomMeetingHostKey = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 404,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Zoom Meeting Host Key for Room Connector",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("ZoomMeetingReservedCode")]
        public JoinDataComplete ZoomMeetingReservedCode = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 405,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Zoom Meeting Reserved Code for Room Connector",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("ZoomMeetingDialCode")]
        public JoinDataComplete ZoomMeetingDialCode = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 406,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Zoom Meeting Dial Code for Room Connector",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("ZoomMeetingIP")]
        public JoinDataComplete ZoomMeetingIp = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 407,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Zoom Meeting IP Address for Room Connector",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });
        [JoinName("ZoomMeetingDial")]
        public JoinDataComplete ZoomMeetingDial = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 401,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Zoom Meeting Dial",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("ZoomMeetingClear")]
        public JoinDataComplete ZoomMeetingClear = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 402,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Zoom Meeting Clear Data",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("WebexDial")]
        public JoinDataComplete WebexDial = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 411,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Dial Webex Meeting",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("WebexDialClear")]
        public JoinDataComplete WebexDialClear = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 412,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Clear Webex Dialer Data",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });
        [JoinName("WebexMeetingNumber")]
        public JoinDataComplete WebexMeetingNumber = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 411,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Set Webex Meeting Number",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });
        [JoinName("WebexMeetingPin")]
        public JoinDataComplete WebexMeetingPin = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 412,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Set Webex Meeting Pin",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });
        [JoinName("WebexMeetingRole")]
        public JoinDataComplete WebexMeetingRole = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 413,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Set Webex Meeting Role",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });





        #endregion


        #region Serials

        [JoinName("WidgetEventData")]
        public JoinDataComplete WidgetEventData = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 321,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Receive/update widget in Cisco Extension editor format, e.g., \"/blinds /pressed /increment\", \"/blinds /closed\"",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Serial
            });


        [JoinName("CommandToDevice")]
        public JoinDataComplete CommandToDevice = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 5,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Sends a serial command to the device.  Do not include the delimiter, it will be added automatically.",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });
        [JoinName("ActiveMeetingData")]
        public JoinDataComplete ActiveMeetingDataXSig = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 104,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "XSig Containing Data for Active Meeting",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });
        [JoinName("AvailableTimeRemaining")]
        public JoinDataComplete AvailableTimeRemaining = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 105,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Formatted String Showing Time until room no longer available" +
                              "",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });
        [JoinName("TimeToNextMeeting")]
        public JoinDataComplete TimeToNextMeeting = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 106,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Formatted String Showing Time to next meeting" +
                              "",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });





        #endregion

        public CiscoCodecJoinMap(uint joinStart)
            : base(joinStart, typeof(CiscoCodecJoinMap))
        {
        }

        public CiscoCodecJoinMap(uint joinStart, Type type)
            : base(joinStart, type)
        {
        }
    }
}