# Cisco RoomOS Video Codec Plugin - SIMPL Example

This SIMPL example is part of the [Cisco RoomOS Video Codec Plugin](https://github.com/PepperDash/epi-videoCodec-ciscoExtended)

## Dependencies
The *_compiled.zip is included so that all dependencies are available for re-compiling the SIMPL program.
Classfiles for any simplsharp modules are included as *.cs files only, no project folders.

Import the *_compiled.zip in SIMPL windows then compile.

## Loading the demo
It is expected that you already know how to get PepperDash Essentials running on a Crestron processor.

1. Load [Essentials](https://github.com/PepperDash/Essentials).
2. Load the [Cisco RoomOS Video Codec Plugin](https://github.com/PepperDash/epi-videoCodec-ciscoExtended) plugin, into the Essentials plugin folder.
3. Load the example PepperDash Essentials config file (configurationFile_epi_cisco_demo.json), which can be found in the config directory of this repo.
4. Load epi-videoCodec-ciscoExtended-SIMPL-example-v1.0.lpz into a spare program slot.
5. Load epi_cisco_roomcontrolconfig.xml onto a Cisco RoomOS codec using the built in UI Extensions editor.


## Testing
Tested on:
* Cisco Codec PlusL RoomOS 10.20.1.6 and a
* DMPS3-4K-350-C Cntrl Eng [v1.600.3792.19330]


In the Cisco UI Extensions editor preview the current configuration, select the blinds panel and then select blinds_up or blinds_down.
* A relay should click on the processor demonstrating that the codec UI event was received by Essentials and transmitted through the bridge to the SIMPL demo.
* The current position of the blinds should be displayed a s a percent value between the blinds_up and blinds_down button indicating that the level has been transmitted from the SIMPL program through the bridge to Essentials and then sent to the codec.

## Troubleshooting
1. Make sure that the codec IP address and credentials are entered correctly in the config file, and that Essentials is communicating with the codec. The following command in a Crestron termianl should get a response as shown below and the status will be viewable on the codec web interface call page.

```
CP3>devjson {"deviceKey":"Codec-1","methodName":"MuteToggle", "params": []}
Method MuteToggle successfully called on device Codec-1
CP3>
```
2. Make sure the bridge is connected.
Check the IP table entry is connected.
```
IP Table for program 1
CIP_ID  Type    Status     DevID  Port   IP Address/SiteName      RoomId
    4F  CIP     ONLINE            41794  127.000.000.002          Not Specified
```
Make sure the config file is loaded and the bridge eisc is defined with the correct ip address, ipid, and devicekey.
In Toolbox debugger trigger any command (e.g., MicriphoneMuteToggle) and check that it is controlling the codec.

3. In the codec web interface select UI Extensions editor and make sure there is a custom UI loaded, if not open the editor menu (round burger menu at the top right) and import a configuration from file, then select epi_cisco_roomcontrolconfig.xml from the config folder in this repo.

4. In the UI Extensions editor preview the configurtion then press the blinds buttons, if the text between the buttons changes then it is working.
