using System;
using System.Collections.Generic;
using PepperDash.Core;

using Newtonsoft.Json;


namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// This class exists to capture serialized data sent back by a Cisco codec in JSON output mode
    /// </summary>
    public class CiscoCodecConfiguration
    {
        public class DefaultVolume
        {
            
            public string Value { get; set; }
        }

        public class Dereverberation
        {
            
            public string Value { get; set; }
        }

        public class Mode : ValueProperty
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

        }
        public class AutoAnswerMode : ValueProperty
        {
            public bool BoolValue
            {
                get { return _value.ToLower() == "on"; }
            }

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

        }

        public class NoiseReduction
        {
            
            public string Value { get; set; }
        }

        public class EchoControl
        {
            public Dereverberation Dereverberation { get; set; }
            public Mode Mode { get; set; }
            public NoiseReduction NoiseReduction { get; set; }
        }

        public class Level
        {
            
            public string Value { get; set; }
        }


        public class Microphone
        {
            public EchoControl EchoControl { get; set; }
            public Level Level { get; set; }
            public Mode Mode { get; set; }
        }

        public class Input
        {
            [JsonProperty("microphone")]
            public List<Microphone> InputMicrophones { get; set; }
        }

        public class Enabled
        {
            
            public string Value { get; set; }
        }

        public class Mute
        {
            public Enabled Enabled { get; set; }
        }

        public class Microphones
        {
            public Mute Mute { get; set; }
        }


        public class InternalSpeaker
        {
            public Mode Mode { get; set; }
        }

        public class Output
        {
            public InternalSpeaker InternalSpeaker { get; set; }
        }

        public class RingTone
        {
            
            public string Value { get; set; }
        }

        public class RingVolume : ValueProperty
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

            public int Volume
            {
                get
                {
                    return Int32.Parse(_value);
                }
            }
        }

        public class SoundsAndAlerts
        {
            public RingTone RingTone { get; set; }
            public RingVolume RingVolume { get; set; }

            public SoundsAndAlerts()
            {
                RingVolume = new RingVolume();
            }
        }

        public class Audio
        {
            public DefaultVolume DefaultVolume { get; set; }
            public Input Input { get; set; }
            public Microphones Microphones { get; set; }
            public Output Output { get; set; }
            public SoundsAndAlerts SoundsAndAlerts { get; set; }


            public Audio()
            {
                SoundsAndAlerts = new SoundsAndAlerts();
            }
        }

        public class DefaultMode
        {
            
            public string Value { get; set; }
        }

        public class Backlight
        {
            public DefaultMode DefaultMode { get; set; }
        }

        public class DefaultLevel
        {
            
            public string Value { get; set; }
        }


        public class Brightness
        {
            public DefaultLevel DefaultLevel { get; set; }
            public Mode Mode { get; set; }
        }


        public class Focus
        {
            public Mode Mode { get; set; }
        }



        public class Gamma
        {
            public Level Level { get; set; }
            public Mode Mode { get; set; }
        }

        public class Mirror
        {
            
            public string Value { get; set; }
        }



        public class Whitebalance
        {
            public Level Level { get; set; }
            public Mode Mode { get; set; }
        }

        public class Framerate
        {
            
            public string Value { get; set; }
        }

        public class Camera
        {
            public Framerate Framerate { get; set; }
            public Backlight Backlight { get; set; }
            public Brightness Brightness { get; set; }
            public Focus Focus { get; set; }
            public Gamma Gamma { get; set; }
            public Mirror Mirror { get; set; }
            public Whitebalance Whitebalance { get; set; }
        }

        public class Closeup
        {
            
            public string Value { get; set; }
        }


        public class SpeakerTrack
        {
            public Closeup Closeup { get; set; }
            public Mode Mode { get; set; }
        }

        public class Cameras
        {
            //[JsonConverter(typeof(CameraConverter)), JsonProperty("CameraList")]
            //public List<CameraList> CameraList { get; set; }
            //[JsonProperty("SpeakerTrack")]
            public SpeakerTrack SpeakerTrack { get; set; }

            public Cameras()
            {
                //CameraList = new List<CameraList>();
                SpeakerTrack = new SpeakerTrack();
            }
        }

        public class CameraConverter : JsonConverter
        {
            // this is currently not working
            public override bool CanConvert(System.Type objectType)
            {
                return objectType == typeof(Camera) || objectType == typeof(List<Camera>); // This should not be called but is required for implmentation
            }

            public override object ReadJson(JsonReader reader, System.Type objectType, object existingValue, JsonSerializer serializer)
            {
                try
                {
                    if (reader.TokenType == JsonToken.StartArray)
                    {
                        var l = new List<Camera>();
                        reader.Read();
                        while (reader.TokenType != JsonToken.EndArray)
                        {
                            l.Add(reader.Value as Camera);
                            reader.Read();
                        }
                        Debug.Console(1, "[xConfiguration]: Cameras converted as list");
                        return l;
                    }
                    Debug.Console(1, "[xConfiguration]: CameraList converted as single object and added to list");
                    return new List<Camera> { reader.Value as Camera };
                }
                catch (Exception e)
                {
                    Debug.Console(1, "[xConfiguration]: Unable to convert JSON for camera objects: {0}", e);

                    return new List<Camera>();
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return false;
                }
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException("Write not implemented");
            }
        }

        public class Delay
        {
            
            public string Value { get; set; }
        }


        public class MuteString
        {
            
            public string Value { get; set; }
        }

        public class AutoAnswer
        {
            public Delay Delay { get; set; }
            [JsonProperty("Mode")]
            public AutoAnswerMode AutoAnswerMode { get; set; }
            [JsonProperty("Mute")]
            public MuteString MuteString { get; set; }

            public AutoAnswer()
            {
                AutoAnswerMode = new AutoAnswerMode();
                Delay = new Delay();
                MuteString = new MuteString();
            }
        }

        public class Protocol
        {
            
            public string Value { get; set; }
        }

        public class Rate
        {
            
            public string Value { get; set; }
        }

        public class DefaultCall
        {
            public Protocol Protocol { get; set; }
            public Rate Rate { get; set; }
        }

        public class DefaultTimeout
        {
            
            public string Value { get; set; }
        }

        public class DoNotDisturb
        {
            public DefaultTimeout DefaultTimeout { get; set; }
        }


        public class Encryption
        {
            public Mode Mode { get; set; }
        }


        public class FarEndControl
        {
            public Mode Mode { get; set; }
        }

        public class MaxReceiveCallRate
        {
            
            public string Value { get; set; }
        }

        public class MaxTotalReceiveCallRate
        {
            
            public string Value { get; set; }
        }

        public class MaxTotalTransmitCallRate
        {
            
            public string Value { get; set; }
        }

        public class MaxTransmitCallRate
        {
            
            public string Value { get; set; }
        }


        public class MultiStream
        {
            public Mode Mode { get; set; }
        }

        public class Conference
        {
            public AutoAnswer AutoAnswer { get; set; }
            public DefaultCall DefaultCall { get; set; }
            public DoNotDisturb DoNotDisturb { get; set; }
            public Encryption Encryption { get; set; }
            public FarEndControl FarEndControl { get; set; }
            public MaxReceiveCallRate MaxReceiveCallRate { get; set; }
            public MaxTotalReceiveCallRate MaxTotalReceiveCallRate { get; set; }
            public MaxTotalTransmitCallRate MaxTotalTransmitCallRate { get; set; }
            public MaxTransmitCallRate MaxTransmitCallRate { get; set; }
            public MultiStream MultiStream { get; set; }

            public Conference()
            {
                AutoAnswer = new AutoAnswer();
            }
        }

        public class LoginName
        {
            
            public string Value { get; set; }
        }


        public class Password
        {         
            public string Value { get; set; }
        }

        public class Authentication
        {
            public LoginName LoginName { get; set; }
            public Mode Mode { get; set; }
            public Password Password { get; set; }
        }


        public class CallSetup
        {
            public Mode Mode { get; set; }
        }

        public class KeySize
        {
            
            public string Value { get; set; }
        }

        public class H323Encryption
        {
            public KeySize KeySize { get; set; }
        }

        public class Address
        {
            
            public string Value { get; set; }
        }

        public class Gatekeeper
        {
            public Address Address { get; set; }
        }

        public class E164 : ValueProperty
        {
            private string _value;
            public string Value { get { return _value; }
                set
                {
                    _value = value;
                    OnValueChanged();
                }
            }
        }

        public class Id : ValueProperty
        {
            private string _value;


            public string Value { get { return _value; } set { _value = value; OnValueChanged(); } }
        }

        public class H323Alias
        {
            public E164 E164 { get; set; }
            [JsonProperty("id")]
            public Id H323AliasId { get; set; }

            public H323Alias()
            {
                E164 = new E164();
                H323AliasId = new Id();
            }
        }



        public class Nat
        {
            public Address Address { get; set; }
            public Mode Mode { get; set; }
        }

        public class H323
        {
            public Authentication Authentication { get; set; }
            public CallSetup CallSetup { get; set; }
            [JsonProperty("encryption")]
            public H323Encryption H323Encryption { get; set; }
            public Gatekeeper Gatekeeper { get; set; }
            public H323Alias H323Alias { get; set; }
            public Nat Nat { get; set; }

            public H323()
            {
                H323Alias = new H323Alias();

            }
        }

        public class Name
        {    
            public string Value { get; set; }
        }

        public class Domain
        {
            public Name Name { get; set; }
            public string Value { get; set; }

            public Domain()
            {
                Name = new Name();
            }
        }


        public class ServerBase
        {
            [JsonProperty("id")]
            public string ServerId { get; set; }
            public Address Address { get; set; }
        }

        public class Dns
        {
            public Domain Domain { get; set; }
            public List<ServerBase> Server { get; set; }
        }

        public class AnonymousIdentity
        {
            
            public string Value { get; set; }
        }

        public class Md5
        {
            
            public string Value { get; set; }
        }

        public class Peap
        {
            
            public string Value { get; set; }
        }

        public class Tls
        {
            
            public string Value { get; set; }
        }

        public class Ttls
        {
            
            public string Value { get; set; }
        }

        public class Eap
        {
            public Md5 Md5 { get; set; }
            public Peap Peap { get; set; }
            public Tls Tls { get; set; }
            public Ttls Ttls { get; set; }
        }

        public class Identity
        {
            
            public string Value { get; set; }
        }



        public class TlsVerify
        {
            
            public string Value { get; set; }
        }

        public class UseClientCertificate
        {
            
            public string Value { get; set; }
        }

        public class Ieee8021X
        {
            public AnonymousIdentity AnonymousIdentity { get; set; }
            public Eap Eap { get; set; }
            public Identity Identity { get; set; }
            public Mode Mode { get; set; }
            public Password Password { get; set; }
            public TlsVerify TlsVerify { get; set; }
            public UseClientCertificate UseClientCertificate { get; set; }
        }

        public class IpStack
        {
            
            public string Value { get; set; }
        }


        public class Assignment
        {
            
            public string Value { get; set; }
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
            public Assignment Assignment { get; set; }
            public Gateway Gateway { get; set; }
            public SubnetMask SubnetMask { get; set; }

            public IPv4()
            {
                Address = new Address();
            }
        }



        public class DhcpOptions
        {
            
            public string Value { get; set; }
        }

        public class Gateway2
        {
            
            public string Value { get; set; }
        }

