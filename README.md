# Cisco RoomOS Video Codec Plugin - Configuration Guide

This plugin provides comprehensive device control over the Cisco Webex family of video conference codecs. It implements all available Essentials interfaces relevant to video codec devices and supports a wide range of Cisco Room OS codec models.

<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- **PepperDash Essentials 2.7.0** or later

| Plugin Version | Minimum Essentials Version |
|---|---|
| 2.0.x | 2.7.0 |
| 1.11.x, 1.10.x, 1.9.x | 2.7.0 |
| 1.8.x, 1.7.x | 2.3.0 |
| 1.6.x | 2.1.0 |
| 1.5.x & below | 1.16.x |

<!-- END Minimum Essentials Framework Versions -->

<!-- START IMPORTANT -->
### ⚠️ IMPORTANT: SSH Connection Requirements

This plugin communicates with Cisco RoomOS devices exclusively via **SSH protocol**. Ensure that:
- SSH is enabled on the target Cisco codec device
- Port 22 is accessible from the control system
- Valid SSH credentials (username/password) are configured
- The codec firmware supports SSH connectivity (tested on Room OS 10.11.5.2 and later)
- The control system has persistent network connectivity to the device

**The plugin does NOT support HTTP, COM, or other communication methods — SSH over TCP/IP is required for all device communication.**

<!-- END IMPORTANT -->

<!-- START Supported Types -->
### Supported Types

**Device Types:**

| Device Type | Description |
|-------------|-------------|
| `ciscoRoomOS` | Generic Cisco Room OS codec (primary type) |
| `ciscoRoomBar` | Cisco Room Bar video codec |
| `ciscoRoomBarPro` | Cisco Room Bar Pro video codec |
| `ciscoCodecEq` | Cisco Codec EQ video codec |
| `ciscoCodecPro` | Cisco Codec Pro video codec |

All supported codec models provide identical control and monitoring capabilities through this plugin. Use the type name that corresponds to your specific hardware. All device types communicate exclusively via SSH.

<!-- END Supported Types -->

<!-- START Config Example -->
### Config Examples

#### SSH Configuration (Network)

**Basic SSH Connection:**

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
    }
  }
}
```

**Note:** The SSH connection method is **required**. Ensure port 22 is open and SSH is enabled on the codec.

**Full Configuration (With Optional Features):**

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
    "phonebookMode": "Corporate",
    "phonebookDisableAutoPopulate": true,
    "phonebookDisableAutoDial": false,
    "phonebookResultsLimit": 50,
    "getPhonebookOnStartup": true,
    "getBookingsOnStartup": true,
    "showSelfViewByDefault": true,
    "selfViewDefaultMonitorRole": "OSD",
    "defaultTrackingMode": "SpeakerTrack",
    "overrideMeetingsLimit": true,
    "joinableCooldownSeconds": 0,
    "endAllCallsOnMeetingJoin": false,
    "sharing": {
      "autoShareContentWhileInCall": false,
      "defaultShareLocalOnly": true
    },
    "externalSourceListEnabled": true,
    "externalSourceInputPort": "HDMI1",
    "cameraInfo": [
      {
        "CameraNumber": 1,
        "Name": "Audience"
      },
      {
        "CameraNumber": 2,
        "Name": "Presenter"
      }
    ]
  }
}
```

<!-- END Config Example -->

<!-- START Device Series Capabilities -->
### Device Characteristics

All Cisco Room OS codecs supported by this plugin share the following capabilities:

| Capability | Status | Notes |
|---|---|---|
| SSH Communication | ✓ | Primary communication method, required |
| Call History | ✓ | Tracked and accessible via history joins |
| Directory/Phonebook | ✓ | Corporate (LDAP) or Local mode |
| Meeting Scheduling | ✓ | Calendar awareness and joinable meeting detection |
| Camera Control | ✓ | PTZ, presets, auto-tracking (SpeakerTrack/PresenterTrack) |
| Content Sharing | ✓ | Local and remote sharing modes |
| Room Presets | ✓ | Save and recall room configurations |
| External Source Switching | ✓ | HDMI/input routing support |
| Occupancy Sensing | ✓ | People count and room occupancy feedback |
| Do Not Disturb | ✓ | Meeting suppression and presence control |
| Half-Wake Mode | ✓ | Low-power standby with wake capability |
| WebEx Integration | ✓ | WebEx PIN handling and guest join support |
| UI Extensions | ✓ | Custom panel and widget support |
| Emergency OSD | ✓ | Emergency on-screen display capability |
| WebView Support | ✓ | Web-based UI and navigation support |

