using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Devices.Common.VideoCodec;
using PepperDash.Essentials.Core.Presets;

namespace epi_videoCodec_ciscoExtended
{
    // Helper Classes for Proerties
    public abstract class ValueProperty
    {
        /// <summary>
        /// Triggered when Value is set
        /// </summary>
        public Action ValueChangedAction { get; set; }

        protected void OnValueChanged()
        {
            var a = ValueChangedAction;
            if (a != null)
                a();
        }

    }

    /// <summary>
    /// This class exists to capture serialized data sent back by a Cisco codec in JSON output mode
    /// </summary>
    public class CiscoCodecStatus 
    {
        public class ConnectionStatus
        {
            public string Value { get; set; }
        }

        public class EcReferenceDelay
        {
            public string Value { get; set; }
        }

        public class Microphone
        {
            [JsonProperty("MicrophoneId")]
            public string MicrophoneId { get; set; }
            public ConnectionStatus ConnectionStatus { get; set; }
            public EcReferenceDelay EcReferenceDelay { get; set; }
        }

        public class Connectors
        {
            [JsonProperty("microphone")]
            public List<Microphone> MicrophoneList { get; set; }
        }

        public class Input
        {
            public Connectors Connectors { get; set; }
        }

        public class Mute : ValueProperty
        {
            public bool BoolValue { get; private set; }

            public string Value
            {
                set
                {
                    // If the incoming value is "On" it sets the BoolValue true, otherwise sets it false
                    BoolValue = value == "On";
                    OnValueChanged();
                }
            }
        }

        public class Microphones
        {
            public Mute Mute { get; set; }

            public Microphones()
            {
                Mute = new Mute();
            }
        }


        public class DelayMs
        {
            public string Value { get; set; }
        }

        public class Line
        {
            [JsonProperty("LineId")]
            public string LineId { get; set; }
            public ConnectionStatus ConnectionStatus { get; set; }
            public DelayMs DelayMs { get; set; }
        }

        public class OutputConnectors
        {
            [JsonProperty("line")]
            public List<Line> Lines { get; set; }
        }

        public class Output
        {
            [JsonProperty("connectors")]
            public OutputConnectors OutputConnectors { get; set; }
        }

        public class Volume : ValueProperty
        {
            string _value;

            /// <summary>
            /// Sets Value and triggers the action when set
            /// </summary>
            public string Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                    OnValueChanged();
                }
            }

