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
    "vlanId": 1, // optional property to specify the VLAN ID for the camera network on a managed switch. Used by the CameraManager
    "commandStallTimeoutSeconds": 30 // optional, see "Send watchdog" below. 0 turns the watchdog off
  }
}
```

### Send watchdog

Commands to the codec go through one queue and one worker. If that worker stops taking commands off the queue, the codec can still look healthy: its status keeps arriving and the communication monitor stays OK. The plugin watches for this.

- When commands have waited `commandStallTimeoutSeconds` (default 30) with none sent, or one command has been running that long, it logs `CODEC_SEND_STALL_START` with the cause and the queue's state.
- It then drops the commands that waited (`CODEC_SEND_STALL_DROPPED`, with the first few named), so an old camera reset or standby isn't sent minutes late, and recovers (`CODEC_SEND_STALL_RECOVERY`): it restarts the worker if it isn't running; a JSON response that never ends is abandoned; and a worker that is blocked or held off reconnects the session. Over RS-232 there is no session to reconnect and a new worker would block the same way, so the current worker is kept and a blocked write is left to finish. It tries again at most every 5 minutes while the stall lasts.
- If the stall is still there `commandStallTimeoutSeconds` after a recovery attempt, it logs `CODEC_SEND_STALL_UNRECOVERED` and the communication monitor reports an error (`devcommstatus`, System Status) until commands go out again. A stall that comes back within 5 minutes of a recovery can't be recovered again yet, so the earlier attempt counts: the error shows `commandStallTimeoutSeconds` into the new stall. Meanwhile the camera manager treats the codec as offline and holds floating-camera recovery for its cameras.
- `CODEC_SEND_STALL_END` is logged when the send worker finishes an action again. After a recovery the watchdog queues a no-op probe to check: an empty queue left by the recovery proves nothing.

Commands dropped because the connection reports not connected, or because the queue is full, are logged as warnings too: once when it starts, every 5 minutes while it lasts, and once when it ends.

`codecperf <codecKey>` shows the send path on its `Send:` line: whether the connection reports connected, how many commands wait and for how long, what the worker is running, how long it has held off for an incoming JSON response, and when text last went out.

## Navigator

Place your custom icon .png files in the /user/programX/navigatorIcons/ folder.
This will automatically generate an output file:
/user/programX/navigatorIcons/icons-base64.txt,
which contains the Base64-encoded "customIconContent" for each icon.

Over SSH, the plugin sends `customIconContent` to the codec on one line. Over RS-232, it splits the content into 76-character lines: RoomOS restarts the serial session when it receives a single line longer than 2,048 bytes (including CR LF), and an icon sent as one line is usually longer than that. When the codec is controlled over RS-232, the plugin logs an error and does not send any command that still has a line over 2,048 bytes.

RoomOS 26.7 rejects the whole UI Extensions set with "Failed to decode data" when the icon content contains line breaks, so on RS-232 a custom icon can stop all panels from loading. The plugin logs this rejection as an error. If you see it, use a built-in icon instead.

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
    "type": "ciscoRoomBar",
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
        "commandStallTimeoutSeconds": 0,
        "endAllCallsOnMeetingJoin": true,
        "usePersistentWebAppForLockout": true,
        "extensions": {
            "Version": "SampleString",
            "ConfigId": "SampleValue",
            "panels": [
                {
                    "id": "SampleString",
                    "Clicked": {
                        "id": "SampleString",
                        "PanelId": {
                            "id": "SampleString",
                            "Value": "SampleString"
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

- ciscoRoomBar
- ciscoCodecPro
- ciscoRoomBarPro
- ciscoRoomOS
- ciscoCodecEq
- ciscoRoomOsMobileControl
- ciscocamera
<!-- END Supported Types -->
<!-- START Join Maps -->

<!-- END Join Maps -->
<!-- START Interfaces Implemented -->
### Interfaces Implemented

- IKeyed
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
- IQueueMessage
- ICiscoCodecCameraFactoryReset
- INavigatorLockoutHandler
- IMobileControlTouchpanelController
- INavigatorLockoutHandlerWithPwa
- IHasCameraPtzControl
- IAmFarEndCamera
- IBridgeAdvanced
- IHasCameraFocusControl
- IDisposable
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- GenericCommunicationMonitor
- JsonConverter
- VideoCodecControllerJoinMap
- VideoCodecBase
- ValueProperty
- EventArgs
- VideoCodecInfo
- EssentialsDevice
- ReconfigurableDevice
- MessengerBase
- CiscoCodecUserInterface
- UserInterfaceConfig
- DefaultContractResolver
- CameraBase
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public void FireUpdate()
- public bool IsHeldFor(CommunicationHoldReason reason)
- public void HoldInError()
- public void ReleaseHold()
- public void HoldInError(CommunicationHoldReason reason)
- public void ReleaseHold(CommunicationHoldReason reason)
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
- public bool TryEnqueue(QueuedAction action)
- public bool TryDequeue(out QueuedAction action)
- public void Clear()
- public void Set()
- public void Wait()
- public void Wait(int timeoutMs)
- public int WebsocketConnected(WebSocketClient.WEBSOCKET_RESULT_CODES error)
- public int WebsocketDisconnected(WebSocketClient.WEBSOCKET_RESULT_CODES error, object item)
- public int WebsocketReceiveCallback(byte[] data, uint dataLen,
            WebSocketClient.WEBSOCKET_PACKET_TYPES opcode, WebSocketClient.WEBSOCKET_RESULT_CODES error)
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
- public void Dispatch()
- public void ParseAuthenticationRequest(JToken token)
- public void ParseAuthenticationResponse(JToken token)
- public void SetPin(string pin)
- public void JoinAsGuest()
- public void JoinAsHost()
- public void JoinAsPanelist()
- public void JoinAsAttendee()
- public void LinkToApi(BasicTriList trilist, CiscoCodecJoinMap joinMap)
- public void PostMessage(Action message)
- public void Start(int port)
- public void Push(T message)
- public T Pop()
- public void ParseStatus(CiscoCodecEvents.Widget val)
- public void ParseStatus(CiscoCodecEvents.UiEvent val)
- public void RegisterFeedback()
- public void LinkToApi(BasicTriList trilist, CiscoCodecJoinMap joinMap)
- public void UpdateWidget(string val)
- public void CameraFactoryReset(uint cameraId)
- public void RequestCameraStatus()
- public string GetCameraSerialNumber(uint cameraId)
- public void SetCameraAssignedSerialNumber(uint cameraId, string serialNumber)
- public void SetInputCameraId(uint videoConnectorId, uint inputCameraId)
- public void ClearCameraAssignedSerialNumber(uint cameraId)
- public void SetCameraFlip(uint cameraId, bool flip)
- public void SetPresenterTrackConnector(uint connector)
- public void SetPresenterTrackEnabled(bool enabled)
- public bool OnResponse(string? result)
- public void SetId(int id)
- public void ParseStatus(JToken token)
- public void ActivateDoNotDisturbMode()
- public void DeactivateDoNotDisturbMode()
- public void ToggleDoNotDisturbMode()
- public SwitchHealthTransition OnCommandStarted(DateTime nowUtc)
- public SwitchHealthTransition OnCommandCompleted(DateTime nowUtc)
- public SwitchHealthTransition Evaluate(DateTime nowUtc)
- public bool TryAcquireProbe(DateTime nowUtc, out DateTime retryAtUtc)
- public bool Observe(string codecKey, bool inCall, int session = 0)
- public void Forget(string codecKey)
- public CodecOutageTransition Update(string codecKey, bool codecOnline, int onlineCameraCount, int onlineAnchorCameraCount, DateTime nowUtc)
- public bool IsInOutage(string codecKey)
- public bool IsHolding(string codecKey)
- public bool IsArmed(string codecKey)
- public DateTime GetOutageSinceUtc(string codecKey)
- public void RecordPortAction(string port, DateTime nowUtc)
- public bool RefreshOnCompletion(string port, DateTime nowUtc)
- public bool IsBooting(string port, DateTime nowUtc, out DateTime bootingUntilUtc)
- public string GetConfiguredCameraName(string cameraKey)
- public string GetPrimaryCameraKey()
- public string GetPresenterTrackCameraKey()
- public bool TryAcquire(string key, DateTime nowUtc)
- public void AddCustomActivationAction(Action a)
- public void PreActivateAction()
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
- public bool CanAttempt(DateTime nowUtc)
- public int Record(DateTime nowUtc)
- public void Reset()
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
- public string xCommand()
- public void Initialize(CiscoCodec parent, Action<string> enqueueCommand)
- public void Update(Action<string> enqueueCommand)
- public string XCommand()
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
- public void Record(string name, double milliseconds)
- public void RecordSince(string name, long startTimestamp)
- public void Start(string name, string? id = null)
- public bool Stop(string name, string? id = null, string? recordAs = null)
- public bool IsPending(string name, string? id = null)
- public void Reset()
- public string FormatTable(string newLine = "\r\n")
- public void Add(double ms)
- public TimingSummary Summarize(string name)
- public bool OnDropped(DateTime nowUtc, out int droppedSinceLastLog)
- public int OnSucceeded()
- public void OnEnqueued(SendQueueKind kind, DateTime nowUtc)
- public void OnEnqueueRejected(SendQueueKind kind)
- public void OnQueuesEmpty()
- public void OnStarted(SendQueueKind kind, string? what, DateTime nowUtc)
- public void OnFinished()
- public void OnFinished(DateTime nowUtc)
- public void OnProbeCompleted()
- public void OnWorkerReplaced()
- public void OnCleared(SendQueueKind kind)
- public void OnJsonHold(DateTime nowUtc)
- public void OnJsonHoldEnded()
- public void OnSent(DateTime nowUtc)
- public SendQueueSnapshot Snapshot(DateTime nowUtc)
- public string Format()
- public SendStallTransition Evaluate(SendQueueSnapshot snapshot, out SendStallCause cause, out TimeSpan stalledFor)
- public bool TryAcquireRecovery(DateTime nowUtc)
- public bool RecoveryFailed(DateTime nowUtc)
- public SendStallCause CauseOf(SendQueueSnapshot snapshot)
- public void Feed(string text)
- public void Reset()
- public void Feed(string text)
- public void Reset(string reason)
- public bool TryComplete(string? resultId)
- public int Reset()
- public SessionStartResult Start(bool jsonModeSet, bool fullStatusReceived)
- public SessionStartResult Adopt(bool fullStatusReceived)
- public string Begin(string kind, TimeSpan? timeout = null)
- public bool TryComplete(string? id, out string kind)
- public bool TryGetKind(string? id, out string kind)
- public int PendingCount()
- public int PendingCount(string kind)
- public int Cancel(string kind)
- public void Clear()
- public void On(string path, Action<JToken, XapiMessage> handler)
- public int Dispatch(string document)
- public int Dispatch(XapiMessage message)
- public bool Enqueue(SendQueueKind kind, Action action, string what, Action<bool>? onResult = null)
- public bool EnqueueSystemOnce(string key, Action action, string? sessionMode = null, Action<bool>? onResult = null)
- public int EnqueueReset(Action action, string what)
- public void RequestProbe(Action probe, string what)
- public void Schedule()
- public void RestartWorker()
- public void Stop()
- public void ClearCommands()
- public int DrainCommands(int describe, out List<string> described)
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
- public void SetBindingState(CameraBindingState state)
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
- public void Idle_Path_Is_Not_Stalled()
- public void Commands_Waiting_Less_Than_The_Threshold_Are_Not_A_Stall()
- public void Waiting_With_An_Idle_Worker_Means_The_Worker_Is_Not_Running()
- public void Worker_Running_One_Action_Too_Long_Is_Blocked_Even_With_Nothing_Waiting()
- public void Busy_Worker_With_A_Short_Wait_Is_Fine()
- public void Waiting_Behind_A_Json_Hold_Is_A_Json_Hold_Stall()
- public void A_Hold_That_Switches_Off_And_On_Still_Counts_As_A_Json_Hold()
- public void An_Old_Json_Hold_Does_Not_Count()
- public void Stall_Is_Reported_Once_Then_Its_End_Once()
- public void Recovery_Is_Allowed_Once_Per_Interval_While_Stalled()
- public void No_Recovery_After_The_Stall_Ends()
- public void Recovery_Has_Failed_Only_After_It_Had_Time_To_Work()
- public void Recovery_Failure_Clears_When_The_Stall_Ends()
- public void A_New_Stall_Starts_Without_The_Old_Recovery_Attempt()
- public void A_Stall_Back_Within_The_Recovery_Interval_Shows_The_Fault()
- public void A_Counted_Attempt_Still_Needs_A_Probe_After_The_Real_One()
- public void A_Counted_Attempt_Ends_Without_A_Probe_When_The_Path_Clears_By_Itself()
- public void Recovery_Restarts_A_Dead_Worker_Reconnects_When_It_Can_And_Keeps_The_Worker_Over_Serial(SendStallCause cause, bool canReconnect, SendStallAction expected)
- public void After_A_Recovery_An_Empty_Queue_Is_Not_Proof_Of_Recovery()
- public void A_Probe_Completed_Before_The_Recovery_Does_Not_Count()
- public void An_Unrelated_Action_Finishing_Does_Not_End_The_Stall()
- public void The_Probe_Completing_After_The_Recovery_Ends_The_Stall()
- public void A_Probe_Finishing_In_The_Same_Clock_Tick_As_The_Recovery_Counts()
- public void Retries_Do_Not_Postpone_The_Fault()
- public void A_Stall_That_Clears_Without_A_Recovery_Needs_No_Completion()
- public void Threshold_Must_Be_Positive()
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
- public void Availability_Maps_To_Capabilities(bool speakerTrack, bool presenterTrack, eCameraTrackingCapabilities expected)
- public void TryGetKind_Reads_Without_Completing()
- public void Cancel_Drops_Only_The_Given_Kind()
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
- public void Dispose()
- public void Runs_System_Actions_Before_Commands_In_Order()
- public void An_Action_That_Throws_Is_Reported_And_The_Worker_Carries_On()
- public void Full_Queue_Drops_Instead_Of_Blocking_The_Caller()
- public void Worker_That_Is_Not_Running_Is_Detected_And_Restarted()
- public void Blocked_Write_Is_Detected_And_A_Reconnect_Ends_It()
- public void Blocked_Write_Over_Serial_Keeps_The_Worker_And_Piles_Up_No_Threads()
- public void Write_Blocked_Under_The_Send_Lock_Over_Serial_Is_Reported_As_Unrecovered()
- public void Json_Response_That_Never_Ends_Is_Detected_And_A_Reconnect_Clears_It()
- public void Healthy_Busy_Worker_Never_Reads_As_A_Stall()
- public void Bookkeeping_Stays_Right_Under_Concurrent_Queuing_Draining_And_Holds()
- public void Reset_Arriving_At_A_Full_System_Queue_Replaces_The_Stale_Actions()
- public void Reset_With_Room_In_The_Queue_Keeps_Its_Place()
- public void A_Replaced_Worker_Never_Takes_Another_Action()
- public void A_Replaced_Worker_Passes_On_A_Wake_Up_It_Took()
- public void The_Probe_Survives_A_Full_System_Queue_And_A_Session_Reset()
- public void Probe_Requests_Wait_As_One_And_Run_Before_Queued_Actions()
- public void A_Stalled_Worker_Does_Not_Fill_The_Queue_With_Copies_Of_A_Sync_Step()
- public void A_Sync_Step_Runs_Once_For_Requests_Made_While_It_Waits_And_Again_For_Later_Ones()
- public void A_Step_Requested_While_It_Runs_Is_Queued_Again()
- public void A_Step_Dropped_By_A_Full_Queue_Is_Not_Marked_Waiting()
- public void A_Step_Asked_For_After_A_Reset_Is_Queued_After_It()
- public void Steps_Keep_Their_Order_Across_A_Session_Mode_Change()
- public void Steps_Still_Coalesce_Across_Steps_That_Are_Not_Mode_Changes()
- public void An_Older_Copy_Does_Not_Release_A_Newer_Copys_Mark()
- public void Steps_Of_The_Same_Session_Mode_Still_Collapse_When_They_Alternate()
- public void Poll_Echoes_Do_Not_Separate_Json_Replies()
- public void A_Coalesced_Request_Reports_No_Result()
- public void Enqueue_Reports_Its_Result_Under_The_Lock()
- public void JoinThreads(TimeSpan timeout)
- public Check Check()
- public Check WatchUntil(Func<Check, bool> match)
- public bool TryEnqueue(QueuedAction action)
- public bool TryDequeue(out QueuedAction? action)
- public void Clear()
- public void Set()
- public void Reset()
- public void Wait()
- public void Wait(int timeoutMs)
- public void Resume(int index)
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
- public void A_Call_Starting_Is_Reported_Once()
- public void A_Call_Already_Up_At_The_First_Reading_Is_Left_Alone()
- public void A_Forgotten_Codec_Starts_Over()
- public void A_Reading_From_A_New_Session_Starts_Over()
- public void Codecs_Are_Tracked_Separately()
- public void Keys_Are_Case_Insensitive()
- public void An_Empty_Key_Is_Ignored()
- public void Configured_Source_Id_Wins()
- public void Unset_Source_Id_Falls_Back_To_Camera_Id()
- public void Nothing_Configured_Stays_Invalid()
- public void Non_Zero_Source_Ids_Are_Valid(uint sourceId)
- public void First_Drop_Of_An_Episode_Is_Logged()
- public void Further_Drops_Are_Quiet_Until_The_Reminder_Interval()
- public void Reminder_Interval_Restarts_From_The_Last_Logged_Drop()
- public void Success_Ends_The_Episode_And_Reports_The_Total()
- public void Next_Episode_Logs_Its_First_Drop_Again()
- public void Success_With_No_Episode_Reports_Nothing()
- public void Reminder_Interval_Must_Be_Positive()
- public void Describe_Keeps_Only_The_First_Words(string? command, string expected)
- public void Describe_Caps_A_Long_Word()
- public void All_Cameras_Lost_After_An_Anchor_Was_Seen_Is_An_Outage()
- public void Outage_Is_Reported_Once_And_Ends_When_A_Camera_Returns()
- public void Codec_With_Only_Moving_Cameras_Is_Never_Armed()
- public void Codec_That_Never_Reported_Cameras_Is_Not_In_Outage()
- public void Codec_Offline_Is_An_Outage_Even_With_Cameras_Listed()
- public void Offline_Codec_Does_Not_Arm_From_Stale_Cameras()
- public void Reason_Follows_The_Current_Cause_Without_A_New_Transition()
- public void Codecs_Are_Tracked_Independently()
- public void Fixed_Managed_Camera_Is_An_Anchor_Only_On_Its_Own_Codec()
- public void Unmanaged_Camera_Is_An_Anchor_On_Any_Codec()
- public void Moving_Camera_Is_Never_An_Anchor()
- public void Fixed_Camera_Visiting_Another_Codec_Does_Not_Arm_It()
- public void Count_Treats_A_Known_Non_Moving_Serial_As_An_Anchor()
- public void Count_Ignores_Disconnected_Cameras()
- public void Count_Does_Not_Treat_An_Unidentified_Camera_As_An_Anchor(string? serial)
- public void Unidentified_Camera_Does_Not_Arm_So_Moving_The_Last_Ptz_Away_Is_Not_An_Outage()
- public void Anchor_Returning_While_The_Ptz_Is_Still_Missing_Ends_The_Outage()
- public void All_Cameras_Lost_Hold_Expires_At_The_Ceiling()
- public void Codec_Offline_Holds_Without_A_Ceiling()
- public void Login_Prompt_Without_Line_Ending_Is_Detected()
- public void Prompt_Split_Across_Chunks_Is_Detected()
- public void Captured_Failed_Login_Sequence()
- public void Lockout_Line_Is_Detected()
- public void Prompt_Is_Signalled_Once_Until_A_New_Line()
- public void Codec_Output_Is_Not_A_Login_Signal(string text)
- public void Long_Unfinished_Line_Ending_In_A_Prompt_Word_Is_Not_A_Prompt()
- public void Prompt_After_A_Long_Line_Is_Detected()
- public void Port_Is_Booting_For_The_Window_After_A_Manager_Port_Action()
- public void A_Later_Port_Action_Restarts_The_Window()
- public void Untouched_Ports_And_Zero_Window_Are_Not_Booting()
- public void Boot_Window_Is_Longer_Than_The_First_Sighting_Grace()
- public void Camera_Connected_On_Its_Target_In_A_Different_Slot_Is_Found()
- public void Target_Codec_Is_Preferred_When_Several_Report_The_Serial()
- public void Camera_Connected_Only_On_Another_Codec_Is_Found_There()
- public void Completion_Restarts_The_Window_From_When_Power_Returns()
- public void Completion_On_A_Port_The_Manager_Never_Touched_Opens_No_Window()
- public void Late_Completion_Never_Moves_The_Window_Backwards()
- public void Evaluate_Classifies_Target_Other_And_Absent()
- public void Not_Found_With_An_Unsampled_Codec_Is_Unknown_Not_Absent()
- public void Found_Is_Still_Reported_When_Another_Codec_Was_Not_Sampled()
- public void Status_Refresh_Is_Rate_Limited_Per_Codec()
- public void Disconnected_Or_Unknown_Serials_Are_Not_Found()
- public void Connected_With_A_Connector_Is_Bound(string connected, string connector)
- public void Connected_With_Connector_Zero_Is_Unbound()
- public void Connected_Without_A_Usable_Connector_Is_Unknown(string connector)
- public void Not_Connected_Is_Disconnected(string connector)
- public void Connected_Not_Reported_Is_Unknown(string connected)
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
- public void Lockout_On_The_Controller_Is_Showing()
- public void Another_Visible_Page_Alone_Is_Not_Showing()
- public void Lockout_On_Top_Of_Another_Page_Is_Showing()
- public void A_NotVisible_Lockout_Does_Not_Count()
- public void A_Lockout_Shown_On_Its_Configured_Target_Counts()
- public void A_Lockout_On_The_Room_Display_Does_Not_Count_For_The_Navigator()
- public void Nothing_On_The_Controller_Is_Not_Showing()
- public void Partial_Feedback_Without_A_Url_Is_Unknown()
- public void A_Missing_Target_Means_Controller()
- public void Any_Entry_Without_A_Url_Makes_The_Message_Partial()
- public void A_Lockout_Still_Loading_Counts_But_No_Status_Does_Not()
- public void A_Longer_Path_Ending_The_Same_Does_Not_Match()
- public void A_Slashless_Configured_Path_Is_Matched_As_Built_Not_As_A_Suffix()
- public void The_Same_Page_Under_Another_App_Path_Does_Not_Match()
- public void A_Query_In_The_Configured_Path_Matches_The_Escaped_Url()
- public void Path_Matching_Ignores_Slashes_Case_And_Query(string lockoutPath)
- public void Percent_Encoded_Paths_Match()
- public void A_Custom_Lockout_Path_Is_Matched_By_Path()
- public void No_Views_Is_Not_Showing_And_No_Path_Is_Unknown()
- public void First_Attempts_Go_Out_Then_At_Most_One_Per_Backoff_Interval()
- public void Checking_Does_Not_Count_Only_Recording_Does()
- public void Reset_Starts_Counting_Again()
- public void UserInterface_WebViews_Maps_The_Codecs_WebView_List()
- public void WebView_Maps_Target()
- public void A_Full_Reply_With_The_Stale_Page_And_The_Lockout_Shows_The_Lockout()
- public void A_One_Item_List_Sent_As_An_Object_Is_Read()
- public void Partial_Status_Feedback_Is_Unknown()
- public void A_Ghost_Only_Message_Lists_Nothing()
- public void Obsolete_UiWebView_Keeps_Its_Public_Setter()
- public void New_Health_Is_Idle_And_Empty()
- public void Waiting_Is_Measured_From_When_The_Queue_Stopped_Being_Empty()
- public void Taking_An_Action_Off_The_Queue_Restarts_The_Wait()
- public void Empty_Queue_Is_Not_Waiting()
- public void Busy_Worker_Reports_What_It_Runs_And_For_How_Long()
- public void Finishing_Records_A_Completion_But_Replacing_The_Worker_Does_Not()
- public void Json_Hold_Is_Measured_From_Its_Start()
- public void Cleared_Queue_Has_Nothing_Waiting()
- public void Pending_Never_Goes_Negative()
- public void Counting_Before_Queuing_Survives_The_Worker_Running_The_Action_First()
- public void Rejected_Enqueue_Is_Taken_Back()
- public void Rejected_Enqueue_Keeps_The_Others_Waiting()
- public void Queues_Found_Empty_Clear_Any_Miscount()
- public void Longest_Waiting_Takes_The_Older_Queue()
- public void Format_Shows_Every_Part()
- public void Clock_Going_Backwards_Reads_As_Zero()
- public void Config_Class_Exists(string className)
- public void Config_Has_Parameterless_Constructor(string className)
- public void Config_Property_Has_JsonPropertyAttribute(string className, string jsonName)
- public void Contact_Info_Number_Is_Used()
- public void Webex_Shared_Sip_Address_Is_Used_Without_Contact_Info()
- public void Document_Without_Contact_Info_Gives_Null()
- public void Registration_Uri_Wins_Then_Contact_Address_Then_Unknown(string registrationUri, string contactAddress, string expected)
- public void Phone_Number_Comes_From_The_Registration_Uri_Only(string registrationUri, string expected)
- public void Webex_Address_Digits_Are_Not_A_Phone_Number()
- public void Document_With_The_Address_Is_Authoritative()
- public void Document_Reporting_No_Address_Clears_It()
- public void Partial_Update_Without_The_Address_Fields_Keeps_The_Cache(string json)
- public void Assembly_Loads_Successfully()
- public void Assembly_Name_Matches_Expected()
- public void Factory_Count_Matches_Expected()
- public void Factory_Exists_ByName(string factoryName)
- public void All_Factories_Have_Parameterless_Constructor()
- public void Three_Codecs_First_Pass_At_Base_Then_Doubling_To_The_Cap(int attempt, int expectedMinutes)
- public void Single_Candidate_Slows_After_The_First_Probe()
- public void No_Candidates_Behaves_Like_One()
- public void Max_Below_Base_Never_Shortens_The_Base_Delay()
- public void Large_Attempt_Numbers_Do_Not_Overflow()
- public void Rotation_With_Nothing_Held_Covers_Every_Candidate_In_Order()
- public void Held_Codec_Is_Removed_Before_Rotating_So_No_Candidate_Repeats()
- public void Probe_Total_Counts_Only_Usable_Candidates()
- public void All_Held_Or_No_Candidates_Selects_Nothing()
- public void Non_Positive_Attempt_Selects_Nothing()
- public void No_Reported_Camera_Is_The_Integrated_Camera_1()
- public void Single_Network_Camera_Without_A_Connector_Uses_Its_Camera_Number()
- public void Detected_Connector_Is_The_Source()
- public void Invalid_Camera_Number_Is_The_Integrated_Camera_1(int cameraNumber)
- public void Negative_Connector_Falls_Back_To_The_Camera_Number()
- public void A_Matched_Reply_Is_Arriving_And_Leaves_The_Tracker()
- public void Other_Replies_Match_Nothing()
- public void Reset_Cancels_The_Request_So_Its_Late_Reply_Matches_Nothing()
- public void Reset_After_A_Match_Clears_Arriving()
- public void A_Reply_In_The_New_Session_Counts()
- public void A_Reset_Waits_Until_A_Match_In_Progress_Has_Marked_The_Reply()
- public void Repeated_Full_Documents_Do_Not_Grow_Any_List()
- public void Partial_Feedback_Updates_Only_That_Item_And_Keeps_The_Count_In_Step()
- public void Excluded_Lists_Are_Left_To_The_Code_That_Maintains_Them()
- public void Wired_Values_Keep_Their_Action_And_Fire_Only_On_A_Real_Change()
- public void An_Action_That_Throws_Is_Reported_And_The_Rest_Of_The_Document_Is_Populated()
- public void Without_A_Failure_Handler_The_Exception_Propagates_As_Before()
- public void Warm_Walks_Past_Accessors_That_Throw_On_Default_Values()
- public void Warm_Runs_No_Actions_And_Counts_Nothing()
- public void Wiring_Is_Not_Codec_Data_So_A_Self_Wired_Model_Warms_And_Keeps_Its_Action()
- public void Whole_Numbers_Of_Any_Type_Are_Inputs(object selector)
- public void Anything_Else_Is_Not(object? selector)
- public void A_Selector_From_Json_Is_A_Long_And_Works()
- public void Healthy_Switch_Grants_Every_Probe()
- public void Switch_Without_Progress_Events_Is_Never_Failing()
- public void New_Session_Starting_While_Previous_Never_Completed_Is_A_Failure()
- public void Session_That_Never_Completes_Fails_After_The_Stale_Window()
- public void Failing_Switch_Holds_Probes_Until_The_Backoff_Elapses_Then_Allows_One_Trial()
- public void Backoff_Doubles_Per_Failure_And_Is_Capped()
- public void Any_Completed_Command_Clears_The_Failure_And_Probes_Resume()
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
- public void Factory_Source_Sets_MinimumEssentialsFrameworkVersion(string factoryName)
- public void Factory_Source_Sets_TypeNames(string factoryName)
- public void Factory_Source_Contains_TypeName(string factoryName, string typeName)
- public void No_Duplicate_TypeNames_Across_Factories()
- public bool Send(string command)
- public bool RequestFullStatus()
- public SessionStarter Starter()
- public void A_Setup_Dropped_Before_Connect_Is_Sent_Again_On_The_Next_Output()
- public void A_Normal_Start_Asks_For_The_Full_Status_Once()
- public void A_Partly_Sent_Setup_Is_Started_Again()
- public void A_Partly_Sent_Setup_Restarted_By_The_Full_Status_Reply_Does_Not_Ask_Again()
- public void An_Existing_Session_Keeps_Its_Claim_When_A_Resend_Is_Dropped()
- public void Json_Mode_Already_Confirmed_Sends_Nothing()
- public void A_Reply_Matched_During_The_Setup_Is_Not_Asked_For_Again()
- public void A_Dropped_Adoption_Restarted_By_A_Poll_Echo_Is_Set_Up_As_An_Adoption()
- public void The_Full_Status_Is_Not_Asked_For_In_Text_Mode()
- public void A_Session_That_Already_Has_Its_Full_Status_Does_Not_Ask_Again()
- public void An_Adopted_Session_Whose_Setup_Was_Dropped_Is_Adopted_Again()
- public void Insert(ushort pos, string val)
- public void Clear()
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
