//-----------------------------------------------------------------------
// file="UIExtensionsHandler.cs"
//     20221124 Rod Driscoll
//     e: rodney.driscoll@thecigroup.com.au
//     m: +61 2 9223 3955
// https://github.com/PepperDash/epi-videoCodec-ciscoExtended
// Licensing is contained in the LICENSE file located in the root folder of the project source code.
//
// Part of the PepperDash Essentials Plugin: epi-videoCodec-ciscoExtended
// Presents UI Extension events as SerialFeedback in json format
// e.g. WidgetFeedbackString == "{ \"WigetId\": \"blinds\", \"Type\": \"pressed\", \"Value"\:\"increment\" }"
// 
// Used DoNotDisturbHandler as a template
//-----------------------------------------------------------------------

using System;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Queues;
using Feedback = PepperDash.Essentials.Core.Feedback;

namespace epi_videoCodec_ciscoExtended
{

    public class UiExtensionsHandler
    {        
        private readonly IKeyed _parent;
        private readonly IBasicCommunication _coms;
        private readonly GenericQueue _handler;

        private string _widgetEventStringData = String.Empty;

        private ushort WidgetEventIntData
        {
            get
            {
                try
                {
                    return ushort.Parse(_widgetEventStringData);
                }
                catch (FormatException)
                {
                    return 0;
                }
                catch (OverflowException)
                {
                    return 0;
                }
            }
        }

        private bool WidgetEventBoolData
        {
            get { return _widgetEventStringData.ToLower() == "true" || _widgetEventStringData.ToLower() == "on"; }
        }

        public StringFeedback WidgetEventStringFeedback { get; private set; }
        public IntFeedback WidgetEventIntFeedback { get; private set; }
        public BoolFeedback WidgetEventBoolFeedback { get; private set; }
        public FeedbackCollection<PepperDash.Essentials.Core.Feedback> WidgetEventFeedback { get; private set; } 

        public UiExtensionsHandler(IKeyed parent, IBasicCommunication coms, GenericQueue handler)
        {
            _parent = parent;
            _coms = coms;
            _handler = handler;
            WidgetEventStringFeedback = new StringFeedback(() => _widgetEventStringData);
            WidgetEventIntFeedback = new IntFeedback(() => WidgetEventIntData);
            WidgetEventBoolFeedback = new BoolFeedback(() => WidgetEventBoolData);
            WidgetEventStringFeedback.OutputChange +=
                (sender, args) => Debug.Console(1, parent, "WidgetEventFeedback Event: {0}", _widgetEventStringData);
            WidgetEventFeedback = new FeedbackCollection<Feedback>
            {
                WidgetEventStringFeedback, WidgetEventIntFeedback, WidgetEventBoolFeedback
            };
        }

        public void ParseStatus(CiscoCodecEvents.Widget val)
        {
            //Removes Inspection Notice - Keeping for future use and constructor consistency
            if (_handler == null) { /*stuff*/}
            if (val.WidgetAction == null || val.WidgetAction.Type == null) return;
            var serializedVal = JsonConvert.SerializeObject(val);
            Debug.Console(1, _parent, "Widget val: {0}", serializedVal);

            var action = val.WidgetAction;

            _widgetEventStringData = JsonConvert.SerializeObject(action);
            Debug.Console(1, _parent, "WidgetEventFeedback data: {0}", _widgetEventStringData);
            foreach (var item in WidgetEventFeedback)
            {
                var feedback = item;
                feedback.FireUpdate();
            }
        }
        public void ParseStatus(CiscoCodecEvents.UiEvent val)
        {
            Debug.Console(1, _parent, "WidgetEvent: {0}", val.ToString());
            var serializedVal = JsonConvert.SerializeObject(val);
            Debug.Console(1, _parent, "WidgetEvent val: {0}", serializedVal);
            
            var action = new CiscoCodecEvents.WidgetAction();
            if (val.Pressed != null)
            {
                action.Type = "Pressed";
                action.Value = val.Pressed.Signal.Value;
            }
            else if (val.Released != null)
            {
                action.Type = "Released";
                action.Value = val.Released.Signal.Value;
            }
            else if (val.Clicked != null)
            {
                action.Type = "Clicked";
                action.Value = val.Clicked.Signal.Value;
            }
            else if (val.Changed != null)
            {
                action.Type = "Changed";
                action.Value = val.Changed.Signal.Value;
            }
            else
            {
                Debug.Console(1, _parent, "WidgetEvent exiting, no event Type");
                return;
            }
            action.Id = String.Empty;
            var arr = action.Value.Split(':'); // "tv_menu:menu";
            if(arr.Length > 1)
            {
                action.Value = arr[0]; // "tv_menu"
                action.Id = arr[1]; // "menu"
                _widgetEventStringData = String.Format("/{0} /{1} /{2}", action.Value, action.Type, action.Id); // e.g. "/blinds_stop /pressed"
            }
            else
                _widgetEventStringData = String.Format("/{0} /{1}", action.Value, action.Type); // e.g. "/blinds /pressed /increment"
            Debug.Console(1, _parent, "WidgetEventFeedback data: {0}", _widgetEventStringData);
            foreach (var item in WidgetEventFeedback)
            {
                var feedback = item;
                feedback.FireUpdate();
            }
        }

        public void RegisterFeedback()
        {
            // get standard events
            _coms.SendText("xfeedback register /Event/UserInterface/Extensions/Event\r\n");
            // detect pages opened, this is unreliable
            _coms.SendText("xfeedback register /Event/UserInterface/Extensions/PageOpened\r\n");
            // detect pages closed, this doesn't work
            _coms.SendText("xfeedback register /Event/UserInterface/Extensions/PageClosed\r\n");
            // detect changes to the UI Layout file
            _coms.SendText("xfeedback register /Event/UserInterface/Extensions/Widget/LayoutUpdated\r\n");
        }

        public void LinkToApi(BasicTriList trilist, CiscoCodecJoinMap joinMap)
        {
            trilist.SetStringSigAction(joinMap.WidgetEventData.JoinNumber, UpdateWidget); // from SIMPL
            WidgetEventStringFeedback.LinkInputSig(trilist.StringInput[joinMap.WidgetEventData.JoinNumber]); // to SIMPL
            WidgetEventBoolFeedback.LinkInputSig(trilist.BooleanInput[joinMap.WidgetEventData.JoinNumber]); // to SIMPL
            WidgetEventIntFeedback.LinkInputSig(trilist.UShortInput[joinMap.WidgetEventData.JoinNumber]); // to SIMPL
        }

        public void UpdateWidget(string data) // "/blinds /open"
        {
            var arr = data.Split('/');
            if (arr.Length != 3) return;
            var id = arr[1].Trim();
            var val = arr[2].Trim();
            if (val == null) throw new ArgumentNullException("val");
            UpdateWidget(id, val);
        }

        public void UpdateWidget<T>(string widgetId, T value)
        {
            var command = String.Format("xCommand UserInterface Extensions Widget SetValue WidgetId: \"{0}\" Value: \"{1}\"\r\n", widgetId, value);
            _coms.SendText(command);
        }
    }
}