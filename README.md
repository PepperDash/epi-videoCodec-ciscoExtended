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

The `CameraManager` device interacts with a managed network switch that must implement `INetworkSwitchPoeVlanManager` as well as instances of `CiscoCodec` that implement `ICiscoCodecCameraFactoryReset` and `CiscoCamera`.

Based on the configuration specified, this device will monitor the current room combination scenario and automatically execute the necessary actions on the codec and the network switch to be able to assign a Cisco Vision camera to a different codec at runtime.

The CameraManager config should ONLY list the cameras that need to be moved between codecs.  Any cameras that will be statically assigned to a single codec and not change at runtime should not be included in the `cameraKeys` arrays for any of the `codecConfig` arrays.

In order for this functionality to work, each codec config must specify a `vlanId` property, and each camera config must specify a `networkSwitchPort` property as well as `defaultParentCodecKey`, `defaultCameraId`, and `serialNumber`.

`sourceId` is still used for video-source routing (`SetMainVideoSource`) but is not required by CameraManager migration logic.

```json
{
  "key": "cameraManager1",
  "name": "Camera Manager",
  "type": "cameramanager",
  "properties": {
    "networkSwitchKey": "ciscoSwitch-1",
    "factoryResetSettleMs": 2000,
    "disablePoeCycling": true,
    "useCameraFactoryResetDisconnectFeedback": true,
    "roomCombinerConfig": {
      "roomCombinerKey": "essentialsroomcombiner",
      "combineScenarios": {
        "divided": {
          "codecConfigs": [
            {
              "codecKey": "codecA",
              "cameraKeys": [{ "cameraKey": "cameraA-front", "cameraId": 7 }, { "cameraKey": "cameraA-side", "cameraId": 9 }]
            },
            {
              "codecKey": "codecB",
              "cameraKeys": [{ "cameraKey": "cameraB-front", "cameraId": 7 }, { "cameraKey": "cameraB-rear", "cameraId": 8 }]
            },
            {
              "codecKey": "codecC",
              "cameraKeys": [{ "cameraKey": "cameraC-front", "cameraId": 7 }, { "cameraKey": "cameraC-side", "cameraId": 9 }]
            }
          ]
        },
        "abcCombined": {
          "codecConfigs": [
            {
              "codecKey": "codecA",
              "cameraKeys": [{ "cameraKey": "cameraB-front", "cameraId": 10 }]
            },
            {
              "codecKey": "codecB",
              "cameraKeys": [{ "cameraKey": "cameraC-front", "cameraId": 7 }, { "cameraKey": "cameraC-side", "cameraId": 9 }]
            }
          ]
        },
        "abCombined": {
          "codecConfigs": [
            {
              "codecKey": "codecA",
              "cameraKeys": [{ "cameraKey": "cameraB-front", "cameraId": 10 }]
            },
            {
              "codecKey": "codecB",
              "cameraKeys": [{ "cameraKey": "cameraA-front", "cameraId": 7 }, { "cameraKey": "cameraA-side", "cameraId": 9 }]
            },
            {
              "codecKey": "codecC",
              "cameraKeys": [{ "cameraKey": "cameraC-front", "cameraId": 7 }, { "cameraKey": "cameraC-side", "cameraId": 9 }]
            }
          ]
        },
        "bcCombined": {
          "codecConfigs": [
            {
              "codecKey": "codecA",
              "cameraKeys": [{ "cameraKey": "cameraA-front", "cameraId": 7 }, { "cameraKey": "cameraA-side", "cameraId": 9 }]
            },
            {
              "codecKey": "codecB",
              "cameraKeys": [{ "cameraKey": "cameraC-front", "cameraId": 7 }, { "cameraKey": "cameraC-side", "cameraId": 9 }]
            },
            {
              "codecKey": "codecC",
              "cameraKeys": [{ "cameraKey": "cameraB-front", "cameraId": 10 }]
            }
          ]
        }
      }
    }
  }
}

```

### Per-scenario camera IDs

Each `cameraKeys` entry can be either:

- a string camera key (legacy form), or
- an object with explicit slot override: `{ "cameraKey": "cameraA", "cameraId": 8 }`

Both forms can be mixed in the same `cameraKeys` array.

Resolution order for the slot used on the target codec:

1. If a scenario entry has an explicit `cameraId`, that value wins.
2. If no explicit `cameraId` is present and `maintainConfiguredCameraId` is `true`, the camera uses `defaultCameraId`.
3. If no explicit `cameraId` is present and `maintainConfiguredCameraId` is `false`, CameraManager allocates from the dynamic pool `[7,8,9]`.

During dynamic allocation, CameraManager also tracks short-lived in-flight reservations per codec to avoid giving the same slot to two cameras before codec feedback catches up.

Activation validation now fails fast when:

- a scenario camera assignment sets `cameraId: 0`
- a single camera key is assigned to multiple codecs in the same scenario
- two cameras on the same codec resolve to the same effective slot (`explicit cameraId` or `defaultCameraId` fallback)

Cameras omitted from a scenario remain unmanaged in that scenario.

In the example above, `cameraId: 10` is used to park `cameraB-front` on a non-home codec during combined scenarios so it stays attached and reachable without colliding with the active in-room camera slots.

### Migration behavior and recovery

When the room combination scenario changes, the `CameraManager` moves each affected camera to the codec that owns it in the new scenario. A migration is only ever started after the camera is **confirmed online** on its current (wrong) codec, then runs a feedback-driven cascade: factory-reset the camera on the source codec → disable PoE and clear the source codec's assigned serial → switch the switch port to the target codec's VLAN → re-enable PoE → wait for the target codec to report the camera attached.

