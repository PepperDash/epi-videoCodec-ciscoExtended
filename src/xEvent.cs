using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.WebView;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// Custom converter for backwards compatibility: handles Url as both plain string (older firmware) and object with Value/id (ce26+)
    /// </summary>
    public class UrlConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.Name == "Url";
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            if (token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
            {
                return null;
            }

            var urlInstance = Activator.CreateInstance(objectType);

            if (token.Type == JTokenType.String)
            {
                // Older firmware: plain string
                var valueProp = objectType.GetProperty("Value");
                valueProp?.SetValue(urlInstance, token.Value<string>());
            }
            else if (token.Type == JTokenType.Object)
            {
                // ce26+: { Value, id }
                var valueProp = objectType.GetProperty("Value");
                var idProp = objectType.GetProperty("Id");
                
                valueProp?.SetValue(urlInstance, token["Value"]?.Value<string>());
                idProp?.SetValue(urlInstance, token["id"]?.Value<string>());
            }

            return urlInstance;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var type = value.GetType();
            var valueProp = type.GetProperty("Value");
            var idProp = type.GetProperty("Id");

            var urlValue = (string)valueProp?.GetValue(value);
            var idValue = (string)idProp?.GetValue(value);

            writer.WriteStartObject();
            writer.WritePropertyName("Value");
            writer.WriteValue(urlValue);
            if (!string.IsNullOrEmpty(idValue))
            {
                writer.WritePropertyName("id");
                writer.WriteValue(idValue);
            }
            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// This class exists to capture serialized data sent back by a Cisco codec in JSON output mode
    /// </summary>
    public class CiscoCodecEvents
    {
        public class CauseValue
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class CauseType
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class CauseString
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class OrigCallDirection
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class RemoteUri
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class DisplayName
        {
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class CallId
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class CauseCode
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class CauseOrigin
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class Protocol
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class Duration
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class CallType
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class CallRate
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class Encryption
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class RequestedUri
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class PeopleCountAverage
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get; set; }
        }

        public class CallDisconnect : ValueProperty
        {
            private string _id;
            [JsonProperty("id")]
            public string Id { get { return _id; } set { _id = value; OnValueChanged(); } }
            public CauseValue CauseValue { get; set; }
            public CauseType CauseType { get; set; }
            public CauseString CauseString { get; set; }
            public OrigCallDirection OrigCallDirection { get; set; }
            public RemoteUri RemoteUri { get; set; }
            public DisplayName DisplayName { get; set; }
            public CallId CallId { get; set; }
            public CauseCode CauseCode { get; set; }
            public CauseOrigin CauseOrigin { get; set; }
            public Protocol Protocol { get; set; }
            public Duration Duration { get; set; }
            public CallType CallType { get; set; }
            public CallRate CallRate { get; set; }
            public Encryption Encryption { get; set; }
            public RequestedUri RequestedUri { get; set; }
            public PeopleCountAverage PeopleCountAverage { get; set; }
        }

        public class UserInterface // /Event/UserInterface/
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("Presentation")]
            public Presentation Presentation { get; set; }

            [JsonProperty("Extensions")]
            public UiExtensions Extensions { get; set; } // /Event/UserInterface/Extensions/

            [JsonProperty("webview")]
            public WebViewEvent WebView { get; set; } // /Event/UserInterface/WebView/Display --- not sure if this is the correct path, but we need to capture this event for PWA mode

            public UserInterface()
            {
                //Presentation = new Presentation();
                //Extensions = new UiExtensions();
            }
        }

        public enum eWebViewEventMode
        {
            Unknown,
            Fullscreen,
            Modal,
        }

        public enum eWebViewTarget
        {
            Unknown,
            OSD,
            Controller,
            PersistentWebApp,
            RoomScheduler
        }

        public class WebViewEvent
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("status")]
            public Status Status { get; set; } // /Event/UserInterface/WebView/Status
    
            [JsonProperty("display")]
            public WebViewDisplay Display { get; set; } // /Event/UserInterface/WebView/Display
            
            [JsonProperty("cleared")]
            public WebViewClear Cleared { get; set; } // /Event/UserInterface/WebView/Cleared
        }


        public class DisplayMode : ValueProperty
        {
            private string _value;
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get { return _value; } set { _value = value; OnValueChanged(); } }

            public eWebViewEventMode WebViewEventMode
            {
                get
                {
                    eWebViewEventMode mode;
                    System.Enum.TryParse(Value, true, out mode);
                    return mode;
                }
            }
        }

        public class Target : ValueProperty
        {
            private string _value;
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get { return _value; } set { _value = value; OnValueChanged(); } }

            public eWebViewTarget WebViewTarget
            {
                get
                {
                    eWebViewTarget target;
                    System.Enum.TryParse(Value, true, out target);
                    return target;
                }
            }
        }

        public class UrlProperty
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("Value")]
            public string Value { get; set; }
        }

        public class TitleProperty
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("Value")]
            public string Value { get; set; }
        }

        public class WebViewDisplay
        {
            [JsonProperty("Mode")]
            public DisplayMode Mode { get; set; }

            [JsonProperty("Url")]
            public UrlProperty Url { get; set; }

            [JsonProperty("Target")]
            public Target Target { get; set; }

            [JsonProperty("Title")]
            public TitleProperty Title { get; set; }

            [JsonProperty("id")]
            public string Id { get; set; }
        }

        public class WebViewClear
        {
            [JsonProperty("target")]
            public Target Target { get; set; }

            [JsonProperty("id")]
            public string Id { get; set; }
        }

        [JsonConverter(typeof(UrlConverter))]
        public class Url : ValueProperty
        {
            private string _value;

            [JsonProperty("id")]
            public string Id { get; set; }

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

        public class UiExtensions : ValueProperty // /Event/UserInterface/Extensions/
        {
            //public PageOpened PageOpened { get; set; }
            // PageClosed PageClosed { get; set; }
            //public WidgetAction Action { get; private set; }
            private UiEvent _event;

            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("Event")]
            public UiEvent WidgetEvent
            {
                get { return _event; }
                set
                {
                    _event = value;
                    /*
                    Action = new WidgetAction();
                    if (_event.Pressed != null)
                    {
                        Action.Type = "pressed";
                        Action.Value = _event.Pressed.Signal.Value;
                    }
                    if (_event.Released != null)
                    {
                        Action.Type = "released";
                        Action.Value = _event.Released.Signal.Value;
                    }
                    if (_event.Clicked != null)
                    {
                        Action.Type = "clicked";
                        Action.Value = _event.Clicked.Signal.Value;
                    }
                    //_action.Value = "tv_menu:menu";
                    Action.Id = String.Empty;
                    var arr_ = Action.Value.Split(':');
                    if(arr_.Length > 1)
                    {
                        Action.Value = arr_[0]; // "tv_menu"
                        Action.Id = arr_[1]; // "menu"
                    }
                    OnValueChanged(); 
                     * */
                }
            }

            public Widget Widget { get; set; }

            public PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions.Panels.CiscoCodecEvents.Panel Panel { get; set; }

            public UiExtensions()
            {
                //PageOpened = new PageOpened();
                //PageClosed = new PageClosed();
                //WidgetEvent = new WidgetEvent();
                Widget = new Widget();
                Panel = new PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions.Panels.CiscoCodecEvents.Panel();
            }
        }

        public class UiEvent // /Event/UserInterface/Extensions/Event
        {
            //Clicked Signal: "tv_menu:menu"\n
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("Pressed")]
            public UiEventType Pressed { get; set; }
            [JsonProperty("Released")]
            public UiEventType Released { get; set; }
            [JsonProperty("Clicked")]
            public UiEventType Clicked { get; set; }

            public UiEvent()
            {
            }
        }
        public class UiEventType
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("Signal")]
            public UiEventSignal Signal { get; set; }
        }
        public class UiEventSignal
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("Value")]
            public string Value { get; set; }
        }

        public class Widget : ValueProperty // /Event/UserInterface/Extensions/Widget/
        {
            //public LayoutUpdated LayoutUpdated { get; set; }

            private WidgetAction _action;
            [JsonProperty("Action")]
            public WidgetAction WidgetAction { get { return _action; } set { _action = value; OnValueChanged(); } }

            public Widget()
            {
                //LayoutUpdated = new LayoutUpdated();
                WidgetAction = new WidgetAction();
            }
        }

        public class WidgetAction // /Event/UserInterface/Extensions/Widget/Action/
        {
            // WidgetAction is WidgetEventObject
            [JsonProperty("WidgetId")]
            public string Id { get; set; }
            [JsonProperty("Value")]
            public string Value { get; set; }
            [JsonProperty("Type")]
            public string Type { get; set; }

            //private string _value;
            //public string Value { get { return _value; } set { _value = value; OnValueChanged(); } 
        }

        public class Presentation
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public ExternalSource ExternalSource { get; set; }

            public Presentation()
            {
                ExternalSource = new ExternalSource();
            }
        }
        public class ExternalSource
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public Selected Selected { get; set; }

            public ExternalSource()
            {
                Selected = new Selected();
            }
        }
        public class Selected
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public SourceIdentifier SourceIdentifier { get; set; }

            public Selected()
            {
                SourceIdentifier = new SourceIdentifier();
            }
        }
        public class SourceIdentifier : ValueProperty
        {
            private string _value;
            [JsonProperty("id")]
            public string Id { get; set; }
            public string Value { get { return _value; } set { _value = value; OnValueChanged(); } }
        }
        public class EventObject // renamed from Event, too easy to confuse it with System.Event
        {
            [JsonProperty("CallDisconnect")]
            public CallDisconnect CallDisconnect { get; set; }

            [JsonProperty("UserInterface")]
            public UserInterface UserInterface { get; set; }

            [JsonProperty("WebView")]
            public WebViewDisplay WebView { get; set; }

            public EventObject()
            {
                CallDisconnect = new CallDisconnect();
                UserInterface = new UserInterface();
                WebView = new WebViewDisplay();
            }
        }

        public class RootObject
        {
            [JsonProperty("Event")]
            public EventObject Event { get; set; }

            public RootObject()
            {
                Event = new EventObject();
            }
        }
    }
}