// ReSharper disable once InconsistentNaming
        public class IPv6
        {
            public Address Address { get; set; }
            public Assignment Assignment { get; set; }
            public DhcpOptions DhcpOptions { get; set; }
            public Gateway Gateway { get; set; }
        }

        public class Mtu
        {
            
            public string Value { get; set; }
        }

        public class AudioString
        {
            
            public string Value { get; set; }
        }

        public class Data
        {
            
            public string Value { get; set; }
        }

        public class IcmPv6
        {
            
            public string Value { get; set; }
        }

        public class Ntp
        {
            
            public string Value { get; set; }
        }

        public class Signalling
        {
            
            public string Value { get; set; }
        }

        public class Video
        {
            
            public string Value { get; set; }
        }

        public class Diffserv
        {
            [JsonProperty("audio")]
            public AudioString AudioString { get; set; }
            public Data Data { get; set; }
            public IcmPv6 IcmPv6 { get; set; }
            public Ntp Ntp { get; set; }
            public Signalling Signalling { get; set; }
            public Video Video { get; set; }
        }


        public class QoS
        {
            public Diffserv Diffserv { get; set; }
            public Mode Mode { get; set; }
        }

        public class Allow
        {
            
            public string Value { get; set; }
        }

        public class RemoteAccess
        {
            public Allow Allow { get; set; }
        }

        public class Speed
        {
            
            public string Value { get; set; }
        }



        public class VlanId
        {
            
            public string Value { get; set; }
        }

        public class Voice
        {
            public Mode Mode { get; set; }
            public VlanId VlanId { get; set; }
        }

        public class Vlan
        {
            public Voice Voice { get; set; }
        }

        public class Network
        {
            [JsonProperty("id")]
            public string NetworkIdString { get; set; }
            public Dns Dns { get; set; }
            public Ieee8021X Ieee8021X { get; set; }
            public IpStack IpStack { get; set; }
// ReSharper disable once InconsistentNaming
            public IPv4 IPv4 { get; set; }
// ReSharper disable once InconsistentNaming
            public IPv6 IPv6 { get; set; }
            public Mtu Mtu { get; set; }
            public QoS QoS { get; set; }
            public RemoteAccess RemoteAccess { get; set; }
            public Speed Speed { get; set; }
            public Vlan Vlan { get; set; }

            public Network()
            {
                IPv4 = new IPv4();
            }
        }



        public class Cdp
        {
            public Mode Mode { get; set; }
        }



        public class H3232
        {
            public Mode Mode { get; set; }
        }



        public class Http
        {
            public Mode Mode { get; set; }
        }

        public class MinimumTlsVersion
        {
            
            public string Value { get; set; }
        }

        public class TlsServer
        {
            public MinimumTlsVersion MinimumTlsVersion { get; set; }
        }

        public class StrictTransportSecurity
        {
            
            public string Value { get; set; }
        }

        public class VerifyClientCertificate
        {
            
            public string Value { get; set; }
        }

        public class VerifyServerCertificate
        {
            
            public string Value { get; set; }
        }

        public class Https
        {
            public TlsServer Server { get; set; }
            public StrictTransportSecurity StrictTransportSecurity { get; set; }
            public VerifyClientCertificate VerifyClientCertificate { get; set; }
            public VerifyServerCertificate VerifyServerCertificate { get; set; }
        }




        public class NtpService
        {
            public Mode Mode { get; set; }
            public List<ServerBase> Server { get; set; }
        }


        public class Sip
        {
            public Mode Mode { get; set; }
        }

        public class CommunityName
        {
            
            public string Value { get; set; }
        }


        public class Host
        {
            [JsonProperty("id")]
            public string HostIdString { get; set; }
            public Address Address { get; set; }
        }


        public class SystemContact
        {
            
            public string Value { get; set; }
        }

        public class SystemLocation
        {
            
            public string Value { get; set; }
        }

        public class Snmp
        {
            public CommunityName CommunityName { get; set; }
            [JsonProperty("host")]
            public List<Host> Hosts { get; set; }
            public Mode Mode { get; set; }
            public SystemContact SystemContact { get; set; }
            public SystemLocation SystemLocation { get; set; }
        }


        public class Ssh
        {
            public Mode Mode { get; set; }
        }


        public class UpnP
        {
            public Mode Mode { get; set; }
        }

        public class WelcomeText
        {
            
            public string Value { get; set; }
        }

        public class NetworkServices
        {
            public Cdp Cdp { get; set; }
            [JsonProperty("h323")]
            public H3232 H323Service { get; set; }
            public Http Http { get; set; }
            public Https Https { get; set; }
            [JsonProperty("ntp")]
            public NtpService NtpService { get; set; }
            public Sip Sip { get; set; }
            public Snmp Snmp { get; set; }
            public Ssh Ssh { get; set; }
            public UpnP UpnP { get; set; }
            public WelcomeText WelcomeText { get; set; }
        }

        public class ProfileCameras
        {
            
            public string Value { get; set; }
        }

        public class ControlSystems
        {
            
            public string Value { get; set; }
        }

        public class TouchPanels
        {
            
            public string Value { get; set; }
        }

        public class Profile
        {
            [JsonProperty("cameras")]
            public ProfileCameras ProfileCameras { get; set; }
            public ControlSystems ControlSystems { get; set; }
            public TouchPanels TouchPanels { get; set; }
        }

        public class Peripherals
        {
            public Profile Profile { get; set; }
        }


        public class Type
        {
            
            public string Value { get; set; }
        }

        public class Url
        {
            
            public string Value { get; set; }
        }

        public class PhonebookServer
        {
            public Type Type { get; set; }
            public Url Url { get; set; }
        }

        public class Phonebook
        {
            public List<PhonebookServer> Server { get; set; }
        }

        public class Connectivity
        {
            
            public string Value { get; set; }
        }


        public class AlternateAddress
        {
            
            public string Value { get; set; }
        }

        public class Domain2
        {
            
            public string Value { get; set; }
        }

        public class Path
        {
            
            public string Value { get; set; }
        }

        public class Protocol2
        {
            
            public string Value { get; set; }
        }

        public class ExternalManager
        {
            public Address Address { get; set; }
            public AlternateAddress AlternateAddress { get; set; }
            public Domain Domain { get; set; }
            public Path Path { get; set; }
            public Protocol Protocol { get; set; }
        }

        public class HttpMethod
        {
            
            public string Value { get; set; }
        }




        public class Provisioning
        {
            public Connectivity Connectivity { get; set; }
            public ExternalManager ExternalManager { get; set; }
            public HttpMethod HttpMethod { get; set; }
            public LoginName LoginName { get; set; }
            public Mode Mode { get; set; }
            public Password Password { get; set; }
        }


        public class CallControl
        {
            
            public string Value { get; set; }
        }

        public class FromClients
        {
            
            public string Value { get; set; }
        }

        public class ToClients
        {
            
            public string Value { get; set; }
        }

        public class ContentShare
        {
            public FromClients FromClients { get; set; }
            public ToClients ToClients { get; set; }
        }

        public class Services
        {
            public CallControl CallControl { get; set; }
            public ContentShare ContentShare { get; set; }
        }

        public class Proximity
        {
            public Mode Mode { get; set; }
            public Services Services { get; set; }
        }

        public class PeopleCountOutOfCall
        {
            
            public string Value { get; set; }
        }

        public class PeoplePresenceDetector
        {
            
            public string Value { get; set; }
        }

        public class RoomAnalytics
        {
            public PeopleCountOutOfCall PeopleCountOutOfCall { get; set; }
            public PeoplePresenceDetector PeoplePresenceDetector { get; set; }
        }


        public class UserName
        {
            
            public string Value { get; set; }
        }

        public class SipAuthentication
        {
            public Password Password { get; set; }
            public UserName UserName { get; set; }
        }

        public class DefaultTransport
        {
            
            public string Value { get; set; }
        }

        public class DisplayName
        {
            
            public string Value { get; set; }
        }

        public class DefaultCandidate
        {
            
            public string Value { get; set; }
        }


        public class Ice
        {
            public DefaultCandidate DefaultCandidate { get; set; }
            public Mode Mode { get; set; }
        }

        public class ListenPort
        {
            
            public string Value { get; set; }
        }


        public class Proxy
        {
            [JsonProperty("id")]
            public string ProxyId { get; set; }
            public Address Address { get; set; }
        }


        public class TurnServer
        {
            
            public string Value { get; set; }
        }


        public class Turn
        {
            public Password Password { get; set; }
            public TurnServer Server { get; set; }
            public UserName UserName { get; set; }
        }

        public class CiscoCodecUri
        {
            
            public string Value { get; set; }
        }

        public class SipConfiguration
        {
            [JsonProperty("authentication")]
            public SipAuthentication SipAuthentication { get; set; }
            public DefaultTransport DefaultTransport { get; set; }
            public DisplayName DisplayName { get; set; }
            public Ice Ice { get; set; }
            public ListenPort ListenPort { get; set; }
            [JsonProperty("Proxy")]
            public List<Proxy> Proxies { get; set; }
            public Turn Turn { get; set; }
            public CiscoCodecUri Uri { get; set; }
        }

        public class BaudRate
        {
            
            public string Value { get; set; }
        }

        public class LoginRequired
        {
            
            public string Value { get; set; }
        }


        public class SerialPort
        {
            public BaudRate BaudRate { get; set; }
            public LoginRequired LoginRequired { get; set; }
            public Mode Mode { get; set; }
        }

        public class BootAction
        {
            
            public string Value { get; set; }
        }

        public class Control
        {
            
            public string Value { get; set; }
        }

        public class StandbyAction
        {
            
            public string Value { get; set; }
        }

        public class WakeupAction
        {
            
            public string Value { get; set; }
        }

        public class Standby
        {
            public BootAction BootAction { get; set; }
            public Control Control { get; set; }
            public Delay Delay { get; set; }
            public StandbyAction StandbyAction { get; set; }
            public WakeupAction WakeupAction { get; set; }
        }


        public class SystemUnit
        {
            public Name Name { get; set; }
        }

        public class DateFormat
        {
            
            public string Value { get; set; }
        }

        public class TimeFormat
        {
            
            public string Value { get; set; }
        }

        public class Zone
        {
            
            public string Value { get; set; }
        }

        public class Time
        {
            public DateFormat DateFormat { get; set; }
            public TimeFormat TimeFormat { get; set; }
            public Zone Zone { get; set; }
        }


        public class ContactInfo
        {
            public Type Type { get; set; }
        }


        public class KeyTones
        {
            public Mode Mode { get; set; }
        }

        public class Language
        {
            
            public string Value { get; set; }
        }

        public class OsdOutput
        {         
            public string Value { get; set; }
        }

        public class Osd
        {
            [JsonProperty("output")]
            public OsdOutput OsdOutput { get; set; }
        }

        public class UserInterface
        {
            public ContactInfo ContactInfo { get; set; }
            public KeyTones KeyTones { get; set; }
            public Language Language { get; set; }
            public Osd Osd { get; set; }
        }

        public class Filter
        {
            
            public string Value { get; set; }
        }

        public class Group
        {
            
            public string Value { get; set; }
        }

        public class Admin
        {
            public Filter Filter { get; set; }
            public Group Group { get; set; }
        }

        public class Attribute
        {
            
            public string Value { get; set; }
        }

        public class BaseDn
        {
            
            public string Value { get; set; }
        }

        public class LdapEncryption
        {
            
            public string Value { get; set; }

        }




        public class Port
        {
            
            public string Value { get; set; }
        }

        public class LdapServer
        {
            public Address Address { get; set; }
            public Port Port { get; set; }
        }


        public class Ldap
        {
            public Admin Admin { get; set; }
            public Attribute Attribute { get; set; }
            public BaseDn BaseDn { get; set; }
            [JsonProperty("encryption")]
            public LdapEncryption LdapEncryption { get; set; }
            [JsonProperty("minimumTlsVersion")]
            public MinimumTlsVersion LdapMinimumTlsVersion { get; set; }
            public Mode Mode { get; set; }
            public LdapServer Server { get; set; }
            public VerifyServerCertificate VerifyServerCertificate { get; set; }
        }

        public class UserManagement
        {
            public Ldap Ldap { get; set; }
        }

        public class DefaultMainSource
        {
            
            public string Value { get; set; }
        }

        public class CameraId
        {
            
            public string Value { get; set; }
        }


        public class CameraControl
        {
            public CameraId CameraId { get; set; }
            public Mode Mode { get; set; }
        }

        public class InputSourceType
        {
            
            public string Value { get; set; }
        }


        public class PreferredResolution
        {
            
            public string Value { get; set; }
        }

        public class PresentationSelection
        {
            
            public string Value { get; set; }
        }

        public class Quality
        {
            
            public string Value { get; set; }
        }

        public class Visibility
        {
            
            public string Value { get; set; }
        }

        public class Connector
        {
            [JsonProperty("id")]
            public string ConnectorIdString { get; set; }
            public CameraControl CameraControl { get; set; }
            public InputSourceType InputSourceType { get; set; }
            public Name Name { get; set; }
            public PreferredResolution PreferredResolution { get; set; }
            public PresentationSelection PresentationSelection { get; set; }
            public Quality Quality { get; set; }
            public Visibility Visibility { get; set; }
        }

        public class ConfigurationVideoInput
        {
            [JsonProperty("connector")]
            public List<Connector> Connectors { get; set; }
        }

        public class Monitors
        {
            
            public string Value { get; set; }
        }


        public class Cec
        {
            public Mode Mode { get; set; }
        }

        public class MonitorRole
        {
            
            public string Value { get; set; }
        }

        public class Resolution
        {
            
            public string Value { get; set; }
        }

        public class ConfigurationVideoOutputConnector
        {
            [JsonProperty("id")]
            public string IdString { get; set; }
            public Cec Cec { get; set; }
            public MonitorRole MonitorRole { get; set; }
            public Resolution Resolution { get; set; }
        }

        public class ConfigurationVideoOutput
        {
            [JsonProperty("connector")]
            public List<ConfigurationVideoOutputConnector> Connectors { get; set; }
        }

        public class DefaultSource
        {
            
            public string Value { get; set; }
        }

        public class Presentation
        {
            public DefaultSource DefaultSource { get; set; }
        }

        public class FullscreenMode
        {
            
            public string Value { get; set; }
        }


        public class OnMonitorRole
        {
            
            public string Value { get; set; }
        }

        public class PipPosition
        {
            
            public string Value { get; set; }
        }

        public class Default
        {
            public FullscreenMode FullscreenMode { get; set; }
            public Mode Mode { get; set; }
            public OnMonitorRole OnMonitorRole { get; set; }
            public PipPosition PipPosition { get; set; }
        }

        public class Duration
        {
            
            public string Value { get; set; }
        }


        public class OnCall
        {
            public Duration Duration { get; set; }
            public Mode Mode { get; set; }
        }

        public class Selfview
        {
            public Default Default { get; set; }
            public OnCall OnCall { get; set; }
        }

        public class ConfigurationVideo
        {
            public DefaultMainSource DefaultMainSource { get; set; }
            [JsonProperty("input")]
            public ConfigurationVideoInput ConfigurationVideoInput { get; set; }
            public Monitors Monitors { get; set; }
            [JsonProperty("output")]
            public ConfigurationVideoOutput ConfigurationVideoOutput { get; set; }
            public Presentation Presentation { get; set; }
            public Selfview Selfview { get; set; }
        }

        public class Configuration
        {
            public Audio Audio { get; set; }
            public Cameras Cameras { get; set; }
            public Conference Conference { get; set; }
            public H323 H323 { get; set; }
            [JsonProperty("network")]
            public List<Network> Networks { get; set; }
            public NetworkServices NetworkServices { get; set; }
            public Peripherals Peripherals { get; set; }
            public Phonebook Phonebook { get; set; }
            public Provisioning Provisioning { get; set; }
            public Proximity Proximity { get; set; }
            public RoomAnalytics RoomAnalytics { get; set; }
            [JsonProperty("sip")]
            public SipConfiguration SipConfiguration { get; set; }
            public SerialPort SerialPort { get; set; }
            public Standby Standby { get; set; }
            public SystemUnit SystemUnit { get; set; }
            public Time Time { get; set; }
            public UserInterface UserInterface { get; set; }
            public UserManagement UserManagement { get; set; }
            [JsonProperty("video")]
            public ConfigurationVideo ConfigurationVideo { get; set; }

            public Configuration()
            {
                Audio = new Audio();
                Conference = new Conference();
                Networks = new List<Network>();
                H323 = new H323();
            }
        }

        public class RootObject
        {
            public Configuration Configuration { get; set; }

            public RootObject()
            {
                Configuration = new Configuration();
            }
        }
    }
}