<!-- END Device Series Capabilities -->

<!-- START Core Properties -->
### Core Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `control` | Object | ✓ | SSH connection configuration (method, address, port, credentials) |
| `phonebookMode` | String | ✗ | Directory mode: "Local" or "Corporate" (default: "corporate") |
| `phonebookDisableAutoPopulate` | Boolean | ✗ | Disable automatic phonebook loading on startup |
| `phonebookDisableAutoDial` | Boolean | ✗ | Disable automatic dialing when selecting directory contacts |
| `phonebookResultsLimit` | Integer | ✗ | Maximum results for directory queries (default: 50) |
| `getPhonebookOnStartup` | Boolean | ✗ | Fetch phonebook when device starts (default: true) |
| `getBookingsOnStartup` | Boolean | ✗ | Fetch calendar bookings when device starts (default: true) |
| `showSelfViewByDefault` | Boolean | ✗ | Enable self-view on startup |
| `selfViewDefaultMonitorRole` | Enum | ✗ | Monitor for self-view: "OSD", "Primary", or "Secondary" |
| `defaultTrackingMode` | String | ✗ | Camera tracking: "SpeakerTrack" or "PresenterTrack" |
| `overrideMeetingsLimit` | Boolean | ✗ | Allow more than default meeting count display |
| `joinableCooldownSeconds` | Integer | ✗ | Seconds before a meeting becomes joinable |
| `endAllCallsOnMeetingJoin` | Boolean | ✗ | End all existing calls when joining a meeting |
| `sharing` | Object | ✗ | Content sharing settings (autoShare, defaultShareLocalOnly) |
| `externalSourceListEnabled` | Boolean | ✗ | Enable external source switching |
| `externalSourceInputPort` | String | ✗ | Input port name for external source routing (e.g., "HDMI1") |
| `cameraInfo` | Array | ✗ | Camera configuration with CameraNumber and Name for each camera |

<!-- END Core Properties -->

### Property Details

#### Communication Configuration (`control` - REQUIRED)

- **`method`:** Must be set to `"Ssh"` for SSH connectivity
- **`endOfLineString`:** Line terminator sent to device (typically `"\n"`)
- **`deviceReadyResponsePattern`:** Pattern to detect device ready state (empty for auto-detection)
- **`tcpSshProperties`:** SSH connection parameters:
  - **`address`:** IP address or hostname of the codec
  - **`port`:** SSH port (default: 22)
  - **`username`:** SSH login credential
  - **`password`:** SSH password
  - **`autoReconnect`:** Automatically reconnect if connection drops (recommended: true)
  - **`AutoReconnectIntervalMs`:** Milliseconds between reconnection attempts (default: 10000)

#### Phonebook & Directory Configuration

- **`phonebookMode`:** Selects directory source:
  - `"Corporate"` - Use corporate directory/LDAP (default)
  - `"Local"` - Use codec's local phonebook
- **`phonebookDisableAutoPopulate`:** Set to `true` to skip phonebook loading during startup (improves startup speed for large directories)
- **`phonebookDisableAutoDial`:** Set to `true` to require explicit dial action instead of auto-dialing selected contacts
- **`phonebookResultsLimit`:** Maximum contacts returned in search results. Reduce for large directories to improve performance (recommended: 50)
- **`getPhonebookOnStartup`:** Default `true`. Set to `false` if phonebook is very large or updates are handled separately

#### Self-View & Display Configuration

- **`showSelfViewByDefault`:** Display self-view when codec starts
- **`selfViewDefaultMonitorRole`:** Monitor placement for self-view window:
  - `"OSD"` - Overlay on-screen display
  - `"Primary"` - Main display monitor
  - `"Secondary"` - Secondary display monitor

#### Camera Tracking Configuration

- **`defaultTrackingMode`:** Sets preferred automatic camera tracking when auto-mode is enabled:
  - `"SpeakerTrack"` - Track active speaker (default)
  - `"PresenterTrack"` - Track presentation source