            /// <summary>
            /// Converted value of _Value for use as feedback
            /// </summary>
            public int IntValue
            {
                get {
                    return !string.IsNullOrEmpty(_value) ? Convert.ToInt32(_value) : 0;
                }
            }
        }

        public class VolumeMute : ValueProperty
        {
            public bool BoolValue { get; private set; }

            public string Value
            {
                set
                {
                    // If the incoming value is "On" it sets the BoolValue true, otherwise sets it false
                    BoolValue = value == "On";
                    OnValueChanged();
                }
            }
        }

        public class Audio
        {
            public Input Input { get; set; }
            public Microphones Microphones { get; set; } // Can we have this setter fire the update on the CiscoCodec feedback?
            public Output Output { get; set; }
            public Volume Volume { get; set; }
            public VolumeMute VolumeMute { get; set; }

            public Audio()
            {
                Volume = new Volume();
                VolumeMute = new VolumeMute();
                Microphones = new Microphones();
            }
        }

        public class Id
        {
            public string Value { get; set; }
        }

        public class Current
        {
            public Id Id { get; set; }
        }

        public class Bookings
        {
            public Current Current { get; set; }
        }

        public class Options
        {
            public string Value { get; set; }
        }

        public class Capabilities
        {
            public Options Options { get; set; }
        }

        public class Connected
        {
            public string Value { get; set; }
        }

        public class Framerate
        {
            public string Value { get; set; }
        }

        public class Flip
        {
            public string Value { get; set; }
        }

        public class HardwareId
        {
            public string Value { get; set; }
        }

        public class Manufacturer
        {
            public string Value { get; set; }
        }

        public class Model
        {
            public string Value { get; set; }
        }

        public class Pan
        {
            public string Value { get; set; }
        }

        public class Tilt
        {
            public string Value { get; set; }
        }

        public class Zoom
        {
            public string Value { get; set; }
        }

        public class Position
        {
            public Pan Pan { get; set; }
            public Tilt Tilt { get; set; }
            public Zoom Zoom { get; set; }
        }

        public class SoftwareId
        {
            public string Value { get; set; }
        }

        public class DectectedConnector
        {
            public string Value { get; set; }

            public int DetectedConnectorId
            {
                get
                {
                    if (!string.IsNullOrEmpty(Value))
                    {
                        return Convert.ToUInt16(Value);
                    }
                    return -1;
                }
            }
        }

        public class Camera
        {
            [JsonProperty("CameraId")]
            public string CameraId { get; set; }
            public Capabilities Capabilities { get; set; }
            public Connected Connected { get; set; }
            public DectectedConnector DetectedConnector { get; set; }
            public Flip Flip { get; set; }
            public HardwareId HardwareId { get; set; }
            public MacAddress MacAddress { get; set; }
            public Manufacturer Manufacturer { get; set; }
            public Model Model { get; set; }
            public Position Position { get; set; }
            public SerialNumber SerialNumber { get; set; }
            public SoftwareId SoftwareId { get; set; }

            public Camera()
            {
                Manufacturer = new Manufacturer();
                Model = new Model();
                DetectedConnector = new DectectedConnector();
            }
        }

        public class Availability : ValueProperty
        {
            string _value;
            public bool BoolValue { get; private set; }

            public string Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    // If the incoming value is "Available" it sets the BoolValue true, otherwise sets it false
                    _value = value;

                    BoolValue = value.ToLower() == "available";
                    OnValueChanged();
                }
            }
        }

        public class CallStatus : ValueProperty
        {
            string _value;
            public bool BoolValue { get; private set; }


            public string Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    // If the incoming value is "Active" it sets the BoolValue true, otherwise sets it false
                    _value = value;
                    BoolValue = value == "Connected";
                    OnValueChanged();
                }
            }
        }

        public class SpeakerTrackStatus : ValueProperty
        {
            string _value;
            public bool BoolValue { get; private set; }
            public string StringValue { get; private set; }

            public List<string> SpeakerTrackStatusValues { get; private set; }  


            public string Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    // If the incoming value is "Active" it sets the BoolValue true, otherwise sets it false
                    _value = value;
                    BoolValue = value.ToLower() == "active";
                    StringValue = value;
                    OnValueChanged();
                }
            }

            public SpeakerTrackStatus()
            {
                SpeakerTrackStatusValues = new List<string> { "active", "inactive" };
            }
        }

        public class SpeakerTrack
        {
            public Availability Availability { get; set; }
            [JsonProperty("status")]
            public SpeakerTrackStatus SpeakerTrackStatus { get; set; }

            public SpeakerTrack()
            {
                SpeakerTrackStatus = new SpeakerTrackStatus();
                Availability = new Availability();
            }
        }

        public class PresenterTrack
        {
            public Availability Availability { get; set; }
            [JsonProperty("status")]
            public PresenterTrackStatus PresenterTrackStatus { get; set; }

            public PresenterTrack()
            {
                PresenterTrackStatus = new PresenterTrackStatus();
                Availability = new Availability();
            }

        }

        public class PresenterTrackStatus : ValueProperty
        {
            string _value;
            public string StringValue { get; private set; }
            public bool BoolValue { get; private set; }
            public List<string> PresenterTrackStatusValues { get; private set; }  

            public string Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    // If the incoming value is "Active" it sets the BoolValue true, otherwise sets it false
                    _value = value;
                    StringValue = value;
                    BoolValue = value.ToLower() == "off";
                    OnValueChanged();
                }
            }

            public PresenterTrackStatus()
            {
                PresenterTrackStatusValues = new List<string> {"off", "follow", "diagnostic", "background", "setup", "persistent"};
            }
        }

        public class Cameras
        {
            [JsonProperty("camera")]
            public List<Camera> CameraList { get; set; }
            public SpeakerTrack SpeakerTrack { get; set; }
            public PresenterTrack PresenterTrack { get; set; }

            public Cameras()
            {
                CameraList = new List<Camera>();
                SpeakerTrack = new SpeakerTrack();
                PresenterTrack = new PresenterTrack();
                //CameraCapability = new CameraTrackingCapability(SpeakerTrack, PresenterTrack);
            }
        }

        public class CameraTrackingCapability : ValueProperty
        {
            public eCameraTrackingCapabilities CameraTrackingCapabilities { get; private set; }
            private readonly SpeakerTrack _speakerTrack;
            private readonly PresenterTrack _presenterTrack;


            public CameraTrackingCapability(SpeakerTrack speakerTrack, PresenterTrack presenterTrack, eCameraTrackingCapabilities cameraTrackingCapabilities)
            {
                _speakerTrack = speakerTrack;
                _presenterTrack = presenterTrack;
                CameraTrackingCapabilities = cameraTrackingCapabilities;
            }

            protected Func<eCameraTrackingCapabilities> CameraTrackingFeedbackFunc
            {
                get
                {
                    return () =>
                    {
                        var trackingType = eCameraTrackingCapabilities.None;

                        if (_speakerTrack.Availability.BoolValue && _presenterTrack.Availability.BoolValue)
                        {
                            trackingType = eCameraTrackingCapabilities.Both;
                            return trackingType;
                        }
                        if (!_speakerTrack.Availability.BoolValue && _presenterTrack.Availability.BoolValue)
                        {
                            trackingType = eCameraTrackingCapabilities.PresenterTrack;
                            return trackingType;
                        }
                        if (_speakerTrack.Availability.BoolValue && !_presenterTrack.Availability.BoolValue)
                        {
                            trackingType = eCameraTrackingCapabilities.SpeakerTrack;
                            return trackingType;
                        }
                        return trackingType;
                    };
                }
            }

        }

        //public class CameraConverter : JsonConverter
        //{

        //    public override bool CanConvert(System.Type objectType)
        //    {
        //        return true; // objectType == typeof(CameraList) || objectType == typeof(List<CameraList>); // This should not be called but is required for implmentation
        //    }

        //    public override object ReadJson(JsonReader reader, System.Type objectType, object existingValue, JsonSerializer serializer)
        //    {
        //        try
        //        {
        //            if (reader.TokenType == JsonToken.StartArray)
        //            {
        //                var l = new List<CameraList>();
        //                reader.Read();
        //                while (reader.TokenType != JsonToken.EndArray)
        //                {
        //                    l.Add(reader.Value as CameraList);
        //                    reader.Read();
        //                }
        //                Debug.Console(1, "[xStatus]: Cameras converted as list");
        //                return l;
        //            }
        //            else
        //            {
        //                Debug.Console(1, "[xStatus]: CameraList converted as single object and added to list");
        //                return new List<CameraList> { reader.Value as CameraList };
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            Debug.Console(1, "[xStatus]: Unable to convert JSON for camera objects: {0}", e);

        //            return new List<CameraList>();
        //        }
        //    }

        //    public override bool CanWrite
        //    {
        //        get
        //        {
        //            return false;
        //        }
        //    }

        //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        //    {
        //        throw new NotImplementedException("Write not implemented");
        //    }
        //}


        public class MaxActiveCalls
        {
            public string Value { get; set; }
        }

        public class MaxAudioCalls
        {
            public string Value { get; set; }
        }

        public class MaxCalls
        {
            public string Value { get; set; }
        }

        public class MaxVideoCalls
        {
            public string Value { get; set; }
        }

        public class Conference
        {
            public MaxActiveCalls MaxActiveCalls { get; set; }
            public MaxAudioCalls MaxAudioCalls { get; set; }
            public MaxCalls MaxCalls { get; set; }
            public MaxVideoCalls MaxVideoCalls { get; set; }
        }

        public class StatusCapabilities
        {
            public Conference Conference { get; set; }
        }

        public class CallId
        {
            public string Value { get; set; }
        }

        public class ActiveSpeaker
        {
            public CallId CallId { get; set; }
        }

        public class DoNotDisturb : ValueProperty
        {
            string _value;

            public bool BoolValue { get; private set; }

            public string Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                    // If the incoming value is "On" it sets the BoolValue true, otherwise sets it false
                    BoolValue = value == "On" || value == "Active";
                    OnValueChanged();
                }
            }
        }

        public class Mode
        {
            public string Value { get; set; }
        }

        public class Multipoint
        {
            public Mode Mode { get; set; }
        }


        public class ModeValueProperty : ValueProperty
        {
            string _value;

            public bool BoolValue { get; private set; }

            public string Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                    // If the incoming value is "Sending" it sets the BoolValue true, otherwise sets it false
                    BoolValue = value == "Sending";
                    OnValueChanged();
                }
            }
        }


        public class ReleaseFloorAvailability
        {
            public string Value { get; set; }
        }

        public class RequestFloorAvailability
        {
            public string Value { get; set; }
        }

        public class Whiteboard
        {
            public Mode Mode { get; set; }
            public ReleaseFloorAvailability ReleaseFloorAvailability { get; set; }
            public RequestFloorAvailability RequestFloorAvailability { get; set; }
        }


        public class SourceValueProperty : ValueProperty
        {
            string _value;

            /// <summary>
            /// Sets Value and triggers the action when set
            /// </summary>
            public string Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                    OnValueChanged();
                }
            }

            /// <summary>
            /// Converted value of _Value for use as feedback
            /// </summary>
            public int IntValue
            {
                get {
                    return !string.IsNullOrEmpty(_value) ? Convert.ToInt32(_value) : 0;
                }
            }
        }

        public class SendingMode : ValueProperty
        {
            string _value;

            /// <summary>
            /// Sets Value and triggers the action when set
            /// </summary>
            public string Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                    OnValueChanged();
                }
            }

            public bool LocalOnly
            {
                get
                {
                    if (string.IsNullOrEmpty(_value))
                        return false;


                    return _value.ToLower() == "localonly";
                }
            }

            public bool LocalRemote
            {
                get
                {
                    if (string.IsNullOrEmpty(_value))
                        return false;

                    return _value.ToLower() == "localremote";
                }
            }

            public bool Off
            {
                get
                {
                    if (string.IsNullOrEmpty(_value))
                        return false;

                    return _value.ToLower() == "off";
                }
            }
        }

        public class LocalInstance
        {
            [JsonProperty("MessageId")]
            public string LocalInstanceId { get; set; }

            public string Ghost { get; set; }
            public SendingMode SendingMode { get; set; }
            [JsonProperty("source")]
            public SourceValueProperty SourceValueProperty { get; set; }

            public LocalInstance()
            {
                SourceValueProperty = new SourceValueProperty();
                SendingMode = new SendingMode();
            }
        }

        public class Presentation
        {
            public CallId CallId { get; set; }
            [JsonProperty("mode")]
            public ModeValueProperty ModeValueProperty { get; set; }
            public Whiteboard Whiteboard { get; set; }
            [JsonProperty("localInstance")]
            public List<LocalInstance> PresentationLocalInstances { get; set; }

            public Presentation()
            {
                ModeValueProperty = new ModeValueProperty();
                PresentationLocalInstances = new List<LocalInstance>();
            }
        }



        public class SpeakerLock
        {
            public CallId CallId { get; set; }
            public Mode Mode { get; set; }
        }

        public class StatusConference
        {
            public ActiveSpeaker ActiveSpeaker { get; set; }
            public DoNotDisturb DoNotDisturb { get; set; }
            public Multipoint Multipoint { get; set; }
            public Presentation Presentation { get; set; }
            public SpeakerLock SpeakerLock { get; set; }

            public StatusConference()
            {
                Presentation = new Presentation();
                DoNotDisturb = new DoNotDisturb();
            }
        }


        public class Level
        {
            public string Value { get; set; }
        }

        public class References
        {
            public string Value { get; set; }
        }

        public class Message
        {

            public string MessageId { get; set; }
            public Description Description { get; set; }
            public Level Level { get; set; }
            public References References { get; set; }
            public Type Type { get; set; }
        }

        public class Diagnostics
        {
            [JsonProperty("DiagnosticsMessage")]
            public List<Message> DiagnosticsMessage { get; set; }
        }

        public class ExperimentalConference
        {
        }

        public class Experimental
        {
            [JsonProperty("ExperimentalConference")]
            public ExperimentalConference ExperimentalConference { get; set; }
        }

        public class Address
        {
            public string Value { get; set; }
        }

        public class Port
        {
            public string Value { get; set; }
        }



        public class Gatekeeper
        {
            public Address Address { get; set; }
            public Port Port { get; set; }
            public Reason Reason { get; set; }
            public Status Status { get; set; }
        }

        public class Reason
        {
            public string Value { get; set; }
        }



        public class H323Mode
        {
            public Reason Reason { get; set; }
            public Status Status { get; set; }
        }


        public class H323
        {
            public Gatekeeper Gatekeeper { get; set; }
            [JsonProperty("mode")]
            public H323Mode H323Mode { get; set; }
        }

        public class Expression
        {
            [JsonProperty("ExpressionId")]
            public string ExpressionId { get; set; }
            public string Value { get; set; }
        }

        public class Format
        {
            public string Value { get; set; }
        }

        public class Url
        {
            public string Value { get; set; }
        }

        public class HttpFeedback
        {
            [JsonProperty("HttpFeedbackId")]
            public string HttpFeedbackId { get; set; }
            [JsonProperty("expression")]
            public List<Expression> HttpFedbackExpressions { get; set; }
            public Format Format { get; set; }
            public Url Url { get; set; }
        }

        public class MediaChannels
        {
            [JsonProperty("call")]
            public List<MediaChannelCall> MediaChannelCalls { get; set; }
        }

        public class MediaChannelCall
        {
            [JsonProperty("channel")]
            public List<Channel> Channels { get; set; }
            [JsonProperty("ExpressionId")]
            public string MediaChannelCallId { get; set; }
        }

        public class Channel
        {
            [JsonProperty("video")]
            public ChannelVideo ChannelVideo;
            public Type Type { get; set; }
        }

        public class ChannelVideo
        {
            public Protocol Protocol;

        }


        public class CapabilitiesString
        {
            public string Value { get; set; }
        }

        public class DeviceId
        {
            public string Value { get; set; }
        }

        public class Duplex
        {
            public string Value { get; set; }
        }

        public class Platform
        {
            public string Value { get; set; }
        }

        public class PortId
        {
            public string Value { get; set; }
        }

        public class PrimaryMgmtAddress
        {
            public string Value { get; set; }
        }

        public class SysName
        {
            public string Value { get; set; }
        }

        public class SysObjectId
        {
            public string Value { get; set; }
        }

        public class VtpMgmtDomain
        {
            public string Value { get; set; }
        }

        public class Version : ValueProperty
        {
            private string _value;

            public string Value
            {
                get { return _value; }
                set
                {
                    _value = value;
                    OnValueChanged();
                }
            }
        }

        public class VoIpApplianceVlanId
        {
            public string Value { get; set; }
        }

        public class Cdp
        {
            public Address Address { get; set; }
            [JsonProperty("capabilities")]
            public CapabilitiesString CapabilitiesString { get; set; }
            public DeviceId DeviceId { get; set; }
            public Duplex Duplex { get; set; }
            public Platform Platform { get; set; }
            public PortId PortId { get; set; }
            public PrimaryMgmtAddress PrimaryMgmtAddress { get; set; }
            public SysName SysName { get; set; }
            public SysObjectId SysObjectId { get; set; }
            public VtpMgmtDomain VtpMgmtDomain { get; set; }
            public Version Version { get; set; }
            public VoIpApplianceVlanId VoIpApplianceVlanId { get; set; }
        }

        public class Name
        {
            public string Value { get; set; }
        }

        public class Domain
        {
            public Name Name { get; set; }
        }


        public class Server
        {
            [JsonProperty("NetworkId")]
            public string ServerId { get; set; }
            public Address Address { get; set; }
        }

        public class Dns
        {
            public Domain Domain { get; set; }
            [JsonProperty("servers")]
            public List<Server> Servers { get; set; }
        }

        public class MacAddress
        {
            public string Value { get; set; }
        }

        public class Speed
        {
            public string Value { get; set; }
        }

        public class Ethernet
        {
            public MacAddress MacAddress { get; set; }
            public Speed Speed { get; set; }
        }


        public class Gateway
        {
            public string Value { get; set; }
        }

        public class SubnetMask
        {
            public string Value { get; set; }
        }

