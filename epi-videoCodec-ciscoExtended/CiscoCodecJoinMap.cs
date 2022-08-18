﻿using System;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges.JoinMaps;


namespace epi_videoCodec_ciscoExtended
{
    public class CiscoCodecJoinMap : VideoCodecControllerJoinMap
    {

        #region Digital
        [JoinName("PhoneBookClearSelected")]
        public JoinDataComplete PhoneBookClearSelected = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 110,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Clear Selected Entry and String from Search",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });


        [JoinName("PresenterTrackEnabled")]
        public JoinDataComplete PresenterTrackEnabled = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 130,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Presenter Track Enabled Feedback",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });


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

        [JoinName("DialMeeting4")]
        public JoinDataComplete DialMeeting4 = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 164,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Join fourth meeting",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("DialMeeting5")]
        public JoinDataComplete DialMeeting5 = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 165,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Join fifth meeting",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
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

        [JoinName("DirectorySelectRowFeedback")]
        public JoinDataComplete DirectorySelectRowFeedback = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 101,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Directory Select Row Feedback",
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


        #endregion


        #region Serials

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