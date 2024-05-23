using PepperDash.Essentials.Core;

namespace epi_videoCodec_ciscoExtended.V2
{
    public class CiscoJoinMap : JoinMapBaseAdvanced
    {
        [JoinName("IsOnline")] public JoinDataComplete IsOnline = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "IsOnline",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("DtmfJoins")] public JoinDataComplete DtmfJoins = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 11,
                JoinSpan = 10
            },
            new JoinMetadata
            {
                Description = "DtmfJoins",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("DtmfStar")] public JoinDataComplete DtmfStar = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 21,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DtmfStar",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("DtmfPound")] public JoinDataComplete DtmfPound = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 22,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DtmfPound",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("NumberOfActiveCalls")] public JoinDataComplete NumberOfActiveCalls = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 25,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NumberOfActiveCalls",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("EndAllCalls")] public JoinDataComplete EndAllCalls = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 24,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "EndAllCalls",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("CallIsConnectedOrConnecting")] public JoinDataComplete CallIsConnectedOrConnecting = new JoinDataComplete
            (
            new JoinData
            {
                JoinNumber = 31,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "CallIsConnectedOrConnecting",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("CallIsIncoming")] public JoinDataComplete CallIsIncoming = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 50,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "CallIsIncoming",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("AnswerIncoming")] public JoinDataComplete AnswerIncoming = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 51,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "AnswerIncoming",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("RejectIncoming")] public JoinDataComplete RejectIncoming = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 52,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "RejectIncoming",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("IncomingName")] public JoinDataComplete IncomingName = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 51,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "IncomingName",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("IncomingNumber")] public JoinDataComplete IncomingNumber = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 52,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "IncomingNumber",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("HangUpCall")] public JoinDataComplete HangUpCall = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 81,
                JoinSpan = 8
            },
            new JoinMetadata
            {
                Description = "HangUpCall",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("JoinAllCalls")] public JoinDataComplete JoinAllCalls = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 90,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "JoinAllCalls",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("JoinCall")] public JoinDataComplete JoinCall = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 91,
                JoinSpan = 8
            },
            new JoinMetadata
            {
                Description = "JoinCall",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("HoldAllCalls")] public JoinDataComplete HoldAllCalls = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 220,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "HoldAllCalls",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("HoldCall")] public JoinDataComplete HoldCall = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 221,
                JoinSpan = 8
            },
            new JoinMetadata
            {
                Description = "HoldCall",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("ResumeAllCalls")] public JoinDataComplete ResumeAllCalls = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 230,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "ResumeAllCalls",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("ResumeCall")] public JoinDataComplete ResumeCall = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 231,
                JoinSpan = 8
            },
            new JoinMetadata
            {
                Description = "JoinCall",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("NearEndPresentationSource")] public JoinDataComplete NearEndPresentationSource = new JoinDataComplete
            (
            new JoinData
            {
                JoinNumber = 201,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NearEndPresentationSource",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("NearEndPresentationStart")] public JoinDataComplete NearEndPresentationStart = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 201,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NearEndPresentationStart",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("NearEndPresentationStop")] public JoinDataComplete NearEndPresentationStop = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 202,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NearEndPresentationStop",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("StandbyOn")] public JoinDataComplete StandbyOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 246,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "StandbyOn",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("StandbyOff")] public JoinDataComplete StandbyOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 247,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "StandbyOff",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("EnteringStandby")] public JoinDataComplete EnteringStandby = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 248,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "EnteringStandby",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("SearchIsBusy")] public JoinDataComplete SearchIsBusy = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 100,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SearchIsBusy",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("MicMuteOn")] public JoinDataComplete MicMuteOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 171,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "MicMuteOn",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("MicMuteOff")] public JoinDataComplete MicMuteOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 172,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "MicMuteOff",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("MicMuteToggle")] public JoinDataComplete MicMuteToggle = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 173,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "MicMuteToggle",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("VolumeUp")] public JoinDataComplete VolumeUp = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 174,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "VolumeUp",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("VolumeDown")] public JoinDataComplete VolumeDown = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 175,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "VolumeDown",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("Volume")] public JoinDataComplete Volume = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 174,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Volume",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("VolumeMuteOn")] public JoinDataComplete VolumeMuteOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 176,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "VolumeMuteOn",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("VolumeMuteOff")] public JoinDataComplete VolumeMuteOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 177,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "VolumeMuteOff",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("VolumeMuteToggle")] public JoinDataComplete VolumeMuteToggle = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 178,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "VolumeMuteToggle",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("DoNotDisturbOn")] public JoinDataComplete DoNotDisturbOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 241,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DoNotDisturbOn",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("DoNotDisturbOff")] public JoinDataComplete DoNotDisturbOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 242,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DoNotDisturbOff",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("DoNotDisturbToggle")] public JoinDataComplete DoNotDisturbToggle = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 243,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DoNotDisturbToggle",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        /* We will probably need these one day
        [JoinName("SelfviewOn")]
        public JoinDataComplete SelfviewOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 241,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SelfviewOn",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("SelfviewOff")]
        public JoinDataComplete SelfviewOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 242,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SelfviewOff",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });
         * */

        [JoinName("SelfviewToggle")] public JoinDataComplete SelfviewToggle = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 141,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SelfviewToggle",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });


        [JoinName("NearEndCameraUp")] public JoinDataComplete NearEndCameraUp = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 111,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NearEndCameraUp",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("NearEndCameraDown")] public JoinDataComplete NearEndCameraDown = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 112,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NearEndCameraDown",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("NearEndCameraLeft")] public JoinDataComplete NearEndCameraLeft = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 113,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NearEndCameraLeft",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("NearEndCameraRight")] public JoinDataComplete NearEndCameraRight = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 114,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NearEndCameraRight",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("NearEndCameraZoomIn")] public JoinDataComplete NearEndCameraZoomIn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 115,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NearEndCameraZoomIn",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("NearEndCameraZoomOut")] public JoinDataComplete NearEndCameraZoomOut = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 116,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NearEndCameraUp",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("NearEndCameraFocusIn")] public JoinDataComplete NearEndCameraFocusIn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 117,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NearEndCameraFocusIn",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("NearEndCameraFocusOut")] public JoinDataComplete NearEndCameraFocusOut = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 121,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "NearEndCameraFocusOut",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });


        [JoinName("FarEndCameraUp")] public JoinDataComplete FarEndCameraUp = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 122,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "FarEndCameraUp",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("FarEndCameraDown")] public JoinDataComplete FarEndCameraDown = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 123,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "FarEndCameraDown",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("FarEndCameraLeft")] public JoinDataComplete FarEndCameraLeft = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 124,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "FarEndCameraLeft",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("FarEndCameraRight")] public JoinDataComplete FarEndCameraRight = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 125,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "FarEndCameraRight",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("FarEndCameraZoomIn")] public JoinDataComplete FarEndCameraZoomIn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 126,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "FarEndCameraZoomIn",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("FarEndCameraZoomOut")] public JoinDataComplete FarEndCameraZoomOut = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 127,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "FarEndCameraUp",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });


        [JoinName("SpeakerTrackEnabled")] public JoinDataComplete SpeakerTrackEnabled = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 131,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SpeakerTrackEnabled",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("SpeakerTrackDisabled")] public JoinDataComplete SpeakerTrackDisabled = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 132,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SpeakerTrackDisabled",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("SpeakerTrackAvailable")] public JoinDataComplete SpeakerTrackAvailable = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 143,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SpeakerTrackAvailable",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("ManualDial")] public JoinDataComplete ManualDial = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 71,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "ManualDial",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("DialNumber")] public JoinDataComplete DialNumber = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DialNumber",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("CallStatusXSig")] public JoinDataComplete CallStatusXSig = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "CallStatusXSig",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("DirectoryXSig")] public JoinDataComplete DirectoryXSig = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 101,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DirectoryXSig",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("SearchDirectory")] public JoinDataComplete SearchDirectory = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 100,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SearchDirectory",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("DirectoryNumberOfRows")] public JoinDataComplete DirectoryNumberOfRows = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 101,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DirectoryNumberOfRows",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("DirectorySelectContact")] public JoinDataComplete DirectorySelectContact = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 101,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DirectorySelectContact",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("SelectedContactName")] public JoinDataComplete SelectedContactName = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 102,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SelectedContactName",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("DirectorySelectContactMethod")] public JoinDataComplete DirectorySelectContactMethod = new JoinDataComplete
            (
            new JoinData
            {
                JoinNumber = 103,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DirectorySelectContactMethod",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("SelectedDirectoryItemNumberOfContactMethods")] public JoinDataComplete
            SelectedDirectoryItemNumberOfContactMethods = new JoinDataComplete(
                new JoinData
                {
                    JoinNumber = 102,
                    JoinSpan = 1
                },
                new JoinMetadata
                {
                    Description = "SelectedDirectoryItemNumberOfContactMethods",
                    JoinCapabilities = eJoinCapabilities.ToSIMPL,
                    JoinType = eJoinType.Analog
                });

        [JoinName("SelectedDirectoryItemContactMethodsXsig")] public JoinDataComplete
            SelectedDirectoryItemContactMethodsXsig = new JoinDataComplete(
                new JoinData
                {
                    JoinNumber = 103,
                    JoinSpan = 1
                },
                new JoinMetadata
                {
                    Description = "SelectedDirectoryItemContactMethodsXsig",
                    JoinCapabilities = eJoinCapabilities.ToSIMPL,
                    JoinType = eJoinType.Analog
                });

        [JoinName("SelectedContactNumber")] public JoinDataComplete SelectedContactNumber = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 104,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SelectedContactNumber",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("ClearPhonebookSearch")] public JoinDataComplete ClearPhonebookSearch = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 110,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "ClearPhonebookSearch",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("DialSelectedContact")] public JoinDataComplete DialSelectedContact = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 106,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "DialSelectedContact",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });


        [JoinName("SelectRecentCall")] public JoinDataComplete SelectRecentCall = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 180,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SelectRecentCall",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("SelectRecentName")] public JoinDataComplete SelectRecentName = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 171,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SelectRecentName",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("SelectRecentNumber")] public JoinDataComplete SelectRecentNumber = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 171,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "SelectRecentNumber",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("Recents")] public JoinDataComplete Recents = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 181,
                JoinSpan = 10
            },
            new JoinMetadata
            {
                Description = "Recents",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("CameraSelect")] public JoinDataComplete CameraSelect = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 60,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "CameraSelect",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("CameraPresetActivate")] public JoinDataComplete CameraPresetActivate = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 121,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "CameraPresetActivate",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("FarEndCameraPresetActivate")] public JoinDataComplete FarEndCameraPresetActivate = new JoinDataComplete
            (
            new JoinData
            {
                JoinNumber = 122,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "FarEndCameraPresetActivate",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("CameraPresetStore")] public JoinDataComplete CameraPresetStore = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 123,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "CameraPresetStore",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("Coms")] public JoinDataComplete Coms = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 5,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Coms",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Serial
            });


        [JoinName("ToggleLayout")]
        public JoinDataComplete ToggleLayout = new JoinDataComplete(new JoinData()
        {
            JoinNumber = 142,
            JoinSpan = 1
        }, new JoinMetadata()
        {
            Description = "Toggle to the next available layout",
            JoinCapabilities = eJoinCapabilities.FromSIMPL,
            JoinType = eJoinType.Digital
        });


        [JoinName("CurrentLayout")]
        public JoinDataComplete CurrentLayout =
            new JoinDataComplete(new JoinData()
            {
                JoinNumber = 141,
                JoinSpan = 1
            }, new JoinMetadata()
            {
                Description = "Current Layout Fb",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("AvailableLayouts")]
        public JoinDataComplete AvailableLayouts =
            new JoinDataComplete(new JoinData()
            {
                JoinNumber = 142,
                JoinSpan = 1
            }, new JoinMetadata()
            {
                Description = "xSig of all available layouts",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("SelectLayout")] public JoinDataComplete SelectLayout = new JoinDataComplete(new JoinData()
            {
                JoinNumber = 142,
                JoinSpan = 1
            }, new JoinMetadata()
            {
                Description = "Select Layout by string",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });

        public CiscoJoinMap(uint joinStart)
            : base(joinStart, typeof (CiscoJoinMap))
        {
        }
    }
}