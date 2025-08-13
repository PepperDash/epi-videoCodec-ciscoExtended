# Navigator Lite Codec - Usage Examples

## Configuration for Navigator-Only Scenarios

### Lite Codec Configuration
Use this configuration when you only need Navigator functionality without full codec features:

```json
{
  "devices": [
    {
      "key": "lite-codec-room1",
      "name": "Room 1 Navigator Lite Codec", 
      "type": "ciscoRoomOSNavigatorLite",
      "group": "plugin",
      "properties": {
        "control": {
          "method": "ssh",
          "tcpSshProperties": {
            "address": "10.1.1.100",
            "port": 22,
            "username": "admin",
            "password": "your-password"
          }
        }
      }
    },
    {
      "key": "navigator-room1",
      "name": "Room 1 Navigator Controller",
      "type": "ciscoRoomOsMobileControl", 
      "group": "plugin",
      "properties": {
        "videoCodecKey": "lite-codec-room1",
        "defaultRoomKey": "room1",
        "useDirectServer": false
      }
    }
  ]
}
```

### Full Codec Configuration (for comparison)
Use this when you need full codec functionality:

```json
{
  "devices": [
    {
      "key": "full-codec-room2",
      "name": "Room 2 Full Codec",
      "type": "ciscoRoomOS",
      "group": "plugin", 
      "properties": {
        "control": {
          "method": "ssh",
          "tcpSshProperties": {
            "address": "10.1.1.101",
            "port": 22,
            "username": "admin",
            "password": "your-password"
          }
        },
        "getPhonebookOnStartup": true,
        "getBookingsOnStartup": true
      }
    }
  ]
}
```

## Key Differences

### Navigator Lite Implementation:
- **Reduced memory footprint** - Only loads essential functionality
- **Faster startup** - No phonebook/booking/status polling initialization
- **Simplified communication** - Direct command sending without complex queuing
- **Navigator-focused** - Optimized for WebView and mobile control scenarios

### Features Available:
✅ Navigator WebView control  
✅ Mobile Control integration  
✅ UI Extensions support  
✅ Basic connectivity monitoring  
✅ Command queuing for UI extensions  

### Features NOT Available:
❌ Call management  
❌ Directory/phonebook integration  
❌ Camera control (SpeakerTrack, PresenterTrack)  
❌ Room presets  
❌ Booking/scheduling integration  
❌ Advanced codec status monitoring  

## Usage Scenarios

### When to use Navigator Lite:
- Room combination scenarios with Navigator-only panels
- Touch panels that only need WebView display capabilities
- Simplified installations without video conferencing features
- Resource-constrained environments
- Navigator lockout scenarios

### When to use Full Codec:
- Full video conferencing installations
- Rooms requiring call management
- Scenarios needing camera control
- Directory/phonebook integration required
- Advanced codec monitoring needed

## Migration Path

Existing installations can gradually migrate to lite implementation:

1. **Assessment Phase**: Identify rooms using Navigator-only functionality
2. **Configuration Update**: Change device type from "ciscoRoomOS" to "ciscoRoomOSNavigatorLite" 
3. **Testing Phase**: Verify Navigator functionality works as expected
4. **Rollout**: Deploy lite implementation to appropriate rooms

The interface-based design ensures compatibility - existing Navigator controllers work with both implementations without code changes.