The factory reset and source serial-clear always target the **actual live slot** the camera occupies on the source codec, resolved by serial number at migration start — never a stale or target slot. A moving camera is pinned to that live source slot for the duration of the migration, so source-side self-heal stays a no-op and never rewrites the source codec while the camera is still on it. The target slot is only pinned once the camera attaches to the target codec.

Assigning the camera's serial to its target slot is **confirmation-driven**: if the target slot is still occupied by another camera (for example, the previous occupant is mid-migration out of that codec), the assignment is deferred and tracked against the blocking camera rather than overwriting the slot. The wait is released by real codec events (the blocker camera disconnecting/connecting, or its slot's assigned-serial clearing), so the manager never force-clears a slot that another camera's migration is still responsible for.

After the factory reset is issued, the manager waits `factoryResetSettleMs` (default 2000 ms) before tearing down PoE and moving the camera. The reset clears the camera's pairing/authentication to the source codec, and that takes time to take effect; if the camera is moved too soon it arrives at the target codec still carrying the source codec's credentials, and the target reports `authentication failed, pinhole factory reset required`. Note that the source codec's connected feedback is not a reliable signal for this — the codec keeps reporting the camera connected until it actually reboots — so a fixed settle delay is used instead. If you still see the pinhole diagnostic on the first attach attempt, increase `factoryResetSettleMs`.

Alternatively, set `useCameraFactoryResetDisconnectFeedback` to `true` to gate the move on the source codec's **disconnect** feedback instead of the fixed timer. In this mode the manager waits for the source codec to report the camera disconnected — the moment the factory reset actually takes effect and the camera drops/reboots — and then starts the PoE/VLAN cascade immediately, rather than waiting out `factoryResetSettleMs`. This adapts to how long a given camera actually takes to reset instead of guessing. A bounded fallback still starts the cascade if no disconnect is reported within 25 seconds, so a missed event cannot stall the migration.

By default the cascade power-cycles (PoE off → VLAN → PoE on) the switch port to force the camera to reboot onto the new VLAN. Set `disablePoeCycling` to `true` to suppress PoE off/on in the main migration cascade and attach-failure reseed path; those flows then move cameras by VLAN change and serial reassignment only. The periodic floating-camera watchdog currently still uses `probeAndBouncePoe` attempts during recovery. Use `disablePoeCycling` when the normal migration path must keep PoE on (for example when the camera is powered/managed elsewhere, or the switch performs the power cycle itself on a VLAN change). The state machine is advanced internally at the points the PoE feedback events would normally drive it.

Alongside the migration cascade, the manager runs a **port-state reconciliation** pass that re-asserts the switch port VLAN (and PoE, unless `disablePoeCycling` is set) to keep already-placed cameras aligned with the active scenario. This pass is intentionally conservative: it only re-asserts the port for a camera that is **confirmed online on its target codec**. A camera that is not yet on its target codec is left on its current VLAN (logged as `CAMERA_PORT_ENSURE ... action='skipped' reason='notOnlineOnTargetCodec'`) so that it stays visible to its source codec and the normal confirmed-online migration cascade can move it. This avoids stranding a camera on the target VLAN before the migration has run, which would hide it from the source codec and prevent the migration from starting.

If the target codec does not report the camera attached within the attach-wait window, the manager performs an **automatic recovery**: it reseeds the camera back onto the source codec's VLAN with PoE on. The source codec then rediscovers the camera, which triggers a fresh migration cascade from scratch. This reseed-and-retry is the only action proven to recover a stuck attach; it repeats until the camera attaches. The retry loop is scoped to the single affected camera's switch port and does not disturb other cameras or codecs.

The number of failed attach attempts is tracked per camera and reported in the logs (`failedAttempts`) on `CAMERA_SWITCHOVER_ATTACH_FAILED`, `CAMERA_SWITCHOVER_ATTACH_AUTOMAGIC_RECOVERY_TRIGGERED`, and `CAMERA_SWITCHOVER_ATTACH_CONFIRMED`, so it is visible how many retries a recovery required.

A periodic **safety-net reconciliation sweep** guards against the case where a reseed never recovers — for example, a camera that, after being pushed back to the source codec, never re-registers (firmware hang, dead NIC, or a cable/PoE fault), leaving it floating with no active migration to retry. The sweep runs on a fixed interval and, for each scenario camera that is not confirmed online on its target codec and has no active migration, takes one of two actions after a per-camera backoff: if the camera is found online on the wrong codec it starts a migration (`CAMERA_SWITCHOVER_RECONCILE_WRONG_CODEC`); if it is online nowhere it applies a bounded floating-recovery policy (`CAMERA_SWITCHOVER_RECONCILE_FLOATING`) that cycles the camera port VLAN across probe codecs (target codec first, then other scenario codecs, then remaining managed codecs) and performs PoE bounce on each attempt (`action='probeAndBouncePoe'`). After **12 total attempts**, the watchdog stops retrying that camera (`reason='attemptLimitReached'`).

Floating recovery state is scoped to the active room-combiner scenario and is cleared on scenario change, so retries from a previous combination do not leak into a new one.

When any managed camera reports connected, the manager immediately re-runs reconciliation for the current scenario. This ensures post-recovery cases where a camera reappears on the wrong codec are corrected right away instead of waiting for the next periodic sweep.

### Codec diagnostics logging

The codec subscribes to `/Status/Diagnostics` and logs each diagnostics message the codec reports (`Type`, `Level`, `Description`, `References`). `Error`/`Critical` messages are logged as errors, `Warning` as warnings, and others as information; cleared (ghosted) messages are logged at debug level. This surfaces camera-related conditions such as `CameraAuthentication`, `CameraPairing`, and `CameraSerial` failures (for example, a camera that requires a physical pinhole factory reset) that the migration logic cannot resolve on its own.

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
