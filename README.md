# Cisco RoomOS Video Codec Plugin

## Overview

The Cisco RoomOS plugin provides device control over the Cisco Webex family of video conference codecs with regards
to the most commonly used and requested attriute and control types.

It implements all available Essentials interfaces relevant to a device of this type, including but not limited to
`VideoCodecBase`. Interfaces supported can be found by viewing the declaration of the `CiscoCodec` class in `CiscoRoomOsCodec.cs`.

Additionally, every component implements `IKeyed` and all devices are added to the `DeviceManager` unpon instantiation.

## Dependencies

As of [v1.6.0](https://github.com/PepperDash/epi-videoCodec-ciscoExtended/tree/v1.6.0), this plugin requires Essentials 2.1.0 or later. See table below for plugin/Essentials version mapping:

| Plugin version        | Minimum Essentials Version |
| --------------------- | -------------------------- |
| 2.0.x                 | 2.7.0                      |
| 1.11.x, 1.10.x, 1.9.x | 2.7.0                      |
| 1.8.x, 1.7.x          | 2.3.0                      |
| 1.6.x                 | 2.1.0                      |
| 1.5.x & below         | 1.16.x                     |

## License

Provided under MIT license

# Join Map

## Digitals

| Join Number | Join Span | Description                                                                                                         | Type          | Capabilities |
| ----------- | --------- | ------------------------------------------------------------------------------------------------------------------- | ------------- | ------------ |
| 1           | 1         | Device is Online                                                                                                    | Digital       | ToSIMPL      |
| 10          | 1         | If High, will send DTMF tones to the call set by SelectCall analog. If low sends DTMF tones to last connected call. | Digital       | FromSIMPL    |
| 11          | 1         | DTMF 1                                                                                                              | Digital       | FromSIMPL    |
| 12          | 1         | DTMF 2                                                                                                              | Digital       | FromSIMPL    |
| 13          | 1         | DTMF 3                                                                                                              | Digital       | FromSIMPL    |
| 14          | 1         | DTMF 4                                                                                                              | Digital       | FromSIMPL    |
| 15          | 1         | DTMF 5                                                                                                              | Digital       | FromSIMPL    |
| 16          | 1         | DTMF 6                                                                                                              | Digital       | FromSIMPL    |
| 17          | 1         | DTMF 7                                                                                                              | Digital       | FromSIMPL    |
| 18          | 1         | DTMF 8                                                                                                              | Digital       | FromSIMPL    |
| 19          | 1         | DTMF 9                                                                                                              | Digital       | FromSIMPL    |
| 20          | 1         | DTMF 0                                                                                                              | Digital       | FromSIMPL    |
| 21          | 1         | DTMF \*                                                                                                             | Digital       | FromSIMPL    |
| 22          | 1         | DTMF #                                                                                                              | Digital       | FromSIMPL    |
| 24          | 1         | End All Calls                                                                                                       | Digital       | FromSIMPL    |
| 31          | 1         | Current Hook State                                                                                                  | Digital       | ToSIMPL      |
| 41          | 4         | Speed Dial                                                                                                          | Digital       | FromSIMPL    |
| 50          | 1         | Incoming Call                                                                                                       | Digital       | ToSIMPL      |
| 51          | 1         | Answer Incoming Call                                                                                                | Digital       | FromSIMPL    |
| 52          | 1         | Reject Incoming Call                                                                                                | Digital       | FromSIMPL    |
| 71          | 1         | Dial manual string specified by CurrentDialString serial join                                                       | Digital       | FromSIMPL    |
| 72          | 1         | Dial Phone                                                                                                          | Digital       | FromSIMPL    |
| 72          | 1         | Dial Phone                                                                                                          | Digital       | ToSIMPL      |
| 73          | 1         | Hang Up Phone                                                                                                       | Digital       | FromSIMPL    |
| 81          | 8         | End a specific call by call index.                                                                                  | Digital       | FromSIMPL    |
| 90          | 1         | Join all calls                                                                                                      | Digital       | FromSIMPL    |
| 91          | 8         | Join a specific call by call index.                                                                                 | Digital       | FromSIMPL    |
| 100         | 1         | Directory Search Busy FB                                                                                            | Digital       | ToSIMPL      |
| 101         | 1         | Directory Selected Entry Is Contact FB                                                                              | Digital       | ToSIMPL      |
| 101         | 1         | Directory Line Selected FB                                                                                          | Digital       | FromSIMPL    |
| 102         | 1         | Directory is on Root FB                                                                                             | Digital       | ToSIMPL      |
| 103         | 1         | Directory has changed FB                                                                                            | Digital       | ToSIMPL      |
| 104         | 1         | Go to Directory Root                                                                                                | Digital       | FromSIMPL    |
| 105         | 1         | Go back one directory level                                                                                         | Digital       | FromSIMPL    |
| 106         | 1         | Dial selected directory line                                                                                        | Digital       | FromSIMPL    |
| 107         | 1         | Set high to disable automatic dialing of a contact when selected                                                    | Digital       | FromSIMPL    |
| 108         | 1         | Pulse to dial the selected contact method                                                                           | Digital       | FromSIMPL    |
| 110         | 1         | Clear Selected Entry and String from Search                                                                         | Digital       | FromSIMPL    |
| 111         | 1         | Camera Tilt Up                                                                                                      | Digital       | FromSIMPL    |
| 112         | 1         | Camera Tilt Down                                                                                                    | Digital       | FromSIMPL    |
| 113         | 1         | Camera Pan Left                                                                                                     | Digital       | FromSIMPL    |
| 114         | 1         | Camera Pan Right                                                                                                    | Digital       | FromSIMPL    |
| 115         | 1         | Camera Zoom In                                                                                                      | Digital       | FromSIMPL    |
| 116         | 1         | Camera Zoom Out                                                                                                     | Digital       | FromSIMPL    |
| 117         | 1         | Camera Focus Near                                                                                                   | Digital       | FromSIMPL    |
| 118         | 1         | Camera Focus Far                                                                                                    | Digital       | FromSIMPL    |
| 119         | 1         | Camera Auto Focus Trigger                                                                                           | Digital       | FromSIMPL    |
| 121         | 1         | Pulse to save selected preset spcified by CameraPresetSelect analog join. FB will pulse for 3s when preset saved.   | Digital       | ToFromSIMPL  |
| 131         | 1         | Camera Mode Auto. Enables camera auto tracking mode, with feedback                                                  | Digital       | ToFromSIMPL  |
| 132         | 1         | Camera Mode Manual. Disables camera auto tracking mode, with feedback                                               | Digital       | ToFromSIMPL  |
| 133         | 1         | Camera Mode Off. Disables camera video, with feedback. Works like video mute.                                       | Digital       | ToFromSIMPL  |
| 134         | 1         | Presenter Track Off Get/Set                                                                                         | Digital       | ToFromSIMPL  |
| 135         | 1         | Presenter Track Follow Get/Set                                                                                      | Digital       | ToFromSIMPL  |
| 136         | 1         | Presenter Track Background Get/Set                                                                                  | Digital       | ToFromSIMPL  |
| 137         | 1         | Presenter Track Persistent Get/Set                                                                                  | Digital       | ToFromSIMPL  |
| 138         | 1         | SpeakerTrack On Get/Set                                                                                             | Digital       | ToFromSIMPL  |
| 139         | 1         | SpeakerTrack Off Get/Set                                                                                            | Digital       | ToFromSIMPL  |
| 140         | 1         | SpeakerTrack Toggle                                                                                                 | Digital       | FromSIMPL    |
| 141         | 1         | Camera Self View Toggle/FB                                                                                          | Digital       | ToFromSIMPL  |
| 142         | 1         | Camera Layout Toggle                                                                                                | Digital       | FromSIMPL    |
| 143         | 1         | Camera Supports Auto Mode FB                                                                                        | Digital       | ToSIMPL      |
| 144         | 1         | Camera Supports Off Mode FB                                                                                         | Digital       | ToSIMPL      |
| 145         | 1         | Speaker Track Available                                                                                             | Digital       | ToSIMPL      |
| 146         | 1         | Presenter Track Availble                                                                                            | Digital       | ToSIMPL      |
| 159         | 1         | Presenter Track Availble                                                                                            | Digital       | FromSIMPL    |
| 160         | 1         | Update Meetings                                                                                                     | Digital       | FromSIMPL    |
| 161         | 10        | Join meeting                                                                                                        | Digital       | FromSIMPL    |
| 171         | 1         | Mic Mute On                                                                                                         | Digital       | ToFromSIMPL  |
| 172         | 1         | Mic Mute Off                                                                                                        | Digital       | ToFromSIMPL  |
| 173         | 1         | Mic Mute Toggle                                                                                                     | Digital       | ToFromSIMPL  |
| 174         | 1         | Volume Up                                                                                                           | Digital       | FromSIMPL    |
| 175         | 1         | Volume Down                                                                                                         | Digital       | FromSIMPL    |
| 176         | 1         | Volume Mute On                                                                                                      | Digital       | ToFromSIMPL  |
| 177         | 1         | Volume Mute Off                                                                                                     | Digital       | ToFromSIMPL  |
| 178         | 1         | Volume Mute Toggle                                                                                                  | Digital       | ToFromSIMPL  |
| 181         | 1         | Pulse to remove the selected recent call item specified by the SelectRecentCallItem analog join                     | Digital       | FromSIMPL    |
| 182         | 1         | Pulse to dial the selected recent call item specified by the SelectRecentCallItem analog join                       | Digital       | FromSIMPL    |
| 200         | 1         | Presentation Active                                                                                                 | Digital       | ToSIMPL      |
| 201         | 1         | Start Sharing & Feedback                                                                                            | Digital       | ToFromSIMPL  |
| 202         | 1         | Stop Sharing & Feedback                                                                                             | Digital       | ToFromSIMPL  |
| 203         | 1         | When high, will autostart sharing when a call is joined                                                             | Digital       | FromSIMPL    |
| 204         | 1         | Recieving content from the far end                                                                                  | Digital       | ToSIMPL      |
| 205         | 1         | Presentation Local Only Feedback                                                                                    | Digital       | ToFromSIMPL  |
| 206         | 1         | Presentation Local and Remote Feedback                                                                              | Digital       | ToFromSIMPL  |
| 207         | 1         | Presentation Local and Remote Feedback                                                                              | Digital       | FromSIMPL    |
| 211         | 1         | Toggles selfview position                                                                                           | Digital       | FromSIMPL    |
| 220         | 1         | Holds all calls                                                                                                     | Digital       | FromSIMPL    |
| 221         | 8         | Holds Call at specified index. FB reported on Call Status XSIG                                                      | Digital       | FromSIMPL    |
| 230         | 1         | Resume all held calls                                                                                               | Digital       | FromSIMPL    |
| 231         | 8         | Resume Call at specified index                                                                                      | Digital       | FromSIMPL    |
| 241         | 1         | Activates Do Not Disturb Mode. FB High if active.                                                                   | Digital       | ToFromSIMPL  |
| 242         | 1         | Deactivates Do Not Disturb Mode. FB High if deactivated.                                                            | Digital       | ToFromSIMPL  |
| 243         | 1         | Toggles Do Not Disturb Mode.                                                                                        | Digital       | ToSIMPL      |
| 246         | 1         | Activates Standby Mode. FB High if active.                                                                          | Digital       | ToFromSIMPL  |
| 247         | 1         | Deactivates Standby Mode. FB High if deactivated.                                                                   | Digital       | ToFromSIMPL  |
| 248         | 1         | Activates Half Wake Mode. FB High if active.                                                                        | Digital       | ToFromSIMPL  |
| 249         | 1         | High to indicate that the codec is entering standby mode                                                            | Digital       | ToSIMPL      |
| 251         | 1         | High to indicate that the codec does not have any meetings currently active                                         | Digital       | ToSIMPL      |
| 252         | 1         | High to indicate that the codec has currently active meetings                                                       | Digital       | ToSIMPL      |
| 253         | 1         | High to indicate that the codec has an impending meeting                                                            | Digital       | ToSIMPL      |
| 261         | 1         | Set / Get PresentationView Default mode                                                                             | Digital       | ToFromSIMPL  |
| 262         | 1         | Set / Get PresentationView Maximized mode                                                                           | Digital       | ToFromSIMPL  |
| 263         | 1         | Set / Get PresentationView Minimized mode                                                                           | Digital       | ToFromSIMPL  |
| 301         | 1         | Multi site option is enabled FB                                                                                     | Digital       | ToSIMPL      |
| 302         | 1         | Auto Answer is enabled FB                                                                                           | Digital       | ToSIMPL      |
| 311         | 1         | Webex Pin Requested                                                                                                 | Digital       | ToSIMPL      |
| 311         | 1         | WebexSendPin                                                                                                        | DigitalSerial | FromSIMPL    |
| 312         | 1         | WebexJoinAsGuest                                                                                                    | Digital       | FromSIMPL    |
| 312         | 1         | WebexJoinedAsHost                                                                                                   | Digital       | ToSIMPL      |
| 313         | 1         | WebexJoinedAsGuest                                                                                                  | Digital       | ToSIMPL      |
| 313         | 1         | WebexPinClear                                                                                                       | Digital       | FromSIMPL    |
| 314         | 1         | WebexPinError                                                                                                       | Digital       | ToSIMPL      |
| 501         | 50        | Toggles the participant's audio mute status                                                                         | Digital       | ToSIMPL      |
| 801         | 50        | Toggles the participant's video mute status                                                                         | Digital       | ToSIMPL      |
| 1101        | 50        | Toggles the participant's pin status                                                                                | Digital       | ToSIMPL      |

> Note: Using the Camera Mode Auto/Manual/Off joins (131-133) to control the tracking mode relies on the tracking capabilities reported from the codec.<br>
> If only SpeakerTrack is available, setting the camera mode to auto will turn on SpeakerTrack.<br>
> If only PresenterTrack is available, setting the camera mode to auto will turn on PresenterTrack.<br>
> If both are available, setting the camera mode to auto will turn on the preferred mode set using the `defaultTrackingMode` configuration value<br>

## Analogs

| Join Number | Join Span | Description                                                                                    | Type   | Capabilities |
| ----------- | --------- | ---------------------------------------------------------------------------------------------- | ------ | ------------ |
| 21          | 1         | Ringtone volume set/FB. Valid values are 0 - 100 in increments of 5 (5, 10, 15, 20, etc.)      | Analog | ToFromSIMPL  |
| 24          | 1         | Sets the selected Call for DTMF commands. Valid values 1-8                                     | Analog | FromSIMPL    |
| 25          | 1         | Reports the number of currently connected calls                                                | Analog | ToSIMPL      |
| 40          | 1         | Set/FB the number of meetings to display via the bridge xsig; default: 3 meetings.             | Analog | ToFromSIMPL  |
| 41          | 1         | Minutes before meeting start that a meeting is joinable                                        | Analog | FromSIMPL    |
| 42          | 1         | Total minutes until next meeting                                                               | Analog | ToSIMPL      |
| 43          | 1         | Hours until next meeting                                                                       | Analog | ToSIMPL      |
| 44          | 1         | Minutes until next meeting                                                                     | Analog | ToSIMPL      |
| 60          | 1         | Camera Number Select/FB. 1 based index. Valid range is 1 to the value reported by CameraCount. | Analog | ToFromSIMPL  |
| 61          | 1         | Reports the number of cameras                                                                  | Analog | ToSIMPL      |
| 101         | 1         | Directory Select Row and Feedback                                                              | Analog | FromSIMPL    |
| 101         | 1         | Directory Row Count FB                                                                         | Analog | ToSIMPL      |
| 102         | 1         | Reports the number of contact methods for the selected contact                                 | Analog | FromSIMPL    |
| 103         | 1         | Selects a contact method by index                                                              | Analog | FromSIMPL    |
| 104         | 1         | Directory Select Row and Feedback                                                              | Analog | ToSIMPL      |
| 121         | 1         | Camera Preset Select                                                                           | Analog | ToSIMPL      |
| 122         | 1         | Far End Preset Preset Select                                                                   | Analog | ToSIMPL      |
| 151         | 1         | Current Participant Count                                                                      | Analog | ToSIMPL      |
| 161         | 1         | Meeting Count                                                                                  | Analog | ToSIMPL      |
| 174         | 1         | Volume Level                                                                                   | Analog | ToFromSIMPL  |
| 180         | 1         | Select/FB for Recent Call Item. Valid values 1 - 10                                            | Analog | ToFromSIMPL  |
| 181         | 10        | Recent Call Occurrence Type. [0-3] 0 = Unknown, 1 = Placed, 2 = Received, 3 = NoAnswer         | Analog | ToSIMPL      |
| 191         | 1         | Recent Call Count                                                                              | Analog | ToSIMPL      |
| 201         | 1         | Presentation set/FB. Valid values are 0 - 6 depending on the codec model.                      | Analog | ToFromSIMPL  |

## Serials

| Join Number | Join Span | Description                                                                                         | Type          | Capabilities |
| ----------- | --------- | --------------------------------------------------------------------------------------------------- | ------------- | ------------ |
| 1           | 1         | Value to dial when ManualDial digital join is pulsed                                                | Serial        | ToSIMPL      |
| 2           | 1         | Phone Dial String                                                                                   | Serial        | FromSIMPL    |
| 2           | 1         | Current Call Data - XSIG                                                                            | Serial        | ToSIMPL      |
| 5           | 1         | Sends a serial command to the device. Do not include the delimiter, it will be added automatically. | Serial        | FromSIMPL    |
| 22          | 1         | Current Call Direction                                                                              | Serial        | ToSIMPL      |
| 51          | 1         | Incoming Call Name                                                                                  | Serial        | ToSIMPL      |
| 52          | 1         | Incoming Call Number                                                                                | Serial        | ToSIMPL      |
| 100         | 1         | Directory Search String                                                                             | Serial        | FromSIMPL    |
| 101         | 1         | Directory Entries - XSig, 255 entries                                                               | Serial        | ToSIMPL      |
| 102         | 1         | Schedule Data - XSIG                                                                                | Serial        | ToSIMPL      |
| 103         | 1         | Contact Methods - XSig, 10 entries                                                                  | Serial        | ToSIMPL      |
| 104         | 1         | XSig Containing Data for Active Meeting                                                             | Serial        | ToSIMPL      |
| 105         | 1         | Formatted String Showing Time until room no longer available                                        | Serial        | ToSIMPL      |
| 106         | 1         | Formatted String Showing Time to next meeting                                                       | Serial        | ToSIMPL      |
| 121         | 1         | Camera Preset Names - XSIG, max of 15                                                               | Serial        | ToSIMPL      |
| 141         | 1         | Current Layout Fb                                                                                   | Serial        | ToSIMPL      |
| 142         | 1         | Select Layout by string                                                                             | Serial        | FromSIMPL    |
| 142         | 1         | xSig of all available layouts                                                                       | Serial        | ToSIMPL      |
| 151         | 1         | Current Participants XSig                                                                           | Serial        | ToSIMPL      |
| 161         | 10        | Camera Name Fb                                                                                      | Serial        | ToSIMPL      |
| 171         | 1         | Selected Recent Call Name                                                                           | Serial        | ToSIMPL      |
| 172         | 1         | Selected Recent Call Number                                                                         | Serial        | ToSIMPL      |
| 181         | 10        | Recent Call Names                                                                                   | Serial        | ToSIMPL      |
| 191         | 10        | Recent Calls Times                                                                                  | Serial        | ToSIMPL      |
| 201         | 1         | Current Source                                                                                      | Serial        | ToSIMPL      |
| 211         | 1         | advance selfview position                                                                           | Serial        | ToSIMPL      |
| 301         | 1         | IP Address of device                                                                                | Serial        | ToSIMPL      |
| 302         | 1         | SIP phone number of device                                                                          | Serial        | ToSIMPL      |
| 303         | 1         | E164 alias of device                                                                                | Serial        | ToSIMPL      |
| 304         | 1         | H323 ID of device                                                                                   | Serial        | ToSIMPL      |
| 305         | 1         | SIP URI of device                                                                                   | Serial        | ToSIMPL      |
| 311         | 1         | WebexSendPin                                                                                        | DigitalSerial | FromSIMPL    |
| 321         | 1         | WidgetEventData                                                                                     | Serial        | ToSIMPL      |
| 356         | 1         | Selected Directory Entry Name                                                                       | Serial        | ToSIMPL      |
| 357         | 1         | Selected Directory Entry Number                                                                     | Serial        | ToSIMPL      |
| 358         | 1         | Selected Directory Folder Name                                                                      | Serial        | ToSIMPL      |

# Configuration

## Codec Device

```json
{
  "key": "Codec-1",
  "name": "Video Codec 1",
  "type": "ciscoRoomOS",
  "group": "videoCodec",
  "properties": {
    "control": {
      "endOfLineString": "\n",
      "deviceReadyResponsePattern": "",
      "method": "Ssh",
      "tcpSshProperties": {
        "address": "10.0.0.1",
        "port": 22,
        "autoReconnect": true,
        "AutoReconnectIntervalMs": 10000,
        "username": "admin",
        "password": "tandberg"
      }
    },
    "phonebookDisableAutoPopulate": true,
    "phonebookMode": "Corporate",
    "showSelfViewByDefault": true,
    "sharing": {
      "autoShareContentWhileInCall": false,
      "defaultShareLocalOnly": true
    },
    "joinableCooldownSeconds": 0,
    "phonebookResultsLimit": 50,
    "defaultTrackingMode": "SpeakerTrack",
    "overrideMeetingsLimit": true,
    "usePersistentWebAppForLockout": true,
    "cameraInfo": [
      {
        "CameraNumber": 1,
        "Name": "Audience"
      },
      {
        "CameraNumber": 2,
        "Name": "Presenter"
      }
    ],
    "vlanId": 1 // optional property to specify the VLAN ID for the camera network on a managed switch. Used by the CameraManager
  }
}
```

## Navigator

Place your custom icon .png files in the /user/programX/navigatorIcons/ folder.
This will automatically generate an output file:
/user/programX/navigatorIcons/icons-base64.txt,
which contains the Base64-encoded "customIconContent" for each icon.

The plugin sends `customIconContent` to the codec split into 76-character lines. Over RS-232, RoomOS restarts the serial session when it receives a single line longer than 2,048 bytes (including CR LF), and an icon sent as one line is usually longer than that. When the codec is controlled over RS-232, the plugin logs an error and does not send any command that still has a line over 2,048 bytes.

### Navigator Lockout Functionality **NEW**

There are two different methods supported to show a lockout screen on a Navigator panel for use in combined room systems where there is a desire to lockout navigator panels that are attached to a codec not being used as the primary codec in a room combination scenario.

- Persistent Web App (PWA) Mode (Use for codecs in Webex mode):
  - Preferred as it switches the Navigator to a full screen web app mode that can't be cancelled by the user and can show any webpage, usually a route in a mobile control app displaying information to the user that the room is locked out or there is an emergency message.
  - If using this method, the `usePersistentWebAppForLockout` config property must be defined on the codec and set to true.
  - Additionally, each instance of the `ciscoRoomOsMobileControl` device needs to have the `macAddress` property defined and populated (MAC Address value is case sensitive!) as the commands to put
    each panel in PWA or controller mode require the MAC address.
  - To get the mac addresses of the Navigator panels, the command `xcommand Peripherals list Type: touchpanel` can be sent to the codec or you can use the codec's web interface and view the paired peripherals.
  - Limitation: This method currently DOES NOT work if the codec is in MTR (Microsoft Teams Mode) configuration. The panels will enter PWA mode but will not return to controller mode without the panels being rebooted.
- WebView Modal Mode with Polling and Timer (Use for codecs in MTR mode):
  - This mode will display a modal webview on the navigator with any URL supplied. However, the modal has a built in X button in the top right corner that the user can use to dismiss the modal. As a result, the plugin logic will poll the codec to determine if the navigator is displaying the modal and if it has been cancelled by the user a command will be re-sent to display the lockout modal again.
  - Limitation: This method DOES NOT work properly if there are more than one navigator panel paired to a codec as we cannot determine whether both panels are displaying the lockout modal if only one panel has dismissed the modal

### RoomOS Firmware Compatibility

#### RoomOS ce26+ Breaking Change

RoomOS ce26 introduced a breaking schema change to WebView Display events:

**Pre-ce26 firmware:**
```json
"Url": "http://10.11.50.169:50002/mc/app/techPin?token=..."
```

**ce26+ firmware:**
```json
"Url": {
  "Value": "http://10.11.50.169:50002/mc/app/techPin?token=...",
  "id": "1"
}
```

Cisco normalized their event property schema to wrap values in objects with `Value` and `id` properties. This change affected WebView URL handling. The plugin includes a custom converter (`UrlConverter` in `xEvent.cs`) that handles both formats transparently for backwards compatibility with older RoomOS versions.

**No action required** – the plugin automatically detects and handles both payload formats.

### Available default icons

Briefing
Camera
Concierge
Disc
Handset
Help
Helpdesk
Home
Hvac
Info
Input
Language
Laptop
Lightbulb
Media
Microphone
Power
Proximity
Record
Spark
Tv
Webex
General
Sliders

```json
{
  "key": "navigator",
  "name": "Rm Navigator",
  "type": "ciscoRoomOsMobileControl",
  "group": "videoCodecTouchpanel",
  "properties": {
    "defaultRoomKey": "room",
    "macAddress": "a0:b1:c2:d3:e4:f5", // CASE SENSITIVE VALUE!!!!
    "useDirectServer": true,
    "videoCodecKey": "Codec-1",
    "enableLockoutPoll": true,
    "lockout": {
      "mobileControlPath": "/lockout",
      "uiWebViewDisplays": {
        "title": "Room Lockout",
        "mode": "Fullscreen",
        "target": "Controller"
      }
    },
    "extensions": {
      "configId": 1,
      "panels": [
        {
          "order": 2,
          "panelId": "audio",
          "location": "ControlPanel",
          "icon": "Sliders",
          "name": "Volume",
          "mobileControlPath": "/audio",
          "uiWebViewDisplays": [
            {
              "title": "Audio Volume",
              "mode": "Modal",
              "target": "Controller"
            }
          ]
        },
        {
          "order": 3,
          "panelId": "roomCombine",
          "location": "ControlPanel",
          "icon": "Custom",
          "iconId": "1234",
          "customIconContent": "iVBORw0KGgoAAAANSUhEUgAAADwAAAA8CAYAAAA6/NlyAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAABS8AAAUvAVpwdGYAAAAYdEVYdFNvZnR3YXJlAFBhaW50Lk5FVCA1LjEuNBLfpoMAAAC2ZVhJZklJKgAIAAAABQAaAQUAAQAAAEoAAAAbAQUAAQAAAFIAAAAoAQMAAQAAAAIAAAAxAQIAEAAAAFoAAABphwQAAQAAAGoAAAAAAAAAw4MAAOgDAADDgwAA6AMAAFBhaW50Lk5FVCA1LjEuNAADAACQBwAEAAAAMDIzMAGgAwABAAAAAQAAAAWgBAABAAAAlAAAAAAAAAACAAEAAgAEAAAAUjk4AAIABwAEAAAAMDEwMAAAAACJB7xjFmMTpAAAAepJREFUaEPtmuFRwjAUx/91AjYQJxAnECeQEXQC2EDdQCaADagTiBOIG+AEssHzS9JL/7xYEbw2r/nd5UNf2pLfpS85Lq8QEfSJMw5YJwtbJwtbp3fCRcO2NAAwAXANYMidHWML4A1ACWDHnRUiorWBiCwkXRbOgb3UGR4BeHWzmzI7ADcANmGQc9iKLJzDq3OqCGd4COA9IrsDsAbwwR1/5IGun+j6UC4BjH8Y+5XL8VoOrzgRRORLRGacB0e2R/4RF+OBVijviLU7N2Zm5e/xn/TQrcYhPgeeKd5llm7MvEpP/C7jhaf1fgDAPSd8Imzc2JkpAuFaYruHSoqlRKlM1giB8Ljehxe6ThF2GEPZlsyTha2Tha2ThbtIURQHtxhJCJ+SLGydNoTPORCJHYv6zjaEPzkQiVX4/7K/IbhXfWcbwq2ShVOni/uwtphosYomiUNoQ1hbTLTYv9CGcKtkYetk4S6inDA0thhJCJ+SLGydLGwdL7ym+C1dpwg7rBEIaydtfF6cEpPIiWglPK/3AQAWykMpMHJjZ+YIhLfKebAvCplRvMvMIkU5pa/x6F1RC9dpWSpbglarxdvSJlIUkiJ7slCE4W64cBUxqbJ0Drz77H3SjLni0iZhc2iftGmysHWysHV6J/wNJ9Ukf3MotnsAAAAASUVORK5CYII=",
          "name": "Room Setup",
          "mobileControlPath": "/roomCombine",
          "uiWebViewDisplays": [
            {
              "title": "Room Setup",
              "mode": "Modal",
              "target": "Controller"
            }
          ]
        },
        {
          "order": 1,
          "panelId": "techPin",
          "location": "ControlPanel",
          "icon": "Language",
          "name": "Technician",
          "mobileControlPath": "/techPin",
          "uiWebViewDisplays": [
            {
              "title": "Technician",
              "mode": "Modal",
              "target": "Controller"
            }
          ]
        }
      ]
    }
  }
}
```

> Note: Not all configuration properties are currently shown

## Camera

The typename `ciscocamera` will create a device that represents a Cisco camera attached to a Cisco codec

```json
{
  "key": "camera1",
  "name": "Camera 1",
  "type": "ciscocamera",
  "properties": {
    "defaultParentCodecKey": "codec1",
    "defaultCameraId": 1,
    "serialNumber": "123456789",
    "macAddress": "00:1A:2B:3C:4D:5E",
    "flipImage": false,
    "sourceId": 1,
    "networkSwitchPort": "Gi/0/1" // used by the CameraManager to setup network port as needed for switching cameras between codecs
  }
}
```

## CameraManager

the typename `ciscocameramanager` will create a device that manages cameras that can switch between different codecs based on room combination scenarios.

Use this section as a quick overview. For detailed behavior, migration flow, failure/recovery logic, and advanced troubleshooting, use `docs/CameraManager-Readme.md`.

The `CameraManager` device works with:

- a managed network switch implementing `INetworkSwitchPoeVlanManager`
- `CiscoCodec` devices implementing `ICiscoCodecCameraFactoryReset`
- `CiscoCamera` devices that include required migration properties (`networkSwitchPort`, `serialNumber`, `defaultParentCodecKey`, `defaultCameraId`)

It monitors the active room-combiner scenario and moves only cameras listed in scenario `cameraKeys` as needed between codecs.
This minimal example uses the simple string form for `cameraKeys`; for explicit per-codec target `cameraId` mappings and full behavior details, see `docs/CameraManager-Readme.md`.

Minimal CameraManager config example:

```json
{
  "key": "cameraManager1",
  "name": "Camera Manager",
  "type": "cameramanager",
  "properties": {
    "networkSwitchKey": "ciscoSwitch-1",
    "roomCombinerConfig": {
      "roomCombinerKey": "essentialsroomcombiner",
      "combineScenarios": {
        "divided": {
          "codecConfigs": [
            {
              "codecKey": "codecA",
              "cameraKeys": ["cameraA-front"]
            }
          ]
        }
      }
    }
  }
}
```

## Bridge

```json
{
  "uid": 20,
  "key": "eisc-vc",
  "type": "eiscApiAdvanced",
  "group": "api",
  "name": "EISC VC Bridge",
  "properties": {
    "control": {
      "tcpSshProperties": {
        "address": "127.0.0.2",
        "port": 0
      },
      "ipId": "4F"
    },
    "devices": [
      {
        "deviceKey": "Codec-1",
        "joinStart": 1
      }
    ]
  }
}
```

# Console commands

- `devjson {"deviceKey":"Codec-1-ssh","methodName":"SendText", "params": ["xStatus SystemUnit\n"]}`
  - Invoke a method on the device. The example here can be used to send the `xStatus SystemUnit` command directly
    to the device
- `devmethods {deviceKey}`
  - Get a list of methods to use with the `devjson` command
  - {deviceKey} - device key to get the methods for
- `setdevicestreamdebug {deviceKey} {Off|Both|Tx|Rx}`
  - Turn on comm-level (ssh, tcp, serial) debugging messages
  - {deviceKey} - Device key to turn on comm debugging for. Should be the actual connection device, usually the standard key name with `-ssh` appended
  - {Off|Both|Tx|Rx} - Direction to turn on debugging for
    - Off - Logging will be turned off
    - Tx - Commands sent to the device will be logged
    - Rx - Reponsees from the device will be logged
    - Both - Commands and responses will be logged
- `setcodeccommdebug {1 | 0}`
  - Turn on device-level communications debugging. In order to see thes messages, the `appdebug 1` command must be sent
  - {1 | 0} - 1 turns debuggging on, 0 turns it off
<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
<!-- END Minimum Essentials Framework Versions -->
<!-- START Config Example -->
### Config Example

```json
{
    "key": "GeneratedKey",
    "uid": 1,
    "name": "GeneratedName",
    "type": "ciscoRoomBarPro",
    "group": "Group",
    "properties": {
        "communicationMonitorProperties": "SampleValue",
        "favorites": [
            "SampleValue"
        ],
        "phonebookMode": "SampleString",
        "showSelfViewByDefault": true,
        "SelfViewDefaultMonitorRole": "SampleValue",
        "sharing": {
            "autoShareContentWhileInCall": true,
            "defaultShareLocalOnly": true
        },
        "externalSourceListEnabled": true,
        "externalSourceInputPort": "SampleString",
        "phonebookResultsLimit": "SampleValue",
        "overrideMeetingsLimit": true,
        "EnableCommDebugOnStartup": true,
        "getPhonebookOnStartup": true,
        "getBookingsOnStartup": true,
        "phonebookDisableAutoPopulate": true,
        "phonebookDisableAutoDial": true,
        "UiBranding": {
            "enable": true,
            "brandingUrl": "SampleString"
        },
        "enableCameraConfigFromRoomCameraList": true,
        "cameraInfo": [
            {
                "CameraNumber": 0,
                "Name": "SampleString",
                "SourceId": 0
            }
        ],
        "defaultTrackingMode": "SampleString",
        "timeFormatSpecifier": "SampleString",
        "dateFormatSpecifier": "SampleString",
        "joinableCooldownSeconds": 0,
        "endAllCallsOnMeetingJoin": true,
        "usePersistentWebAppForLockout": true,
        "extensions": {
            "Version": "SampleString",
            "ConfigId": "SampleValue",
            "panels": [
                {
                    "order": "SampleValue",
                    "panelId": "SampleString",
                    "Origin": "SampleString",
                    "ActivityType": "SampleString",
                    "location": "SampleValue",
                    "Type": "SampleString",
                    "color": "SampleString",
                    "icon": "SampleValue",
                    "CustomIcon": {
                        "Id": "SampleString",
                        "Content": "SampleString",
                        "WrappedContent": "SampleString"
                    },
                    "iconId": "SampleString",
                    "customIconContent": "SampleString",
                    "name": "SampleString",
                    "Url": "SampleString",
                    "MobileControlPath": "SampleString",
                    "UiWebViewDisplays": [
                        {
                            "Target": "SampleString",
                            "Mode": "SampleString",
                            "Title": "SampleString",
                            "Url": "SampleString",
                            "Header": "SampleString",
                            "Options": "SampleString",
                            "QueryParams": {
                                "SampleString": "SampleString"
                            }
                        }
                    ],
                    "DeviceActions": [
                        "SampleValue"
                    ],
                    "PanelFeedback": {
                        "deviceKey": "SampleString",
                        "feedbackKey": "SampleString",
                        "propertyToChange": "SampleValue",
                        "feedbackEventType": "SampleValue",
                        "TruePropertyValue": "SampleString",
                        "FalsePropertyValue": "SampleString",
                        "StringFeedbackPropertyValues": {
                            "SampleString": "SampleString"
                        },
                        "IntFeedbackPropertyValues": {
                            "0": "SampleString"
                        }
                    },
                    "PanelFeedbacks": [
                        {
                            "deviceKey": "SampleString",
                            "feedbackKey": "SampleString",
                            "propertyToChange": "SampleValue",
                            "feedbackEventType": "SampleValue",
                            "TruePropertyValue": "SampleString",
                            "FalsePropertyValue": "SampleString",
                            "StringFeedbackPropertyValues": {
                                "SampleString": "SampleString"
                            },
                            "IntFeedbackPropertyValues": {
                                "0": "SampleString"
                            }
                        }
                    ],
                    "Page": {
                        "name": "SampleString",
                        "pageId": "SampleString",
                        "Options": "SampleString",
                        "rows": [
                            {
                                "Name": "SampleString",
                                "widgets": [
                                    {
                                        "widgetId": "SampleString",
                                        "name": "SampleString",
                                        "type": "SampleValue",
                                        "Options": "SampleString"
                                    }
                                ]
                            }
                        ]
                    },
                    "PanelFeedback1": {
                        "deviceKey": "SampleString",
                        "feedbackKey": "SampleString",
                        "propertyToChange": "SampleValue",
                        "feedbackEventType": "SampleValue",
                        "TruePropertyValue": "SampleString",
                        "FalsePropertyValue": "SampleString",
                        "StringFeedbackPropertyValues": {
                            "SampleString": "SampleString"
                        },
                        "IntFeedbackPropertyValues": {
                            "0": "SampleString"
                        }
                    },
                    "PanelFeedback2": {
                        "deviceKey": "SampleString",
                        "feedbackKey": "SampleString",
                        "propertyToChange": "SampleValue",
                        "feedbackEventType": "SampleValue",
                        "TruePropertyValue": "SampleString",
                        "FalsePropertyValue": "SampleString",
                        "StringFeedbackPropertyValues": {
                            "SampleString": "SampleString"
                        },
                        "IntFeedbackPropertyValues": {
                            "0": "SampleString"
                        }
                    },
                    "PanelFeedback3": {
                        "deviceKey": "SampleString",
                        "feedbackKey": "SampleString",
                        "propertyToChange": "SampleValue",
                        "feedbackEventType": "SampleValue",
                        "TruePropertyValue": "SampleString",
                        "FalsePropertyValue": "SampleString",
                        "StringFeedbackPropertyValues": {
                            "SampleString": "SampleString"
                        },
                        "IntFeedbackPropertyValues": {
                            "0": "SampleString"
                        }
                    },
                    "PanelFeedback4": {
                        "deviceKey": "SampleString",
                        "feedbackKey": "SampleString",
                        "propertyToChange": "SampleValue",
                        "feedbackEventType": "SampleValue",
                        "TruePropertyValue": "SampleString",
                        "FalsePropertyValue": "SampleString",
                        "StringFeedbackPropertyValues": {
                            "SampleString": "SampleString"
                        },
                        "IntFeedbackPropertyValues": {
                            "0": "SampleString"
                        }
                    },
                    "PanelFeedback5": {
                        "deviceKey": "SampleString",
                        "feedbackKey": "SampleString",
                        "propertyToChange": "SampleValue",
                        "feedbackEventType": "SampleValue",
                        "TruePropertyValue": "SampleString",
                        "FalsePropertyValue": "SampleString",
                        "StringFeedbackPropertyValues": {
                            "SampleString": "SampleString"
                        },
                        "IntFeedbackPropertyValues": {
                            "0": "SampleString"
                        }
                    }
                }
            ],
            "doNotSendXml": true,
            "PanelsHandler": {}
        },
        "emergency": {
            "mobileControlPath": "SampleString",
            "uiWebViewDisplay": {
                "Header": "SampleString",
                "Mode": "SampleString",
                "Options": "SampleString",
                "Target": "SampleString",
                "Title": "SampleString",
                "Url": "SampleString"
            }
        },
        "DefaultProvisioningMode": "SampleString",
        "VLanId": "SampleValue"
    }
}
```
<!-- END Config Example -->
<!-- START Supported Types -->
### Supported Types

- ciscoRoomBarPro
- ciscoRoomOS
- ciscoCodecEq
- ciscoCodecPro
- ciscoRoomBar
- ciscocamera
- ciscoRoomOsMobileControl
<!-- END Supported Types -->
<!-- START Join Maps -->

<!-- END Join Maps -->
<!-- START Interfaces Implemented -->
### Interfaces Implemented

- IHasCallHistory
- IHasCallFavorites
- IHasDirectory
- IHasScheduleAwareness
- IOccupancyStatusProvider
- IHasCodecLayoutsAvailable
- IHasCodecSelfView
- ICommunicationMonitor
- IRoutingSinkWithFeedback
- IRoutingSource
- IHasCodecCameras
- IHasCameraAutoMode
- IHasCodecRoomPresets
- IHasExternalSourceSwitching
- IHasBranding
- IHasCameraOff
- IHasCameraMute
- IHasDoNotDisturbMode
- IHasHalfWakeMode
- IHasCallHold
- IJoinCalls
- IDeviceInfoProvider
- IHasPhoneDialing
- ICiscoCodecCameraConfig
- ISpeakerTrack
- IPresenterTrack
- IEmergencyOSD
- IHasWebViewWithPwaMode
- IKeyed
- IQueueMessage
- ICiscoCodecCameraFactoryReset
- IHasCameraPtzControl
- IAmFarEndCamera
- IBridgeAdvanced
- IHasCameraFocusControl
- INavigatorLockoutHandlerWithPwa
- INavigatorLockoutHandler
- IMobileControlTouchpanelController
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- ValueProperty
- VideoCodecInfo
- VideoCodecBase
- JsonConverter
- EventArgs
- GenericCommunicationMonitor
- VideoCodecControllerJoinMap
- MessengerBase
- CameraBase
- ReconfigurableDevice
- CiscoCodecUserInterface
- UserInterfaceConfig
- EssentialsDevice
- DefaultContractResolver
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public void Insert(ushort pos, string val)
- public void Clear()
- public void Availability_Maps_To_Capabilities(bool speakerTrack, bool presenterTrack, eCameraTrackingCapabilities expected)
- public void Factory_Source_Sets_MinimumEssentialsFrameworkVersion(string factoryName)
- public void Factory_Source_Sets_TypeNames(string factoryName)
- public void Factory_Source_Contains_TypeName(string factoryName, string typeName)
- public void No_Duplicate_TypeNames_Across_Factories()
- public void A_Status_Value_Notifies_Only_When_It_Changes()
- public void The_First_Set_Always_Notifies_Even_If_It_Is_Null()
- public void An_Event_Value_Notifies_Every_Time()
- public void Counters_Show_Notified_And_Unchanged_Sets_For_Wired_Values_Only()
- public void Populating_The_Same_Status_Twice_Notifies_Once()
- public void A_Partial_Update_Changes_Only_The_Item_With_That_Id()
- public void A_Ghost_Removes_The_Item()
- public void A_Ghost_For_An_Unknown_Id_Changes_Nothing()
- public void A_New_Id_Is_Added_At_The_End()
- public void A_Single_Object_Is_Treated_As_A_List_Of_One()
- public void Update_Add_And_Ghost_In_One_Message()
- public void Works_When_The_List_Starts_Null_Or_Empty_And_On_Deserialize()
- public void Serializes_As_A_Plain_Array()
- public void A_Value_Wrapped_Id_Is_Read_Through_Its_Value()
- public void RoomPresetsChange_NotifiesOnEveryPresetListUpdate()
- public void Login_Prompt_Without_Line_Ending_Is_Detected()
- public void Prompt_Split_Across_Chunks_Is_Detected()
- public void Captured_Failed_Login_Sequence()
- public void Lockout_Line_Is_Detected()
- public void Prompt_Is_Signalled_Once_Until_A_New_Line()
- public void Codec_Output_Is_Not_A_Login_Signal(string text)
- public void Long_Unfinished_Line_Ending_In_A_Prompt_Word_Is_Not_A_Prompt()
- public void Prompt_After_A_Long_Line_Is_Detected()
- public void Summary_Reports_Count_Last_Min_Max_And_Percentiles()
- public void Percentiles_Use_Only_The_Most_Recent_Samples()
- public void Invalid_Samples_Are_Ignored(double ms)
- public void Stop_Records_A_Started_Operation_Once()
- public void Stop_Without_Start_Records_Nothing()
- public void Overlapping_Operations_Are_Told_Apart_By_Id()
- public void Pending_Operations_That_Never_Stop_Do_Not_Pile_Up()
- public void Reset_Clears_Samples_And_Pending_Operations()
- public void Concurrent_Records_Are_All_Counted()
- public void FormatTable_Lists_Every_Name_In_Order()
- public void FormatTable_Says_So_When_Empty()
- public void ElapsedMs_Converts_Stopwatch_Ticks()
- public void Config_Class_Exists(string className)
- public void Config_Has_Parameterless_Constructor(string className)
- public void Config_Property_Has_JsonPropertyAttribute(string className, string jsonName)
- public void A_Pretty_Printed_Document_Is_Found()
- public void Chunk_Boundaries_Do_Not_Matter(int chunk)
- public void Braces_And_Quotes_Inside_Strings_Are_Ignored()
- public void One_Line_Documents_Are_Documents()
- public void Lines_Between_Documents_Are_Classified()
- public void An_Echoed_Command_Line_Inside_A_Document_Is_Dropped()
- public void An_Echoed_Tagged_Command_With_Quoted_Values_Is_Dropped()
- public void Echo_Like_Text_Inside_A_String_Is_Kept()
- public void Text_Mode_Output_Abandons_A_Partial_Document()
- public void A_Lost_Quote_Does_Not_Throw_Framing_Out_Of_Step()
- public void DocumentStarted_Fires_Once_Per_Document_Before_It_Is_Received()
- public void A_Login_Banner_Inside_A_Document_Abandons_It()
- public void Text_After_A_Document_On_The_Same_Line_Is_Its_Own_Line()
- public void Nothing_Is_Reported_Until_A_Line_Ends()
- public void Reset_Abandons_A_Partial_Document()
- public void Matches_The_Current_Line_Based_Framing_On_A_Mixed_Stream()
- public void ROOMOS_FIXTURES_When_Set_Points_At_Fixtures()
- public void Every_Fixture_Is_One_Valid_Document_However_It_Is_Chunked_When_Available()
- public void All_Fixtures_Back_To_Back_Match_The_Current_Framing_When_Available()
- public void A_Login_Successful_Value_Does_Not_Abandon_The_Document()
- public void The_Rest_Of_An_Abandoned_Document_Is_Not_Framed_As_New_Documents()
- public void Text_At_Column_Zero_After_An_Abandon_Is_Still_Reported()
- public void A_Webinar_Prompt_Gives_The_Codec_Call_Id_Not_The_List_Position()
- public void Every_Request_Type_But_None_Asks_For_A_Pin(string type, bool requested, bool webinar)
- public void The_Call_With_The_Request_Is_Found_Among_Several_And_As_A_Single_Object()
- public void A_Joined_Response_Gives_The_Role(string role)
- public void A_Pin_Error_And_Missing_Fields_Are_Handled()
- public void Replies_Use_The_Call_Id_The_Role_And_An_Optional_Pin()
- public void A_Rejected_Pin_Is_An_Error_And_Never_A_Join()
- public void A_Pin_Submitted_On_A_Call_That_Ended_Is_Not_Reported_On_The_Next_Call()
- public void A_Guest_Join_At_A_Webinar_Prompt_Is_An_Attendee_And_At_A_Meeting_Prompt_A_Guest()
- public void An_Accepted_Pin_Joins_When_The_Codec_Clears_The_Request()
- public void Contact_Info_Number_Is_Used()
- public void Webex_Shared_Sip_Address_Is_Used_Without_Contact_Info()
- public void Document_Without_Contact_Info_Gives_Null()
- public void Registration_Uri_Wins_Then_Contact_Address_Then_Unknown(string registrationUri, string contactAddress, string expected)
- public void Phone_Number_Comes_From_The_Registration_Uri_Only(string registrationUri, string expected)
- public void Webex_Address_Digits_Are_Not_A_Phone_Number()
- public void Document_With_The_Address_Is_Authoritative()
- public void Document_Reporting_No_Address_Clears_It()
- public void Partial_Update_Without_The_Address_Fields_Keeps_The_Cache(string json)
- public void Handlers_Get_The_Subtree_At_Their_Path()
- public void Absent_Paths_Are_Not_Called()
- public void Handlers_Run_In_Registration_Order_And_A_Path_Can_Have_Several()
- public void ResultId_Is_Passed_With_The_Message()
- public void Every_Top_Level_Object_In_A_Document_Is_Dispatched()
- public void A_Failing_Handler_Does_Not_Stop_The_Others()
- public void A_Path_Through_A_Non_Object_Is_Not_Matched()
- public void Invalid_Json_Throws_For_The_Caller_To_Log()
- public void Dates_Stay_As_The_Codec_Sent_Them()
- public void Registration_Is_Validated()
- public void A_Reply_Completes_Its_Own_Request_Once()
- public void Replies_Without_A_ResultId_Complete_Nothing_And_Drop_Nothing()
- public void Replies_May_Arrive_In_Any_Order()
- public void An_Unknown_Id_Changes_Nothing()
- public void Requests_Without_A_Reply_Expire_After_The_Timeout()
- public void A_Request_Can_Have_Its_Own_Timeout()
- public void PendingCount_Without_A_Kind_Counts_Every_Request()
- public void Clear_Drops_Everything()
- public void Begin_And_Complete_Are_Thread_Safe()
- public void Timeout_Must_Be_Positive()
- public void Assembly_Loads_Successfully()
- public void Assembly_Name_Matches_Expected()
- public void Factory_Count_Matches_Expected()
- public void Factory_Exists_ByName(string factoryName)
- public void All_Factories_Have_Parameterless_Constructor()
- public void Repeated_Full_Documents_Do_Not_Grow_Any_List()
- public void Partial_Feedback_Updates_Only_That_Item_And_Keeps_The_Count_In_Step()
- public void Excluded_Lists_Are_Left_To_The_Code_That_Maintains_Them()
- public void Wired_Values_Keep_Their_Action_And_Fire_Only_On_A_Real_Change()
- public void An_Action_That_Throws_Is_Reported_And_The_Rest_Of_The_Document_Is_Populated()
- public void Without_A_Failure_Handler_The_Exception_Propagates_As_Before()
- public void Warm_Walks_Past_Accessors_That_Throw_On_Default_Values()
- public void Warm_Runs_No_Actions_And_Counts_Nothing()
- public void Wiring_Is_Not_Codec_Data_So_A_Self_Wired_Model_Warms_And_Keeps_Its_Action()
- public void New_Path_Gives_The_Same_Result_As_Text_Round_Trip()
- public void DateTime_Values_Keep_Their_Instant_And_Kind()
- public void Dates_In_String_Properties_Stay_As_The_Codec_Sent_Them()
- public void Populating_Merges_Into_An_Existing_Object()
- public void Settings_Are_Applied_The_Same_Way()
- public void ToObject_Keeps_DateTime_Values_And_Fixes_Dates_In_String_Properties()
- public void ResultId_Is_Read_When_Present()
- public void ResultId_Is_An_Empty_Guid_When_Absent()
- public void PopulateFromToken_Rejects_Null_Arguments()
- public void Booking_Times_From_The_Codec_Are_Utc_And_Are_Shown_In_Local_Time()
- public void Local_And_Unspecified_Times_Are_Left_Alone()
- public void Whole_Numbers_Of_Any_Type_Are_Inputs(object selector)
- public void Anything_Else_Is_Not(object? selector)
- public void A_Selector_From_Json_Is_A_Long_And_Works()
- public void Start(int port)
- public void FireUpdate()
- public void DialZoom()
- public string DialZoomEnhanced()
- public void DialWebex()
- public void InitializeBranding(string roomKey)
- public void PollSpeakerTrack()
- public void PollPresenterTrack()
- public void SetCommDebug(bool on)
- public void EnqueueCommand(string command)
- public void SendText(string command)
- public void ParseSipToken(JToken sipToken)
- public void PrintCallItem(CodecActiveCallItem callData)
- public eCodecCallType ConvertToTypeEnum(string s)
- public eCodecCallDirection ConvertToDirectionEnum(string s)
- public eCodecCallStatus ConvertToStatusEnum(string s)
- public bool MergeCallData(
			CodecActiveCallItem existingCallData,
			CodecActiveCallItem newCallData
		)
- public void OnWebViewClearedFromUi(PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.WebView.WebViewDisplayClearActionArgs args)
- public void CodecPollLayouts()
- public void ExecuteSwitch(
			object inputSelector,
			object outputSelector,
			eRoutingSignalType signalType
		)
- public string GetCallId()
- public void GetCallHistory()
- public void GetSchedule()
- public void GetBookings(object command)
- public void CheckCurrentHour(object o)
- public void GetPhonebook(string command)
- public void SearchDirectory(string searchString)
- public void GetDirectoryFolderContents(string folderId)
- public void GetDirectoryParentFolderContents()
- public void SetCurrentDirectoryToRoot()
- public void Dial(
			string number,
			string protocol,
			string callRate,
			string callType,
			string meetingId
		)
- public void HoldCall(CodecActiveCallItem activeCall)
- public void ResumeCall(CodecActiveCallItem activeCall)
- public void ResumeAllCalls()
- public void JoinCall(CodecActiveCallItem activeCall)
- public void JoinAllCalls()
- public void SelectPresentationSource(int source)
- public void SetRingtoneVolume(int volume)
- public void SelectPresentationSource1()
- public void SelectPresentationSource2()
- public void VolumeSetToDefault()
- public void LinkCiscoCodecZoomConnector(BasicTriList trilist, CiscoCodecJoinMap joinMap)
- public void LinkCiscoCodecToApi(BasicTriList trilist, CiscoCodecJoinMap joinMap)
- public void SetPresentationLocalOnly()
- public void SetPresentationLocalRemote()
- public void SetPresentationLocalRemoteToggle()
- public void Reboot()
- public void SelfViewModeOn()
- public void SelfViewModeOnForMonitor(string monitorRole, bool fullScreen)
- public void SelfViewModeOff()
- public void SelfViewModeToggle()
- public void SelfviewPipPositionSet(CodecCommandWithLabel position)
- public void SelfviewPipPositionToggle()
- public void LayoutSet(CodecCommandWithLabel layout)
- public void LayoutSet(string layout)
- public void LocalLayoutToggle()
- public void LocalLayoutToggleSingleProminent()
- public void MinMaxLayoutToggle()
- public void PresentationViewDefaultSet()
- public void PresentationViewMinimizedzedSet()
- public void PresentationViewMaximizedSet()
- public void RemoveCallHistoryEntry(CodecCallHistory.CallHistoryEntry entry)
- public void CameraAutoModeToggle()
- public void CameraAutoModeOn()
- public void CameraAutoModeOff()
- public void SpeakerTrackOn()
- public void SpeakerTrackOff()
- public void PresenterTrackOff()
- public void PresenterTrackFollow()
- public void PresenterTrackBackground()
- public void PresenterTrackPersistent()
- public void SetCodecProvisionMode(string mode)
- public void SetCameraName(uint videoConnectorId, string name)
- public void SetInputSourceType(uint videoConnectorId, eCiscoCodecInputSourceType sourceType)
- public void SelectCamera(string key)
- public void CodecRoomPresetSelect(int preset)
- public void CiscoRoomPresetRecall()
- public void CodecRoomPresetStore(int preset, string description)
- public void SelectFarEndPreset(int preset)
- public void AddExternalSource(
			string connectorId,
			string key,
			string name,
			Devices.Common.VideoCodec.Cisco.eExternalSourceType type
		)
- public void SetExternalSourceState(
			string key,
			Devices.Common.VideoCodec.Cisco.eExternalSourceMode mode
		)
- public void ClearExternalSources()
- public void SetSelectedSource(string key)
- public void CameraOff()
- public void CameraMuteOn()
- public void CameraMuteOff()
- public void CameraMuteToggle()
- public void HalfwakeActivate()
- public void UpdateDeviceInfo()
- public void OnCodecInfoChanged(CodecInfoChangedEventArgs args)
- public void ActivateDoNotDisturbMode()
- public void DeactivateDoNotDisturbMode()
- public void ToggleDoNotDisturbMode()
- public void DialPhoneCall(string number)
- public void EndPhoneCall()
- public void SetCurrentSource(eRoutingSignalType signalType, IRoutingSource sourceDevice)
- public void SendDtmfToPhone(string digit)
- public void ShowWebView(string url, string mode, string title, string target)
- public void HideWebView()
- public void SendNavigatorsToPwaUrl(string url)
- public void ExitNavigatorsPwaMode()
- public void ShowEmergencyMessage(string url)
- public void HideEmergencyMessage()
- public void ParseStatus(CiscoCodecEvents.Widget val)
- public void ParseStatus(CiscoCodecEvents.UiEvent val)
- public void RegisterFeedback()
- public void LinkToApi(BasicTriList trilist, CiscoCodecJoinMap joinMap)
- public void UpdateWidget(string val)
- public int WebsocketConnected(WebSocketClient.WEBSOCKET_RESULT_CODES error)
- public int WebsocketDisconnected(WebSocketClient.WEBSOCKET_RESULT_CODES error, object item)
- public int WebsocketReceiveCallback(byte[] data, uint dataLen,
            WebSocketClient.WEBSOCKET_PACKET_TYPES opcode, WebSocketClient.WEBSOCKET_RESULT_CODES error)
- public bool OnResponse(string? result)
- public void HoldInError()
- public void ReleaseHold()
- public void AddCommandToQueue(string query)
- public void LoginMessageReceived()
- public void JsonMessageReceived()
- public void TextOutputReceived()
- public void FullStatusReceived()
- public void InitialStatusMessageReceived()
- public void InitialConfigurationMessageReceived()
- public void InitialSoftwareVersionMessageReceived()
- public void FeedbackRegistered()
- public void SessionLost()
- public void CodecDisconnected()
- public void Push(T message)
- public T Pop()
- public void PostMessage(Action message)
- public void Dispatch()
- public void ParseAuthenticationRequest(JToken token)
- public void ParseAuthenticationResponse(JToken token)
- public void SetPin(string pin)
- public void JoinAsGuest()
- public void JoinAsHost()
- public void JoinAsPanelist()
- public void JoinAsAttendee()
- public void LinkToApi(BasicTriList trilist, CiscoCodecJoinMap joinMap)
- public void ParseStatus(JToken token)
- public void ActivateDoNotDisturbMode()
- public void DeactivateDoNotDisturbMode()
- public void ToggleDoNotDisturbMode()
- public void CameraFactoryReset(uint cameraId)
- public string GetCameraSerialNumber(uint cameraId)
- public void SetCameraAssignedSerialNumber(uint cameraId, string serialNumber)
- public void SetInputCameraId(uint videoConnectorId, uint inputCameraId)
- public void ClearCameraAssignedSerialNumber(uint cameraId)
- public void SetCameraFlip(uint cameraId, bool flip)
- public void SetPresenterTrackConnector(uint connector)
- public void SetPresenterTrackEnabled(bool enabled)
- public void SetId(int id)
- public void PositionHome()
- public void PanLeft()
- public void PanRight()
- public void PanStop()
- public void TiltDown()
- public void TiltUp()
- public void TiltStop()
- public void ZoomIn()
- public void ZoomOut()
- public void ZoomStop()
- public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
- public void SetCameraId(uint id)
- public void SetScenarioCameraId(uint? id)
- public void SetSourceId(uint sourceId)
- public void SetParentCodec(CiscoCodec codec)
- public void SetOnlineStatus(bool isOnline)
- public void SetCapabilites(string capabilites)
- public void PositionHome()
- public void PanLeft()
- public void PanRight()
- public void PanStop()
- public void TiltDown()
- public void TiltUp()
- public void TiltStop()
- public void ZoomIn()
- public void ZoomOut()
- public void ZoomStop()
- public void FocusNear()
- public void FocusFar()
- public void FocusStop()
- public void TriggerAutoFocus()
- public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
- public void Record(string name, double milliseconds)
- public void RecordSince(string name, long startTimestamp)
- public void Start(string name, string? id = null)
- public bool Stop(string name, string? id = null, string? recordAs = null)
- public bool IsPending(string name, string? id = null)
- public void Reset()
- public string FormatTable(string newLine = "\r\n")
- public void Add(double ms)
- public TimingSummary Summarize(string name)
- public string Begin(string kind, TimeSpan? timeout = null)
- public bool TryComplete(string? id, out string kind)
- public int PendingCount()
- public int PendingCount(string kind)
- public void Clear()
- public void On(string path, Action<JToken, XapiMessage> handler)
- public int Dispatch(string document)
- public int Dispatch(XapiMessage message)
- public void AddCustomActivationAction(Action a)
- public void PreActivateAction()
- public void AddCommandToQueue(string command)
- public void ParseStatus(List<WebView.WebView> webViews)
- public bool ParseErrorStatus(JToken statusToken)
- public void ParseStatus(Panels.CiscoCodecEvents.Panel panel)
- public void Initialize(string defaultRoomKey)
- public void ParseStatus(CiscoCodecEvents.Panel panel)
- public bool SubscribeToPanelFeedback(string panelId, bool subscribe)
- public bool SubscribeToPanelFeedback(string panelId)
- public bool UnsubscribeFromPanelFeedback(string panelId)
- public void RegisterFeedback()
- public void DeregisterFeedback()
- public string xCommand()
- public void Activate(NavigatorController parent)
- public void SendWebViewMcUrl(
            string mcPath,
            WebViewDisplayConfig webViewConfig, bool prependmcUrl = true
        )
- public void EnterPwaMode(string url, bool prependmcUrl = true)
- public void ExitPwaMode()
- public void SendWebViewUrl(string url, WebViewDisplayConfig webViewConfig)
- public void ClearWebView()
- public void ClearWebViewOsd()
- public void Activate(NavigatorController parent)
- public void LockoutWebViewChanged(object sender, WebViewChangedEventArgs args)
- public void SendWebViewMcUrl(
            string mcPath,
            WebViewDisplayConfig webViewConfig, bool prependmcUrl = true
        )
- public void SendWebViewUrl(string url, WebViewDisplayConfig webViewConfig)
- public void ClearWebView()
- public void ClearWebViewOsd()
- public void SetAppUrl(string url)
- public void CloseWebViewController()
- public void CloseWebViewOsd()
- public void ShowWebViewOsd(string url, WebViewDisplayConfig webviewConfig)
- public void EnterPwaMode(string url)
- public void ExitPwaMode()
- public void Initialize(CiscoCodec parent, Action<string> enqueueCommand)
- public void Update(Action<string> enqueueCommand)
- public string XCommand()
- public string GetConfiguredCameraName(string cameraKey)
- public string GetPrimaryCameraKey()
- public string GetPresenterTrackCameraKey()
- public void Feed(string text)
- public void Reset(string reason)
- public void Feed(string text)
- public void Reset()
<!-- END Public Methods -->
<!-- START Bool Feedbacks -->
### Bool Feedbacks

- PresentationViewMaximizedFeedback
- PresentationViewMinimizedFeedback
- PresentationViewDefaultFeedback
- RoomIsOccupiedFeedback
- SelfviewIsOnFeedback
- PresentationActiveFeedback
- IsInPwaModeFeedback
- LocalLayoutIsProminentFeedback
- FarEndIsSharingContentFeedback
- CameraAutoModeIsOnFeedback
- SpeakerTrackStatusOnFeedback
- PresenterTrackStatusOnFeedback
- PresenterTrackStatusOffFeedback
- PresenterTrackStatusFollowFeedback
- PresenterTrackStatusBackgroundFeedback
- PresenterTrackStatusPersistentFeedback
- CameraAutoModeAvailableFeedback
- SpeakerTrackAvailableFeedback
- PresenterTrackAvailableFeedback
- DirectorySearchInProgress
- CurrentDirectoryResultIsNotDirectoryRoot
- PresentationSendingLocalOnlyFeedback
- PresentationSendingLocalRemoteFeedback
- ContentInputActiveFeedback
- ControllingFarEndCameraFeedback
- CameraIsOffFeedback
- CameraIsMutedFeedback
- HalfWakeModeIsOnFeedback
- EnteringStandbyModeFeedback
- DoNotDisturbModeIsOnFeedback
- PhoneOffHookFeedback
- DoNotDisturbModeIsOnFeedback
- ScenarioReconciledFeedback
<!-- END Bool Feedbacks -->
<!-- START Int Feedbacks -->
### Int Feedbacks

- PeopleCountFeedback
- RingtoneVolumeFeedback
- PresentationSourceFeedback
<!-- END Int Feedbacks -->
<!-- START String Feedbacks -->
### String Feedbacks

- SelfviewPipPositionFeedback
- LocalLayoutFeedback
- AvailableLayoutsFeedback
- PresenterTrackStatusNameFeedback
- ProvisioningRoomTypeFeedback
- SelectedCameraFeedback
- WidgetEventFeedback
- CallerIdNameFeedback
- CallerIdNumberFeedback
- WidgetEventFeedback
- AppUrlFeedback
<!-- END String Feedbacks -->