- **`cameraInfo`:** Array of camera configuration objects with:
  - **`CameraNumber`:** Camera index (1-based)
  - **`Name`:** Display name for the camera

#### Content Sharing Configuration (`sharing` object)

- **`autoShareContentWhileInCall`:** Automatically start sharing when entering a call
- **`defaultShareLocalOnly`:** When enabled, sharing is limited to local layout (not sent to far end)

#### Meeting & Scheduling Configuration

- **`getBookingsOnStartup`:** Default `true`. Fetch calendar meetings on startup
- **`joinableCooldownSeconds`:** Cooldown period before newly scheduled meetings can be joined (default: 0)
- **`endAllCallsOnMeetingJoin`:** Drop all active calls when joining a scheduled meeting
- **`overrideMeetingsLimit`:** Allow displaying more meetings than the codec's default limit

#### External Source Switching Configuration

- **`externalSourceListEnabled`:** Enable external input switching capability
- **`externalSourceInputPort`:** Name of the codec's input port connected to external switching (e.g., "HDMI1", "HDMI2")

<!-- START Join Maps -->
### Join Maps

#### Digitals

| Join | Direction | Description |
|------|-----------|-------------|
| 1 | R | Device is Online |
| 10 | W | DTMF Target Selection (if High, sends to SelectCall index; if Low, sends to last connected call) |
| 11-22 | W | DTMF Tones (1-9, 0, *, #) |
| 24 | W | End All Calls |
| 31 | R | Current Hook State |
| 41-44 | W | Speed Dial (4-button array) |
| 50 | R | Incoming Call Detection |
| 51 | W | Answer Incoming Call |
| 52 | W | Reject Incoming Call |
| 71 | W | Dial Manual String (from CurrentDialString serial) |
| 72 | W/R | Dial Phone / Phone Dial State |
| 73 | W | Hang Up Phone |
| 81-88 | W | End Specific Call by Index (8-button array) |
| 90 | W | Join All Calls |
| 91-98 | W | Join Specific Call by Index (8-button array) |
| 100 | R | Directory Search Busy |
| 101 | R/W | Directory Selected Entry is Contact / Directory Line Selected |
| 102 | R | Directory is at Root Level |
| 103 | R | Directory Content Changed |
| 104 | W | Directory Go to Root |
| 105 | W | Directory Go Back One Level |
| 106 | W | Dial Selected Directory Entry |
| 107 | W | Disable Auto-Dial Mode |
| 108 | W | Dial Selected Contact Method |
| 110 | W | Clear Search Selection & String |
| 111-119 | W | Camera Control (Tilt Up/Down, Pan Left/Right, Zoom In/Out, Focus Near/Far, Auto Focus) |
| 121 | ↔ | Save Camera Preset (pulses for 3s when saved) |
| 131 | ↔ | Camera Mode Auto (with feedback) |
| 132 | ↔ | Camera Mode Manual (disables auto tracking) |
| 133 | ↔ | Camera Mode Off (video mute equivalent) |
| 134-137 | ↔ | Presenter Track Modes (Off, Follow, Background, Persistent) |
| 138 | ↔ | SpeakerTrack On |
| 139 | ↔ | SpeakerTrack Off |
| 140 | W | SpeakerTrack Toggle |
| 141 | ↔ | Self-View Toggle with Feedback |
| 142 | W | Layout Toggle |
| 143 | R | Camera Supports Auto Mode (feedback only) |
| 144 | R | Camera Supports Off Mode (feedback only) |
| 145 | R | SpeakerTrack Available (feedback only) |
| 146 | R | PresenterTrack Available (feedback only) |
| 159 | W | PresenterTrack Command |
| 160 | W | Update Meetings |
| 161-170 | W | Join Meeting by Index (10-button array) |
| 171-173 | ↔ | Microphone Mute (On, Off, Toggle) |
| 174-175 | W | Volume Up / Down |
| 176-178 | ↔ | Speaker Mute (On, Off, Toggle) |
| 181 | W | Remove Selected Recent Call Item |
| 182 | W | Dial Selected Recent Call Item |
| 200 | R | Presentation Active |
| 201 | ↔ | Start Sharing |
| 202 | ↔ | Stop Sharing |
| 203 | W | Auto-Start Sharing When in Call |
| 204 | R | Receiving Content from Far End |
| 205 | ↔ | Presentation Local Only Mode |
| 206 | ↔ | Presentation Local and Remote Mode |
| 207 | W | Presentation Mode Selection |
| 211 | W | Toggle Self-View Position |
| 220 | W | Hold All Calls |
| 221-228 | W | Hold Specific Call by Index (8-button array) |
| 230 | W | Resume All Held Calls |
| 231-238 | W | Resume Specific Call by Index (8-button array) |
| 241 | ↔ | Do Not Disturb On |
| 242 | ↔ | Do Not Disturb Off |
| 243 | R | Do Not Disturb Toggle State (feedback only) |
| 246 | ↔ | Standby Mode On |
| 247 | ↔ | Standby Mode Off |
| 248 | ↔ | Half-Wake Mode |
| 249 | R | Entering Standby Mode (feedback only) |
| 251 | R | No Current Meetings (feedback only) |
| 252 | R | Current Active Meetings (feedback only) |
| 253 | R | Impending Meeting (feedback only) |
| 261 | ↔ | Presentation View Default Mode |
| 262 | ↔ | Presentation View Maximized Mode |
| 263 | ↔ | Presentation View Minimized Mode |
| 301 | R | Multi-Site Option Enabled (feedback only) |
| 302 | R | Auto-Answer Enabled (feedback only) |
| 311 | R/W | WebEx PIN Requested / Send PIN |
| 312 | W/R | Join as Guest / Joined as Host |
| 313 | W/R | Clear PIN / Joined as Guest |
| 314 | R | WebEx PIN Error (feedback only) |
| 501-550 | R | Participant Audio Mute Status (50-participant array) |
| 801-850 | R | Participant Video Mute Status (50-participant array) |
| 1101-1150 | R | Participant Pin Status (50-participant array) |

#### Analogs

| Join | Direction | Description |
|------|-----------|-------------|
| 21 | ↔ | Ringtone Volume (0-100, increments of 5: 5, 10, 15, 20, etc.) |
| 24 | W | Select Call for DTMF Commands (1-8) |
| 25 | R | Current Connected Call Count |
| 40 | ↔ | Meetings to Display via Bridge (default: 3) |
| 41 | W | Minutes Before Meeting Start for Join-Able Window |
| 42 | R | Total Minutes Until Next Meeting |
| 43 | R | Hours Until Next Meeting |
| 44 | R | Minutes Until Next Meeting |
| 60 | ↔ | Camera Selection (1-based index, 1 to CameraCount) |
| 61 | R | Camera Count |
| 101 | W/R | Directory Row Selection / Directory Row Count |
| 102 | W | Contact Methods Count for Selected Contact |
| 103 | W | Select Contact Method by Index |
| 104 | R | Directory Row Feedback |
| 121 | R | Camera Preset Selection |
| 122 | R | Far-End Preset Selection |
| 151 | R | Current Participant Count in Call |
| 161 | R | Meeting Count |
| 174 | ↔ | Speaker Volume Level |
| 180 | ↔ | Recent Call Item Selection (1-10) |
| 181-190 | R | Recent Call Occurrence Type (0=Unknown, 1=Placed, 2=Received, 3=NoAnswer) |
| 191 | R | Recent Call Count |
| 201 | ↔ | Presentation Selection (0-6, codec model dependent) |

#### Serials

| Join | Direction | Description |
|------|-----------|-------------|
| 1 | R | Current Dial String Value |
| 2 | W/R | Phone Dial String / Current Call Data (XSIG) |
| 5 | W | Send Command to Device (without delimiter) |
| 22 | R | Current Call Direction |
| 51 | R | Incoming Call Name |
| 52 | R | Incoming Call Number |
| 100 | W | Directory Search String |
| 101 | R | Directory Entries (XSIG, 255 entries) |
| 102 | R | Schedule Data (XSIG) |
| 103 | R | Contact Methods (XSIG, 10 entries) |
| 104 | R | Active Meeting Data (XSIG) |
| 105 | R | Time Until Room Availability |
| 106 | R | Time to Next Meeting |
| 121 | R | Camera Preset Names (XSIG, max 15) |
| 141 | R | Current Layout Name |
| 142 | W/R | Select Layout by String / Available Layouts (XSIG) |
| 151 | R | Current Participants (XSIG) |
| 161-170 | R | Camera Names (10-element array) |
| 171 | R | Selected Recent Call Name |
| 172 | R | Selected Recent Call Number |
| 181-190 | R | Recent Call Names (10-element array) |
| 191-200 | R | Recent Call Times (10-element array) |
| 201 | R | Current Content Source |
| 211 | R | Self-View Position |
| 301 | R | Device IP Address |
| 302 | R | SIP Phone Number |
| 303 | R | E.164 Alias |
| 304 | R | H.323 ID |
| 305 | R | SIP URI |
| 311 | W | WebEx PIN (send) |
| 321 | R | Widget Event Data |
| 356 | R | Selected Directory Entry Name |
| 357 | R | Selected Directory Entry Number |
| 358 | R | Selected Directory Folder Name |

> **Note:** Using Camera Mode Auto/Manual/Off joins (131-133) relies on tracking capabilities reported by the codec. If only SpeakerTrack is available, auto mode enables SpeakerTrack. If only PresenterTrack is available, auto mode enables PresenterTrack. If both are available, the `defaultTrackingMode` configuration value determines which is preferred.

<!-- END Join Maps -->

### Join Details

**Call Management Joins (1-100):**
- Joins 1-100 handle device online status, DTMF tone selection, call termination, call joining, and directory operations
- Hook state (31) reflects current call status
- Speed dials (41-44) provide quick access to frequently used numbers
- Directory joins (100-110) manage phonebook search and navigation

**Meeting & Scheduling Joins (160-253):**
- Join 160 updates meeting list from device
- Joins 161-170 allow joining meetings by array index
- Meets and scheduling status feeds (251-253) report meeting availability

**Presenter/Speaker Tracking Joins (131-146):**
- Joins 131-133 control camera auto/manual/off modes with feedback
- Joins 134-137 configure PresenterTrack modes
- Joins 138-140 control SpeakerTrack with feedback
- Joins 143-146 report capability availability

**Audio/Video Control Joins (171-178, 200-207):**
- Microphone control (171-173) for mute operations
- Volume control (174-175) for speaker level adjustment
- Speaker mute (176-178) for audio output control
- Presentation joins (200-207) manage content sharing modes

**Presence & Do Not Disturb Joins (241-248):**
- Join 241 activates Do Not Disturb
- Join 242 deactivates Do Not Disturb
- Joins 246-248 manage Standby and Half-Wake modes

**Participant Status Joins (501-550, 801-850, 1101-1150):**
- 50-element arrays for participant mute status (audio, video, pin)
- Enables participant-level control in multi-party calls

<!-- START Interfaces Implemented -->
### Interfaces Implemented

- `IHasCallHistory` - Call history tracking and management
- `IHasCallFavorites` - Favorite destinations and quick dial
- `IHasDirectory` - Directory and phonebook integration
- `IHasScheduleAwareness` - Meeting schedule monitoring
- `IOccupancyStatusProvider` - Room occupancy detection
- `IHasCodecLayoutsAvailable` - Display layout management
- `IHasCodecSelfView` - Self-view configuration and control
- `ICommunicationMonitor` - Communication status and health monitoring
- `IRoutingSinkWithSwitchingWithInputPort` - Input routing and HDMI switching
- `IRoutingSource` - Source routing capabilities for content sharing
- `IHasCodecCameras` - PTZ camera control and management
- `IHasCameraAutoMode` - Automatic camera tracking modes
- `IHasCodecRoomPresets` - Room configuration presets
- `IHasExternalSourceSwitching` - External HDMI/input source selection
- `IHasBranding` - Custom branding and logo support
- `IHasCameraOff` / `IHasCameraMute` - Camera disable/mute functionality
- `IHasDoNotDisturbMode` - Do Not Disturb mode control
- `IHasHalfWakeMode` - Low-power standby with wake capability
- `IHasCallHold` / `IJoinCalls` - Call hold and multi-call joining
- `ISpeakerTrack` / `IPresenterTrack` - Advanced camera tracking modes
- `IHasPhoneDialing` - Phone number dialing capability
- `IDeviceInfoProvider` - Device information and status reporting
- `ICiscoCodecCameraConfig` - Cisco-specific camera configuration
- `IEmergencyOSD` - Emergency on-screen display support
- `IHasWebView` - WebView and UI extension support
- `IKeyed` - Device key identification (implemented by all components)

<!-- END Interfaces Implemented -->

<!-- START Base Classes -->
### Base Classes

**Device Base Classes:**
- `VideoCodecBase` - Core video codec functionality and state management

**Communication & Monitoring:**
- `CommunicationMonitorBase` - SSH communication status tracking
- `StatusMonitorBase` - Device health and connectivity monitoring
- `CommunicationGather` - Raw command/response handling for SSH protocol

**Support Classes & Handlers:**
- `WebexPinRequestHandler` - WebEx PIN request processing and PIN entry management
- `DoNotDisturbHandler` - Do Not Disturb mode scheduling and state tracking
- `UIExtensionsHandler` - Custom UI panel and widget event management

**Messenger Classes (MessengerBase derivatives):**
- `ISpeakerTrackMessenger` - SpeakerTrack mode messaging and feedback
- `IPresenterTrackMessenger` - PresenterTrack mode messaging and feedback
- `NavigatorMessenger` - Navigator UI messaging and control
- `McVideoCodecUserInterfaceControlMessenger` - Mobile control UI messaging

**Configuration Classes:**
- `CiscoCodecConfig` - Main codec configuration object with all properties
- `SharingProperties` - Content sharing settings
- `CameraInfo` - Individual camera configuration
- `BrandingLogoProperties` - Custom branding configuration
- `WidgetConfig` - Widget configuration for UI extensions
- `Emergency` - Emergency OSD configuration

**Data Classes:**
- `CiscoCodecStatus` - Device status representation (xStatus)
- `CiscoCodecEvents` - Device event data (xEvent)
- `CiscoCodecExtendedPhonebook` - Phonebook and directory data
- `CodecActiveCallItem` - Call item in history/favorites
- `Meeting` - Meeting/booking data structure
- `CodecCommandWithLabel` - Layout/preset command with display label

<!-- END Base Classes -->

<!-- START Routing Framework -->
### Routing Framework & Architecture

**Input Routing Architecture:**
- `CurrentInputPort` property tracks the currently selected input source
- `InputChangedEventHandler` event notifies subscribers when input changes
- `IRoutingSinkWithSwitchingWithInputPort` interface enables bridge-controlled input switching
- External source switching routed through `externalSourceInputPort` configuration

**Output/Presentation Routing:**
- `IRoutingSource` interface enables codec as content source for other devices
- Presentation state tracked via `PresentationActiveFeedback`
- Local vs. Remote presentation modes controlled via joins 205-206
- Layout selection affects presentation source routing

**Camera Routing & Control:**
- Multiple cameras supported via `cameraInfo` configuration array
- Camera selection via analog join 60 (1-based index)
- Auto-tracking via SpeakerTrack/PresenterTrack modes
- Camera presets enable preset-based positioning

**Feedback Architecture:**
- Bool Feedbacks: Device online, call states, mute states, mode selections
- Int Feedbacks: Volume levels, participant counts, meeting counts, time values
- String Feedbacks: Phone numbers, caller names, layout names, device IDs
- XSIG Feedbacks: Complex data structures (participant lists, meeting details, directory entries)

**Bridge Integration:**
- EISC Advanced bridges support join mapping for codec control
- Communication monitor device enables separate EISC bridge for SSH status
- Multiple bridge instances can control same codec via different join starts

<!-- END Routing Framework -->

<!-- START Configuration Best Practices -->
### Configuration Best Practices

**SSH Connectivity:**
- Verify SSH is enabled on the codec (Administration > SSH Settings)
- Use strong SSH credentials; avoid default credentials in production
- Ensure firewall rules allow SSH (port 22) between control system and codec
- Set `autoReconnect: true` for resilient connection handling
- Use a reconnection interval of 10-30 seconds (`AutoReconnectIntervalMs`)
- Test SSH connectivity before full system deployment
- Document SSH login credentials securely separate from configuration files

**Directory & Phonebook Configuration:**
- For large corporate directories (>500 contacts), set `phonebookResultsLimit` to 50 or lower to improve search responsiveness
- Set `phonebookDisableAutoPopulate: true` if phonebook loading impacts startup performance
- Use `phonebookMode: "Corporate"` for LDAP/directory integration; use `"Local"` for codec-stored contacts only
- Verify directory connectivity with codec before deployment
- Test phonebook search functionality with sample contacts

**Meeting & Scheduling:**
- Enable `getBookingsOnStartup: true` for calendar-aware features (default)
- Set `joinableCooldownSeconds` to 5-15 minutes before meeting start for better UX
- Use `endAllCallsOnMeetingJoin: false` to preserve active calls when joining meetings
- Verify calendar integration with codec calendar source (Exchange, Google Calendar, etc.)
- Test meeting join functionality before go-live

**Camera Configuration:**
- Always configure `cameraInfo` with actual camera names for better user experience
- Specify `defaultTrackingMode` as `"SpeakerTrack"` for most meeting rooms
- Use `"PresenterTrack"` if the room emphasizes presentation content
- Camera tracking only functions if the codec firmware supports the tracking feature
- Test camera controls and auto-tracking functionality in real meetings

**Content Sharing:**
- Set `defaultShareLocalOnly: true` to prevent accidental wide-area sharing
- Use `autoShareContentWhileInCall: false` to give users explicit control over sharing initiation
- Test content sharing with multiple participants to verify far-end visibility
- Configure layout preferences to optimize shared content display

**External Source Switching:**
- Only enable `externalSourceListEnabled: true` if external input switching is installed
- Verify the `externalSourceInputPort` name matches the codec's actual input port designation (e.g., "HDMI1", "HDMI2")
- Test external source switching before system deployment
- Document input port assignments for future troubleshooting

**Bridge Configuration:**
- Use EISC Advanced bridges for optimal codec control via SIMPL+
- Separate communication monitor into dedicated EISC bridge with distinct join start
- Document bridge IP IDs, join starts, and device keys for system documentation
- Test all join mappings before system handoff

### Device Configuration Examples

**Basic Codec Device:**

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
    "cameraInfo": [
      {
        "CameraNumber": 1,
        "Name": "Audience"
      },
      {
        "CameraNumber": 2,
        "Name": "Presenter"
      }
    ]
  }
}
```

### Navigator Configuration

Place custom icon .png files in the `/user/programX/navigatorIcons/` folder. The system automatically generates `/user/programX/navigatorIcons/icons-base64.txt` containing Base64-encoded icon content for use in `customIconContent` fields.

**Available Default Icons:** Briefing, Camera, Concierge, Disc, Handset, Help, Helpdesk, Home, Hvac, Info, Input, Language, Laptop, Lightbulb, Media, Microphone, Power, Proximity, Record, Spark, Tv, Webex, General, Sliders

**Navigator Configuration Example:**

```json
{
  "key": "navigator",
  "name": "Rm Navigator",
  "type": "ciscoRoomOsMobileControl",
  "group": "videoCodecTouchpanel",
  "properties": {
    "defaultRoomKey": "room",
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
          "macAddress": "00:01:02:03:04:05",
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

### Bridge Configuration Example

**EISC Advanced Bridge for Codec Control:**

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

<!-- END Configuration Best Practices -->

---

## Console Commands

### `devjson {"deviceKey":"Codec-1-ssh","methodName":"SendText", "params": ["xStatus SystemUnit\n"]}`
Invoke a method on the device. Example sends the `xStatus SystemUnit` command directly to the codec for status query.

### `devmethods {deviceKey}`
Display available methods for device interaction with the `devjson` command. Returns all public methods available on the specified device.

### `setdevicestreamdebug {deviceKey} {Off|Both|Tx|Rx}`
Enable communication-level debugging for SSH/TCP transmission:
- `Off` - Disable logging
- `Tx` - Log outbound commands to device
- `Rx` - Log inbound responses from device
- `Both` - Log both directions

Use device key format like `Codec-1-ssh` for the actual SSH connection device.

### `setcodeccommdebug {1 | 0}`
Enable device-level communications debugging:
- `1` - Enable debugging (use `appdebug 1` to see messages)
- `0` - Disable debugging

---

## License

Provided under MIT license