// ReSharper disable once InconsistentNaming
        public class IPv4
        {
            public Address Address { get; set; }
            public Gateway Gateway { get; set; }
            public SubnetMask SubnetMask { get; set; }

            public IPv4()
            {
                Address = new Address();
            }
        }


// ReSharper disable once InconsistentNaming
        public class IPv6
        {
            public Address Address { get; set; }
            [JsonProperty("gateway")]
            public Gateway GatewayIPv6 { get; set; }
        }

        public class VlanId
        {
            public string Value { get; set; }
        }

        public class Voice
        {
            public VlanId VlanId { get; set; }
        }

        public class Vlan
        {
            public Voice Voice { get; set; }
        }

        public class Network
        {
            [JsonProperty("NetworkId")]
            public string NetworkId { get; set; }
            public Cdp Cdp { get; set; }
            public Dns Dns { get; set; }
            public Ethernet Ethernet { get; set; }
// ReSharper disable once InconsistentNaming
            public IPv4 IPv4 { get; set; }
// ReSharper disable once InconsistentNaming
            public IPv6 IPv6 { get; set; }
            public Vlan Vlan { get; set; }

            public Network()
            {
                IPv4 = new IPv4();
            }
        }

        public class CurrentAddress
        {
            public string Value { get; set; }
        }




        public class Ntp
        {
            public CurrentAddress CurrentAddress { get; set; }
            [JsonProperty("server")]
            public List<Server> Servers { get; set; }
            public Status Status { get; set; }
        }

        public class NetworkServices
        {
            public Ntp Ntp { get; set; }
        }

        public class HardwareInfo
        {
            public string Value { get; set; }
        }



        public class SoftwareInfo
        {
            public string Value { get; set; }
        }



        public class UpgradeStatus
        {
            public string Value { get; set; }
        }

        public class ConnectedDevice
        {
            [JsonProperty("id")]
            public string ConnectedDeviceIdString { get; set; }
            public HardwareInfo HardwareInfo { get; set; }
            [JsonProperty("id")]
            public Id ConnectedDeviceId { get; set; }
            public Name Name { get; set; }
            public SoftwareInfo SoftwareInfo { get; set; }
            public Status Status { get; set; }
            public Type Type { get; set; }
            public UpgradeStatus UpgradeStatus { get; set; }
        }

        public class StatusString
        {
            public string Value { get; set; }
        }

        public class Peripherals
        {
            [JsonProperty("connectedDevice")]
            public List<ConnectedDevice> ConnectedDevices { get; set; }
        }

        public class Enabled
        {
            public string Value { get; set; }
        }

        public class LastLoggedInUserId
        {
            public string Value { get; set; }
        }

        public class LoggedIn
        {
            public string Value { get; set; }
        }

        public class ExtensionMobility
        {
            public Enabled Enabled { get; set; }
            public LastLoggedInUserId LastLoggedInUserId { get; set; }
            public LoggedIn LoggedIn { get; set; }
        }

        public class Cucm
        {
            public ExtensionMobility ExtensionMobility { get; set; }
        }

        public class CompletedAt
        {
            public string Value { get; set; }
        }


        public class VersionId
        {
            public string Value { get; set; }
        }

        public class SoftwareCurrent
        {
            public CompletedAt CompletedAt { get; set; }
            public Url Url { get; set; }
            public VersionId VersionId { get; set; }
        }

        public class LastChange
        {
            public string Value { get; set; }
        }

        public class UpgradeStatusMessage
        {
            public string Value { get; set; }
        }

        public class Phase
        {
            public string Value { get; set; }
        }

        public class SessionId
        {
            public string Value { get; set; }
        }




        public class UpgradeStatusExpanded
        {
            public LastChange LastChange { get; set; }
            [JsonProperty("message")]
            public UpgradeStatusMessage UpgradeStatusMessage { get; set; }
            public Phase Phase { get; set; }
            public SessionId SessionId { get; set; }
            public Status Status { get; set; }
            public Url Url { get; set; }
            [JsonProperty("versionId")]
            public VersionId UgpradeStatusVersionId { get; set; }
        }

        public class Software
        {
            [JsonProperty("current")]
            public SoftwareCurrent SoftwareCurrent { get; set; }
            [JsonProperty("upgradeStatus")]
            public UpgradeStatusExpanded SoftwareUpgradeStatus { get; set; }
        }


        public class Provisioning
        {
            public Cucm Cucm { get; set; }
            public Software Software { get; set; }
            public Status Status { get; set; }
        }

        public class Availability2
        {
            public string Value { get; set; }
        }

        public class Services
        {
            public Availability Availability { get; set; }

            public Services()
            {
                Availability = new Availability();
            }
        }

        public class Proximity
        {
            public Services Services { get; set; }
        }

        public class CurrentPeopleCount : ValueProperty
        {
            string _value;

            /// <summary>
            /// Sets Value and triggers the action when set
            /// </summary>
            public string Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                    OnValueChanged();
                }
            }

            /// <summary>
            /// Converted value of _Value for use as feedback
            /// </summary>
            public int IntValue
            {
                get {
                    return !string.IsNullOrEmpty(_value) ? Convert.ToInt32(_value) : 0;
                }
            }
        }

        public class PeopleCount
        {
            [JsonProperty("current")]
            public CurrentPeopleCount CurrentPeopleCount { get; set; }

            public PeopleCount()
            {
                CurrentPeopleCount = new CurrentPeopleCount();
            }
        }

        public class PeoplePresence : ValueProperty
        {
            public bool BoolValue { get; private set; }

            public string Value
            {
                set
                {
                    // If the incoming value is "Yes" it sets the BoolValue true, otherwise sets it false
                    BoolValue = value == "Yes";
                    OnValueChanged();
                }
            }
        }

        public class RoomAnalytics
        {
            public PeopleCount PeopleCount { get; set; }
            public PeoplePresence PeoplePresence { get; set; }

            public RoomAnalytics()
            {
                PeopleCount = new PeopleCount();
                PeoplePresence = new PeoplePresence();
            }
        }

        public class Primary
        {
            public CiscoUri Uri { get; set; }

            public Primary()
            {
                Uri = new CiscoUri();
            }
        }

        public class AlternateUri
        {
            public Primary Primary { get; set; }

            public AlternateUri()
            {
                Primary = new Primary();
            }
        }

        public class Authentication
        {
            public string Value { get; set; }
        }

        public class DisplayName : ValueProperty
        {
            private string _value;

            public string Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                    OnValueChanged();
                }
            }
        }


        public class CiscoUri
        {
            public string Value { get; set; }
        }

        public class CallForward
        {
            public DisplayName DisplayName { get; set; }
            public Mode Mode { get; set; }
            public CiscoUri Uri { get; set; }
        }

        public class MessagesWaiting
        {
            public string Value { get; set; }
        }


        public class Mailbox
        {
            public MessagesWaiting MessagesWaiting { get; set; }
            public CiscoUri Uri { get; set; }
        }



        public class Proxy
        {
            [JsonProperty("id")]
            public string ProxyId { get; set; }
            public Address Address { get; set; }
            public Status Status { get; set; }
        }




        public class Registration
        {
            [JsonProperty("id")]
            public string StringId { get; set; }
            public Reason Reason { get; set; }
            public Status Status { get; set; }
            public CiscoUri Uri { get; set; }

            public Registration()
            {
                Uri = new CiscoUri();
            }
        }

        public class Secure
        {
            public string Value { get; set; }
        }

        public class Verified
        {
            public string Value { get; set; }
        }

        public class Sip
        {
            public AlternateUri AlternateUri { get; set; }
            public Authentication Authentication { get; set; }
            public CallForward CallForward { get; set; }
            public Mailbox Mailbox { get; set; }
            [JsonProperty("proxy")]
            public List<Proxy> Proxies { get; set; }
            [JsonProperty("registration")]
            public List<Registration> Registrations { get; set; }
            public Secure Secure { get; set; }
            public Verified Verified { get; set; }

            public Sip()
            {
                AlternateUri = new AlternateUri();
                Registrations = new List<Registration>();
            }
        }


        public class Fips
        {
            public Mode Mode { get; set; }
        }

        public class CallHistory
        {
            public string Value { get; set; }
        }

        public class Configurations
        {
            public string Value { get; set; }
        }

        public class Dhcp
        {
            public string Value { get; set; }
        }

        public class InternalLogging
        {
            public string Value { get; set; }
        }

        public class LocalPhonebook
        {
            public string Value { get; set; }
        }

        public class Persistency
        {
            public CallHistory CallHistory { get; set; }
            public Configurations Configurations { get; set; }
            public Dhcp Dhcp { get; set; }
            public InternalLogging InternalLogging { get; set; }
            public LocalPhonebook LocalPhonebook { get; set; }
        }

        public class Security
        {
            public Fips Fips { get; set; }
            public Persistency Persistency { get; set; }
        }

        public class State : ValueProperty
        {
            string _value;

            public bool BoolValue { get; private set; }

            public string Value // Valid values are Standby/EnteringStandby/Halfwake/Off
            {
                get { return _value; }
                set
                {
                    _value = value;
                    // If the incoming value is "On" it sets the BoolValue true, otherwise sets it false
                    BoolValue = value == "On" || value == "Standby";
                    OnValueChanged();
                }
            }
        }

        public class Standby
        {
            public State State { get; set; }

            public Standby()
            {
                State = new State();
            }
        }

        public class CompatibilityLevel
        {
            public string Value { get; set; }
        }

        public class SerialNumber : ValueProperty
        {
            private string _value;

            public string Value
            {
                get { return _value; }
                set { _value = value; OnValueChanged(); }
            }
        }

        public class Module
        {
            public CompatibilityLevel CompatibilityLevel { get; set; }
            public SerialNumber SerialNumber { get; set; }

            public Module()
            {
                SerialNumber = new SerialNumber();
            }
        }

        public class Hardware
        {
            public Module Module { get; set; }

            public Hardware()
            {
                Module = new Module();
            }
        }

        public class ProductId
        {
            public string Value { get; set; }
        }

        public class ProductPlatform
        {
            public string Value { get; set; }
        }

        public class ProductType
        {
            public string Value { get; set; }
        }

        public class DisplayName2
        {
            public string Value { get; set; }
        }


        public class Encryption
        {
            public string Value { get; set; }
        }

        public class MultiSite : ValueProperty
        {
            private string _value;

            public bool BoolValue
            {
                get { return _value.ToLower() == "true"; }
            }

            public string Value { get { return _value; } set { _value = value; } }
        }

        public class RemoteMonitoring
        {
            public string Value { get; set; }
        }

        public class OptionKeys
        {
            public Encryption Encryption { get; set; }
            public MultiSite MultiSite { get; set; }
            public RemoteMonitoring RemoteMonitoring { get; set; }

            public OptionKeys()
            {
                MultiSite = new MultiSite();
            }
        }

        public class ReleaseDate
        {
            public string Value { get; set; }
        }


        public class SystemUnitSoftware
        {
            public DisplayName DisplayName { get; set; }
            public Name Name { get; set; }
            public OptionKeys OptionKeys { get; set; }
            public ReleaseDate ReleaseDate { get; set; }
            public Version Version { get; set; }

            public SystemUnitSoftware()
            {
                OptionKeys = new OptionKeys();
                Version = new Version();
                DisplayName = new DisplayName();
            }
        }

        public class NumberOfActiveCalls
        {
            public string Value { get; set; }
        }

        public class NumberOfInProgressCalls
        {
            public string Value { get; set; }
        }

        public class NumberOfSuspendedCalls
        {
            public string Value { get; set; }
        }

        public class State2
        {
            public NumberOfActiveCalls NumberOfActiveCalls { get; set; }
            public NumberOfInProgressCalls NumberOfInProgressCalls { get; set; }
            public NumberOfSuspendedCalls NumberOfSuspendedCalls { get; set; }
        }

        public class Uptime
        {
            public string Value { get; set; }
        }

        public class SystemUnit
        {
            public Hardware Hardware { get; set; }
            public ProductId ProductId { get; set; }
            public ProductPlatform ProductPlatform { get; set; }
            public ProductType ProductType { get; set; }
            [JsonProperty("software")]
            public SystemUnitSoftware SystemUnitSoftware { get; set; }
            [JsonProperty("state")]
            public State2 SystemUnitState { get; set; }
            public Uptime Uptime { get; set; }

            public SystemUnit()
            {
                SystemUnitSoftware = new SystemUnitSoftware();
                Hardware = new Hardware();
            }
        }

        public class SystemTime
        {
            public DateTime Value { get; set; }
        }

        public class Time
        {
            public SystemTime SystemTime { get; set; }
        }

        public class Number
        {
            public string Value { get; set; }
        }

        public class ContactMethod
        {
            [JsonProperty("id")]
            public string ContactMethodId { get; set; }
            public Number Number { get; set; }
        }

        public class ContactInfo
        {
            [JsonProperty("contactMethod")]
            public List<ContactMethod> ContactMethods { get; set; }
            public Name Name { get; set; }
        }

        public class UserInterface
        {
            public ContactInfo ContactInfo { get; set; }
        }


        public class ActiveSpeakerPip
        {
            public PipPosition PipPosition { get; set; }
        }


        public class SignalState
        {
            public string Value { get; set; }
        }

        public class SourceId
        {
            public string Value { get; set; }
        }


