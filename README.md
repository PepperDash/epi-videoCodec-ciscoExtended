# Cisco RoomOS Video Codec Plugin

## License

Provided under MIT license

## Cloning Instructions

After forking this repository into your own GitHub space, you can create a new repository using this one as the template.  Then you must install the necessary dependencies as indicated below.

## Dependencies

The [Essentials](https://github.com/PepperDash/Essentials) libraries are required. They referenced via nuget. You must have nuget.exe installed and in the `PATH` environment variable to use the following command. Nuget.exe is available at [nuget.org](https://dist.nuget.org/win-x86-commandline/latest/nuget.exe).

### Installing Dependencies

To install dependencies once nuget.exe is installed, run the following command from the root directory of your repository:
`nuget install .\packages.config -OutputDirectory .\packages -excludeVersion`.
To verify that the packages installed correctly, open the plugin solution in your repo and make sure that all references are found, then try and build it.

### Installing Different versions of PepperDash Core

If you need a different version of PepperDash Core, use the command `nuget install .\packages.config -OutputDirectory .\packages -excludeVersion -Version {versionToGet}`. Omitting the `-Version` option will pull the version indicated in the packages.config file.

### Instructions for Renaming Solution and Files

See the Task List in Visual Studio for a guide on how to start using the templage.  There is extensive inline documentation and examples as well.

For renaming instructions in particular, see the XML `remarks` tags on class definitions


# CiscoCodecJoinMap

## Digitals
| Join Number | Join Span | Description                                                                                                          | Type                | Capabilities |
| ----------- | --------- | -------------------------------------------------------------------------------------------------------------------- | ------------------- | ------------ |
| 1001        | 1         | Device is Online                                                                                                     | Digital             | ToSIMPL      |
| 1010        | 1         | If High, will send DTMF tones to the call set by SelectCall analog.  If low sends DTMF tones to last connected call. | Digital             | FromSIMPL    |
| 1011        | 1         | DTMF 1                                                                                                               | Digital             | FromSIMPL    |
| 1012        | 1         | DTMF 2                                                                                                               | Digital             | FromSIMPL    |
| 1013        | 1         | DTMF 3                                                                                                               | Digital             | FromSIMPL    |
| 1014        | 1         | DTMF 4                                                                                                               | Digital             | FromSIMPL    |
| 1015        | 1         | DTMF 5                                                                                                               | Digital             | FromSIMPL    |
| 1016        | 1         | DTMF 6                                                                                                               | Digital             | FromSIMPL    |
| 1017        | 1         | DTMF 7                                                                                                               | Digital             | FromSIMPL    |
| 1018        | 1         | DTMF 8                                                                                                               | Digital             | FromSIMPL    |
| 1019        | 1         | DTMF 9                                                                                                               | Digital             | FromSIMPL    |
| 1020        | 1         | DTMF 0                                                                                                               | Digital             | FromSIMPL    |
| 1021        | 1         | DTMF *                                                                                                               | Digital             | FromSIMPL    |
| 1022        | 1         | DTMF #                                                                                                               | Digital             | FromSIMPL    |
| 1024        | 1         | End All Calls                                                                                                        | Digital             | FromSIMPL    |
| 1031        | 1         | Current Hook State                                                                                                   | Digital             | ToSIMPL      |
| 1041        | 4         | Speed Dial                                                                                                           | Digital             | FromSIMPL    |
| 1050        | 1         | Incoming Call                                                                                                        | Digital             | ToSIMPL      |
| 1051        | 1         | Answer Incoming Call                                                                                                 | Digital             | FromSIMPL    |
| 1052        | 1         | Reject Incoming Call                                                                                                 | Digital             | FromSIMPL    |
| 1071        | 1         | Dial manual string specified by CurrentDialString serial join                                                        | Digital             | FromSIMPL    |
| 1072        | 1         | Dial Phone                                                                                                           | Digital             | FromSIMPL    |
| 1072        | 1         | Dial Phone                                                                                                           | Digital             | ToSIMPL      |
| 1073        | 1         | Hang Up Phone                                                                                                        | Digital             | FromSIMPL    |
| 1081        | 8         | End a specific call by call index.                                                                                   | Digital             | FromSIMPL    |
| 1090        | 1         | Join all calls                                                                                                       | Digital             | FromSIMPL    |
| 1091        | 8         | Join a specific call by call index.                                                                                  | Digital             | FromSIMPL    |
| 1100        | 1         | Directory Search Busy FB                                                                                             | Digital             | ToSIMPL      |
| 1101        | 1         | Directory Selected Entry Is Contact FB                                                                               | Digital             | ToSIMPL      |
| 1101        | 1         | Directory Line Selected FB                                                                                           | Digital             | FromSIMPL    |
| 1102        | 1         | Directory is on Root FB                                                                                              | Digital             | ToSIMPL      |
| 1103        | 1         | Directory has changed FB                                                                                             | Digital             | ToSIMPL      |
| 1104        | 1         | Go to Directory Root                                                                                                 | Digital             | FromSIMPL    |
| 1105        | 1         | Go back one directory level                                                                                          | Digital             | FromSIMPL    |
| 1106        | 1         | Dial selected directory line                                                                                         | Digital             | FromSIMPL    |
| 1107        | 1         | Set high to disable automatic dialing of a contact when selected                                                     | Digital             | FromSIMPL    |
| 1108        | 1         | Pulse to dial the selected contact method                                                                            | Digital             | FromSIMPL    |
| 1110        | 1         | Clear Selected Entry and String from Search                                                                          | Digital             | FromSIMPL    |
| 1111        | 1         | Camera Tilt Up                                                                                                       | Digital             | FromSIMPL    |
| 1112        | 1         | Camera Tilt Down                                                                                                     | Digital             | FromSIMPL    |
| 1113        | 1         | Camera Pan Left                                                                                                      | Digital             | FromSIMPL    |
| 1114        | 1         | Camera Pan Right                                                                                                     | Digital             | FromSIMPL    |
| 1115        | 1         | Camera Zoom In                                                                                                       | Digital             | FromSIMPL    |
| 1116        | 1         | Camera Zoom Out                                                                                                      | Digital             | FromSIMPL    |
| 1117        | 1         | Camera Focus Near                                                                                                    | Digital             | FromSIMPL    |
| 1118        | 1         | Camera Focus Far                                                                                                     | Digital             | FromSIMPL    |
| 1119        | 1         | Camera Auto Focus Trigger                                                                                            | Digital             | FromSIMPL    |
| 1121        | 1         | Pulse to save selected preset spcified by CameraPresetSelect analog join.  FB will pulse for 3s when preset saved.   | Digital             | ToFromSIMPL  |
| 1131        | 1         | Camera Mode Auto.  Enables camera auto tracking mode, with feedback                                                  | Digital             | ToFromSIMPL  |
| 1132        | 1         | Camera Mode Manual.  Disables camera auto tracking mode, with feedback                                               | Digital             | ToFromSIMPL  |
| 1133        | 1         | Camera Mode Off.  Disables camera video, with feedback. Works like video mute.                                       | Digital             | ToFromSIMPL  |
| 1134        | 1         | Presenter Track Off Get/Set                                                                                          | Digital             | ToFromSIMPL  |
| 1135        | 1         | Presenter Track Follow Get/Set                                                                                       | Digital             | ToFromSIMPL  |
| 1136        | 1         | Presenter Track Background Get/Set                                                                                   | Digital             | ToFromSIMPL  |
| 1137        | 1         | Presenter Track Persistent Get/Set                                                                                   | Digital             | ToFromSIMPL  |
| 1141        | 1         | Camera Self View Toggle/FB                                                                                           | Digital             | ToFromSIMPL  |
| 1142        | 1         | Camera Layout Toggle                                                                                                 | Digital             | FromSIMPL    |
| 1143        | 1         | Camera Supports Auto Mode FB                                                                                         | Digital             | ToSIMPL      |
| 1144        | 1         | Camera Supports Off Mode FB                                                                                          | Digital             | ToSIMPL      |
| 1145        | 1         | Speaker Track Available                                                                                              | Digital             | ToSIMPL      |
| 1146        | 1         | Presenter Track Availble                                                                                             | Digital             | ToSIMPL      |
| 1159        | 1         | Presenter Track Availble                                                                                             | Digital             | FromSIMPL    |
| 1160        | 1         | Update Meetings                                                                                                      | Digital             | FromSIMPL    |
| 1161        | 10        | Join meeting                                                                                                         | Digital             | FromSIMPL    |
| 1171        | 1         | Mic Mute On                                                                                                          | Digital             | ToFromSIMPL  |
| 1172        | 1         | Mic Mute Off                                                                                                         | Digital             | ToFromSIMPL  |
| 1173        | 1         | Mic Mute Toggle                                                                                                      | Digital             | ToFromSIMPL  |
| 1174        | 1         | Volume Up                                                                                                            | Digital             | FromSIMPL    |
| 1175        | 1         | Volume Down                                                                                                          | Digital             | FromSIMPL    |
| 1176        | 1         | Volume Mute On                                                                                                       | Digital             | ToFromSIMPL  |
| 1177        | 1         | Volume Mute Off                                                                                                      | Digital             | ToFromSIMPL  |
| 1178        | 1         | Volume Mute Toggle                                                                                                   | Digital             | ToFromSIMPL  |
| 1181        | 1         | Pulse to remove the selected recent call item specified by the SelectRecentCallItem analog join                      | Digital             | FromSIMPL    |
| 1182        | 1         | Pulse to dial the selected recent call item specified by the SelectRecentCallItem analog join                        | Digital             | FromSIMPL    |
| 1200        | 1         | Presentation Active                                                                                                  | Digital             | ToSIMPL      |
| 1201        | 1         | Start Sharing & Feedback                                                                                             | Digital             | ToFromSIMPL  |
| 1202        | 1         | Stop Sharing & Feedback                                                                                              | Digital             | ToFromSIMPL  |
| 1203        | 1         | When high, will autostart sharing when a call is joined                                                              | Digital             | FromSIMPL    |
| 1204        | 1         | Recieving content from the far end                                                                                   | Digital             | ToSIMPL      |
| 1205        | 1         | Presentation Local Only Feedback                                                                                     | Digital             | ToFromSIMPL  |
| 1206        | 1         | Presentation Local and Remote Feedback                                                                               | Digital             | ToFromSIMPL  |
| 1207        | 1         | Presentation Local and Remote Feedback                                                                               | Digital             | FromSIMPL    |
| 1211        | 1         | Toggles selfview position                                                                                            | Digital             | FromSIMPL    |
| 1220        | 1         | Holds all calls                                                                                                      | Digital             | FromSIMPL    |
| 1221        | 8         | Holds Call at specified index. FB reported on Call Status XSIG                                                       | Digital             | FromSIMPL    |
| 1230        | 1         | Resume all held calls                                                                                                | Digital             | FromSIMPL    |
| 1231        | 8         | Resume Call at specified index                                                                                       | Digital             | FromSIMPL    |
| 1241        | 1         | Activates Do Not Disturb Mode.  FB High if active.                                                                   | Digital             | ToFromSIMPL  |
| 1242        | 1         | Deactivates Do Not Disturb Mode.  FB High if deactivated.                                                            | Digital             | ToFromSIMPL  |
| 1243        | 1         | Toggles Do Not Disturb Mode.                                                                                         | Digital             | ToSIMPL      |
| 1246        | 1         | Activates Standby Mode.  FB High if active.                                                                          | Digital             | ToFromSIMPL  |
| 1247        | 1         | Deactivates Standby Mode.  FB High if deactivated.                                                                   | Digital             | ToFromSIMPL  |
| 1248        | 1         | Activates Half Wake Mode.  FB High if active.                                                                        | Digital             | ToFromSIMPL  |
| 1249        | 1         | High to indicate that the codec is entering standby mode                                                             | Digital             | ToSIMPL      |
| 1251        | 1         | High to indicate that the codec does not have any meetings currently active                                          | Digital             | ToSIMPL      |
| 1252        | 1         | High to indicate that the codec has currently active meetings                                                        | Digital             | ToSIMPL      |
| 1253        | 1         | High to indicate that the codec has an impending meeting                                                             | Digital             | ToSIMPL      |
| 1261        | 1         | Set / Get PresentationView Default mode                                                                              | Digital             | ToFromSIMPL  |
| 1262        | 1         | Set / Get PresentationView Maximized mode                                                                            | Digital             | ToFromSIMPL  |
| 1263        | 1         | Set / Get PresentationView Minimized mode                                                                            | Digital             | ToFromSIMPL  |
| 1301        | 1         | Multi site option is enabled FB                                                                                      | Digital             | ToSIMPL      |
| 1302        | 1         | Auto Answer is enabled FB                                                                                            | Digital             | ToSIMPL      |
| 1311        | 1         | Webex Pin Requested                                                                                                  | Digital             | ToSIMPL      |
| 1311        | 1         | WebexSendPin                                                                                                         | DigitalSerial       | FromSIMPL    |
| 1312        | 1         | WebexJoinAsGuest                                                                                                     | Digital             | FromSIMPL    |
| 1312        | 1         | WebexJoinedAsHost                                                                                                    | Digital             | ToSIMPL      |
| 1313        | 1         | WebexJoinedAsGuest                                                                                                   | Digital             | ToSIMPL      |
| 1313        | 1         | WebexPinClear                                                                                                        | Digital             | FromSIMPL    |
| 1314        | 1         | WebexPinError                                                                                                        | Digital             | ToSIMPL      |
| 1501        | 50        | Toggles the participant's audio mute status                                                                          | Digital             | ToSIMPL      |
| 1801        | 50        | Toggles the participant's video mute status                                                                          | Digital             | ToSIMPL      |
| 2101        | 50        | Toggles the participant's pin status                                                                                 | Digital             | ToSIMPL      |

## Analogs

| Join Number | Join Span | Description                                                                                      | Type                | Capabilities |
| ----------- | --------- | ------------------------------------------------------------------------------------------------ | ------------------- | ------------ |
| 1021        | 1         | Ringtone volume set/FB.  Valid values are 0 - 100 in increments of 5 (5, 10, 15, 20, etc.)       | Analog              | ToFromSIMPL  |
| 1024        | 1         | Sets the selected Call for DTMF commands. Valid values 1-8                                       | Analog              | FromSIMPL    |
| 1025        | 1         | Reports the number of currently connected calls                                                  | Analog              | ToSIMPL      |
| 1040        | 1         | Set/FB the number of meetings to display via the bridge xsig; default: 3 meetings.               | Analog              | ToFromSIMPL  |
| 1041        | 1         | Minutes before meeting start that a meeting is joinable                                          | Analog              | FromSIMPL    |
| 1042        | 1         | Total minutes until next meeting                                                                 | Analog              | ToSIMPL      |
| 1043        | 1         | Hours until next meeting                                                                         | Analog              | ToSIMPL      |
| 1044        | 1         | Minutes until next meeting                                                                       | Analog              | ToSIMPL      |
| 1060        | 1         | Camera Number Select/FB.  1 based index.  Valid range is 1 to the value reported by CameraCount. | Analog              | ToFromSIMPL  |
| 1061        | 1         | Reports the number of cameras                                                                    | Analog              | ToSIMPL      |
| 1101        | 1         | Directory Select Row and Feedback                                                                | Analog              | FromSIMPL    |
| 1101        | 1         | Directory Row Count FB                                                                           | Analog              | ToSIMPL      |
| 1102        | 1         | Reports the number of contact methods for the selected contact                                   | Analog              | FromSIMPL    |
| 1103        | 1         | Selects a contact method by index                                                                | Analog              | FromSIMPL    |
| 1104        | 1         | Directory Select Row and Feedback                                                                | Analog              | ToSIMPL      |
| 1121        | 1         | Camera Preset Select                                                                             | Analog              | ToSIMPL      |
| 1122        | 1         | Far End Preset Preset Select                                                                     | Analog              | ToSIMPL      |
| 1151        | 1         | Current Participant Count                                                                        | Analog              | ToSIMPL      |
| 1161        | 1         | Meeting Count                                                                                    | Analog              | ToSIMPL      |
| 1174        | 1         | Volume Level                                                                                     | Analog              | ToFromSIMPL  |
| 1180        | 1         | Select/FB for Recent Call Item.  Valid values 1 - 10                                             | Analog              | ToFromSIMPL  |
| 1181        | 10        | Recent Call Occurrence Type. [0-3] 0 = Unknown, 1 = Placed, 2 = Received, 3 = NoAnswer           | Analog              | ToSIMPL      |
| 1191        | 1         | Recent Call Count                                                                                | Analog              | ToSIMPL      |
| 1201        | 1         | Presentation set/FB.  Valid values are 0 - 6 depending on the codec model.                       | Analog              | ToFromSIMPL  |

## Serials

| Join Number | Join Span | Description                                                                                          | Type                | Capabilities |
| ----------- | --------- | ---------------------------------------------------------------------------------------------------- | ------------------- | ------------ |
| 1001        | 1         | Value to dial when ManualDial digital join is pulsed                                                 | Serial              | ToSIMPL      |
| 1002        | 1         | Phone Dial String                                                                                    | Serial              | FromSIMPL    |
| 1002        | 1         | Current Call Data - XSIG                                                                             | Serial              | ToSIMPL      |
| 1005        | 1         | Sends a serial command to the device.  Do not include the delimiter, it will be added automatically. | Serial              | FromSIMPL    |
| 1022        | 1         | Current Call Direction                                                                               | Serial              | ToSIMPL      |
| 1051        | 1         | Incoming Call Name                                                                                   | Serial              | ToSIMPL      |
| 1052        | 1         | Incoming Call Number                                                                                 | Serial              | ToSIMPL      |
| 1100        | 1         | Directory Search String                                                                              | Serial              | FromSIMPL    |
| 1101        | 1         | Directory Entries - XSig, 255 entries                                                                | Serial              | ToSIMPL      |
| 1102        | 1         | Schedule Data - XSIG                                                                                 | Serial              | ToSIMPL      |
| 1103        | 1         | Contact Methods - XSig, 10 entries                                                                   | Serial              | ToSIMPL      |
| 1104        | 1         | XSig Containing Data for Active Meeting                                                              | Serial              | ToSIMPL      |
| 1105        | 1         | Formatted String Showing Time until room no longer available                                         | Serial              | ToSIMPL      |
| 1106        | 1         | Formatted String Showing Time to next meeting                                                        | Serial              | ToSIMPL      |
| 1121        | 1         | Camera Preset Names - XSIG, max of 15                                                                | Serial              | ToSIMPL      |
| 1141        | 1         | Current Layout Fb                                                                                    | Serial              | ToSIMPL      |
| 1142        | 1         | Select Layout by string                                                                              | Serial              | FromSIMPL    |
| 1142        | 1         | xSig of all available layouts                                                                        | Serial              | ToSIMPL      |
| 1151        | 1         | Current Participants XSig                                                                            | Serial              | ToSIMPL      |
| 1161        | 10        | Camera Name Fb                                                                                       | Serial              | ToSIMPL      |
| 1171        | 1         | Selected Recent Call Name                                                                            | Serial              | ToSIMPL      |
| 1172        | 1         | Selected Recent Call Number                                                                          | Serial              | ToSIMPL      |
| 1181        | 10        | Recent Call Names                                                                                    | Serial              | ToSIMPL      |
| 1191        | 10        | Recent Calls Times                                                                                   | Serial              | ToSIMPL      |
| 1201        | 1         | Current Source                                                                                       | Serial              | ToSIMPL      |
| 1211        | 1         | advance selfview position                                                                            | Serial              | ToSIMPL      |
| 1301        | 1         | IP Address of device                                                                                 | Serial              | ToSIMPL      |
| 1302        | 1         | SIP phone number of device                                                                           | Serial              | ToSIMPL      |
| 1303        | 1         | E164 alias of device                                                                                 | Serial              | ToSIMPL      |
| 1304        | 1         | H323 ID of device                                                                                    | Serial              | ToSIMPL      |
| 1305        | 1         | SIP URI of device                                                                                    | Serial              | ToSIMPL      |
| 1311        | 1         | WebexSendPin                                                                                         | DigitalSerial       | FromSIMPL    |
| 1356        | 1         | Selected Directory Entry Name                                                                        | Serial              | ToSIMPL      |
| 1357        | 1         | Selected Directory Entry Number                                                                      | Serial              | ToSIMPL      |
| 1358        | 1         | Selected Directory Folder Name                                                                       | Serial              | ToSIMPL      |



