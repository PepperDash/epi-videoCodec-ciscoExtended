using Newtonsoft.Json;
using System;

namespace epi_videoCodec_ciscoExtended
{
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
            public Presentation Presentation { get; set; }
            public UiExtensions Extensions { get; set; } // /Event/UserInterface/Extensions/

            public UserInterface()
            {
                Presentation = new Presentation();
                Extensions = new UiExtensions();
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
                set {
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

            public UiExtensions()
            {
                //PageOpened = new PageOpened();
                //PageClosed = new PageClosed();
                //WidgetEvent = new WidgetEvent();
                Widget = new Widget();
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
            [JsonProperty("Changed")]
            public UiEventType Changed { get; set; }

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

        public class Widget: ValueProperty // /Event/UserInterface/Extensions/Widget/
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
            public CallDisconnect CallDisconnect { get; set; }
            public UserInterface UserInterface { get; set; }

            public EventObject()
            {
                CallDisconnect = new CallDisconnect();
                UserInterface = new UserInterface();
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