#region Type
        public class Type
        {
            public string Value { get; set; }
        }

#endregion


        public class Connector
        {
            [JsonProperty("id")]
            public string ConnectorIdString { get; set; }
            public Connected Connected { get; set; }
            public SignalState SignalState { get; set; }
            public SourceId SourceId { get; set; }
            public Type Type { get; set; }
        }

        public class MainVideoSource
        {
            public string Value { get; set; }
        }

        public class MainVideoMute : ValueProperty
        {
            public bool BoolValue { get; private set; }

            public string Value
            {
                set
                {
                    // If the incoming value is "On" it sets the BoolValue true, otherwise sets it false
                    BoolValue = value == "On";
                    OnValueChanged();
                }
            }

        }

        public class ConnectorId
        {
            public string Value { get; set; }
        }

        public class FormatStatus
        {
            public string Value { get; set; }
        }

        public class FormatType
        {
            public string Value { get; set; }
        }

        public class MediaChannelId
        {
            public string Value { get; set; }
        }

        public class Height
        {
            public string Value { get; set; }
        }

        public class RefreshRate
        {
            public string Value { get; set; }
        }

        public class Width
        {
            public string Value { get; set; }
        }

        public class Resolution
        {
            public Height Height { get; set; }
            public RefreshRate RefreshRate { get; set; }
            public Width Width { get; set; }
        }

        public class Source
        {
            [JsonProperty("id")]
            public string SourceIdString { get; set; }
            public ConnectorId ConnectorId { get; set; }
            public FormatStatus FormatStatus { get; set; }
            public FormatType FormatType { get; set; }
            public MediaChannelId MediaChannelId { get; set; }
            public Resolution Resolution { get; set; }
        }

        public class VideoInput
        {
            [JsonProperty("connector")]
            public List<Connector> InputConnectors { get; set; }
            public MainVideoSource MainVideoSource { get; set; }
            public MainVideoMute MainVideoMute { get; set; }
            [JsonProperty("source")]
            public List<Source> InputSources { get; set; }

            public VideoInput()
            {
                MainVideoMute = new MainVideoMute();
            }
        }

        public class Local : ValueProperty
        {
            string _value;

            public string Value // Valid values are On/Off
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                    OnValueChanged();
                }
            }
        }

        public class LayoutFamily
        {
            public Local Local { get; set; }

            public LayoutFamily()
            {
                Local = new Local();
            }
        }

        public class Layout
        {
            public LayoutFamily LayoutFamily { get; set; }

            public CurrentLayouts CurrentLayouts { get; set; }

            public Layout()
            {
                LayoutFamily = new LayoutFamily();
                CurrentLayouts = new CurrentLayouts();
            }
        }

        public class Monitors
        {
            public string Value { get; set; }
        }

        public class Connected3
        {
            public string Value { get; set; }
        }


        public class PreferredFormat
        {
            public string Value { get; set; }
        }

        public class VideoOutputConnectedDevice
        {
            public Name Name { get; set; }
            public PreferredFormat PreferredFormat { get; set; }
        }

        public class MonitorRole
        {
            public string Value { get; set; }
        }

        public class Height2
        {
            public string Value { get; set; }
        }

        public class RefreshRate2
        {
            public string Value { get; set; }
        }

        public class Width2
        {
            public string Value { get; set; }
        }

        public class Resolution2
        {
            public Height Height { get; set; }
            public RefreshRate RefreshRate { get; set; }
            public Width Width { get; set; }
        }


        public class VideoOutputConnector
        {
            [JsonProperty("id")]
            public string VideoOutputConnectorId { get; set; }
            public Connected Connected { get; set; }
            [JsonProperty("connectedDevice")]
            public VideoOutputConnectedDevice VideoOutputConnectedDevice { get; set; }
            public MonitorRole MonitorRole { get; set; }
            public Resolution Resolution { get; set; }
            public Type Type { get; set; }
        }

        public class VideoOutput
        {
            [JsonProperty("connector")]
            public List<VideoOutputConnector> VideoOutputConnectors { get; set; }
        }

        public class PresentationPip
        {
            public PipPosition PipPosition { get; set; }
        }

        public class FullscreenMode
        {
            public string Value { get; set; }
        }

        public class SelfViewMode : ValueProperty
        {
            public bool BoolValue { get; private set; }

            public string Value // Valid values are On/Off
            {
                set
                {
                    // If the incoming value is "On" it sets the BoolValue true, otherwise sets it false
                    BoolValue = value == "On";
                    OnValueChanged();
                }
            }
        }


        public class OnMonitorRole
        {
            public string Value { get; set; }
        }

        public class PipPosition : ValueProperty
        {
            string _value;

            public string Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                    OnValueChanged();
                }
            }
        }

        public class Selfview
        {
            public FullscreenMode FullscreenMode { get; set; }
            [JsonProperty("mode")]
            public SelfViewMode SelfViewMode { get; set; }
            public OnMonitorRole OnMonitorRole { get; set; }
            public PipPosition PipPosition { get; set; }

            public Selfview()
            {
                SelfViewMode = new SelfViewMode();
                PipPosition = new PipPosition();
            }
        }

        public class Video
        {
            [JsonProperty("activeSpeaker")]
            public ActiveSpeakerPip ActiveSpeakerPip { get; set; }
            [JsonProperty("input")]
            public VideoInput VideoInput { get; set; }
            public Layout Layout { get; set; }
            public Monitors Monitors { get; set; }
            [JsonProperty("output")]
            public VideoOutput VideoOutput { get; set; }
            [JsonProperty("presentation")]
            public PresentationPip PresentationPip { get; set; }
            public Selfview Selfview { get; set; }

            public Video()
            {
                Selfview = new Selfview();
                Layout = new Layout();
                VideoInput = new VideoInput();
            }
        }

        public class AnswerState
        {
            public string Value { get; set; }
        }

        public class CallType
        {
            public string Value { get; set; }
        }

        public class CallbackNumber
        {
            public string Value { get; set; }
        }

        public class DeviceType
        {
            public string Value { get; set; }
        }

        public class Direction
        {
            public string Value { get; set; }
        }

        public class Duration : ValueProperty
        {
            private string _value;

            public string Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                    OnValueChanged();
                }
            }

            public TimeSpan DurationValue
            {
                get
                {
                    return new TimeSpan(0, 0, Int32.Parse(_value));
                }
            }
        }

        public class FacilityServiceId
        {
            public string Value { get; set; }
        }

        public class HoldReason
        {
            public string Value { get; set; }
        }

        public class PlacedOnHold : ValueProperty
        {
            public bool BoolValue { get; private set; }

            public string Value
            {
                set
                {
                    // If the incoming value is "True" it sets the BoolValue true, otherwise sets it false
                    BoolValue = value == "True";
                    OnValueChanged();
                }
            }
        }

        public class Protocol
        {
            public string Value { get; set; }
        }

        public class ReceiveCallRate
        {
            public string Value { get; set; }
        }

        public class RemoteNumber
        {
            public string Value { get; set; }
        }

        public class TransmitCallRate
        {
            public string Value { get; set; }
        }

        public class Call
        {
            [JsonProperty("id")]
            public string CallIdString { get; set; }
            public AnswerState AnswerState { get; set; }
            public CallType CallType { get; set; }
            public CallbackNumber CallbackNumber { get; set; }
            public DeviceType DeviceType { get; set; }
            public Direction Direction { get; set; }
            public DisplayName DisplayName { get; set; }
            public Duration Duration { get; set; }
            public Encryption Encryption { get; set; }
            public FacilityServiceId FacilityServiceId { get; set; }
            [JsonProperty("ghost")]
            public string GhostString { get; set; }
            public HoldReason HoldReason { get; set; }
            public PlacedOnHold PlacedOnHold { get; set; }
            public Protocol Protocol { get; set; }
            public ReceiveCallRate ReceiveCallRate { get; set; }
            public RemoteNumber RemoteNumber { get; set; }
            [JsonProperty("status")]
            public CallStatus CallStatus { get; set; }
            public TransmitCallRate TransmitCallRate { get; set; }

            public Call()
            {
                CallType = new CallType();
                CallStatus = new CallStatus();
                Duration = new Duration();
            }
        }


        public class Description : ValueProperty
        {
            string _value;

            public string Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                    OnValueChanged();
                }
            }
        }

        public class Defined : ValueProperty
        {
            public bool BoolValue { get; private set; }

            public string Value // Valid values are True/False
            {
                set
                {
                    // If the incoming value is "True" it sets the BoolValue true, otherwise sets it false
                    BoolValue = value == "True";
                    OnValueChanged();
                }
            }
        }

        /*
        public class RoomPreset
        {
            public string CiscoCallId { get; set; }
            public Defined Defined { get; set; }
            public Description2 Description { get; set; }
            public Type5 Type { get; set; }

            public RoomPreset()
            {
                Defined = new Defined();
                Description = new Description2();
                Type = new Type5();
            }
        }
        */



        public class Status
        {
            public Audio Audio { get; set; }
            public Bookings Bookings { get; set; }
            [JsonProperty("call")]
            public List<Call> Calls { get; set; }
            public Cameras Cameras { get; set; }
            [JsonProperty("capabilities")]
            public StatusCapabilities StatusCapabilities { get; set; }
            [JsonProperty("conference")]
            public StatusConference StatusConference { get; set; }
            public Diagnostics Diagnostics { get; set; }
            public Experimental Experimental { get; set; }
            public H323 H323 { get; set; }
            [JsonProperty("httpFeedback")]
            public List<HttpFeedback> HttpFeedbacks { get; set; }
            public MediaChannels MediaChannels { get; set; }
            [JsonProperty("network")]
            public List<Network> Networks { get; set; }
            public NetworkServices NetworkServices { get; set; }
            public Peripherals Peripherals { get; set; }
            public Provisioning Provisioning { get; set; }
            public Proximity Proximity { get; set; }
            public RoomAnalytics RoomAnalytics { get; set; }
            [JsonProperty("roomPreset")]
            public List<RoomPreset> RoomPresets { get; set; }

            public Sip Sip { get; set; }
            public Security Security { get; set; }
            public Standby Standby { get; set; }
            public SystemUnit SystemUnit { get; set; }
            public Time Time { get; set; }
            public UserInterface UserInterface { get; set; }
            public Video Video { get; set; }

            public Status()
            {
                Audio = new Audio();
                Calls = new List<Call>();
                Standby = new Standby();
                Cameras = new Cameras();
                RoomAnalytics = new RoomAnalytics();
                RoomPresets = new List<RoomPreset>();
                StatusConference = new StatusConference();
                SystemUnit = new SystemUnit();
                Video = new Video();
                Networks = new List<Network>();
                Sip = new Sip();
            }
        }

        public class RoomPreset : ConvertiblePreset
        {
            [JsonProperty("id")]
            public string RoomPresetId { get; set; }
            public Defined Defined { get; set; }
            public Description Description { get; set; }
            public Type Type { get; set; }

            public RoomPreset()
            {
                Defined = new Defined();
                Description = new Description();
                Type = new Type();
            }

            public override PresetBase ConvertCodecPreset()
            {
                try
                {
                    var preset = new CodecRoomPreset(UInt16.Parse(RoomPresetId), Description.Value, Defined.BoolValue, true);

                    Debug.Console(2, "Preset ID {0} Converted from Cisco Codec Preset to Essentials Preset", RoomPresetId);

                    return preset;
                }
                catch (Exception e)
                {
                    Debug.Console(2, "Unable to convert preset: {0}. Error: {1}", RoomPresetId, e);
                    return null;
                }
            }

        }


        public class RootObject
        {
            public Status Status { get; set; }

            public RootObject()
            {
                Status = new Status();
            }
        }
    }

    public class CurrentLayouts
    {
        public ActiveLayout ActiveLayout { get; set; }

        public AvailableLayoutsCount AvailableLayoutsCount { get; set; }

        public List<LayoutData> AvailableLayouts { get; set; }




        public CurrentLayouts()
        {
            AvailableLayoutsCount = new AvailableLayoutsCount();
            AvailableLayouts = new List<LayoutData>();
            ActiveLayout = new ActiveLayout();
        }

    }

    public class AvailableLayoutsCount : ValueProperty
    {
        private int _value;

        public int Value
        {
            get { return _value; }
            set
            {
                //Debug.Console(0, "**************VALPROPERTY SET**********");
                _value = value;
                OnValueChanged();
            }
        }
    }

    public class ActiveLayout : ValueProperty
    {
        string _value;

        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                OnValueChanged();
            }
        }

    }


    public class LayoutData
    {
        [JsonProperty("LocalInstanceId")]
        public string StringId { get; set; }
        public LayoutName LayoutName { get; set; }
    }

    public class LayoutName
    {
        public string Value;
    }


}
