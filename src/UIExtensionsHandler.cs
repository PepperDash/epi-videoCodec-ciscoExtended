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
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Queues;

namespace epi_videoCodec_ciscoExtended
{

    public class UIExtensionsHandler
    {        
        private readonly IKeyed _parent;
        private readonly IBasicCommunication _coms;
        private readonly GenericQueue _handler;

        private string _widgetEventData = String.Empty;
        public StringFeedback WidgetEventFeedback { get; private set; }

        public UIExtensionsHandler(IKeyed parent, IBasicCommunication coms, GenericQueue handler)
        {
            _parent = parent;
            _coms = coms;
            _handler = handler;
            WidgetEventFeedback = new StringFeedback(() => _widgetEventData);
            WidgetEventFeedback.OutputChange +=
                (sender, args) => Debug.Console(1, parent, "WidgetEventFeedback Event: {0}", _widgetEventData);
        }

        public void ParseStatus(CiscoCodecEvents.Widget val)
        {
            Debug.Console(1, _parent, "Widget Action: {0}", val.ToString());
            if (val.WidgetAction != null && val.WidgetAction.Type != null)
            {
                var val_ = JsonConvert.SerializeObject(val);
                Debug.Console(1, _parent, "Widget val: {0}", val_);

                var action_ = val.WidgetAction;

                _widgetEventData = JsonConvert.SerializeObject(action_);
                Debug.Console(1, _parent, "WidgetEventFeedback data: {0}", _widgetEventData);
                WidgetEventFeedback.FireUpdate();
            }
        }
        public void ParseStatus(CiscoCodecEvents.UiEvent val)
        {
            Debug.Console(1, _parent, "WidgetEvent: {0}", val.ToString());
            var val_ = JsonConvert.SerializeObject(val);
            Debug.Console(1, _parent, "WidgetEvent val: {0}", val_);
            
            var action_ = new CiscoCodecEvents.WidgetAction();
            if (val.Pressed != null)
            {
                action_.Type = "Pressed";
                action_.Value = val.Pressed.Signal.Value;
            }
            else if (val.Released != null)
            {
                action_.Type = "Released";
                action_.Value = val.Released.Signal.Value;
            }
            else if (val.Clicked != null)
            {
                action_.Type = "Clicked";
                action_.Value = val.Clicked.Signal.Value;
            }
            else
            {
                Debug.Console(1, _parent, "WidgetEvent exiting, no event Type");
                return;
            }
            action_.Id = String.Empty;
            var arr_ = action_.Value.Split(':'); // "tv_menu:menu";
            if(arr_.Length > 1)
            {
                action_.Value = arr_[0]; // "tv_menu"
                action_.Id = arr_[1]; // "menu"
                _widgetEventData = String.Format("/{0} /{1} /{2}", action_.Value, action_.Type, action_.Id); // e.g. "/blinds_stop /pressed"
            }
            else
                _widgetEventData = String.Format("/{0} /{1}", action_.Value, action_.Type); // e.g. "/blinds /pressed /increment"
            Debug.Console(1, _parent, "WidgetEventFeedback data: {0}", _widgetEventData);            
            WidgetEventFeedback.FireUpdate();
        }

        public void RegisterFeedback()
        {
            /* Moved to Registrations class to control the pacing
            // get standard events
            _coms.SendText("xfeedback register /Event/UserInterface/Extensions/Event\r\n");
            // detect pages opened, this is unreliable
            _coms.SendText("xfeedback register /Event/UserInterface/Extensions/PageOpened\r\n");
            // detect pages closed, this doesn't work
            _coms.SendText("xfeedback register /Event/UserInterface/Extensions/PageClosed\r\n");
            // detect changes to the UI Layout file
            _coms.SendText("xfeedback register /Event/UserInterface/Extensions/Widget/LayoutUpdated\r\n");
             */
        }

        public void LinkToApi(BasicTriList trilist, CiscoCodecJoinMap joinMap)
        {
            trilist.SetStringSigAction(joinMap.WidgetEventData.JoinNumber, UpdateWidget); // from SIMPL
            WidgetEventFeedback.LinkInputSig(trilist.StringInput[joinMap.WidgetEventData.JoinNumber]); // to SIMPL
        }

        public void UpdateWidget(string val) // "/blinds /open"
        {
            var arr_ = val.Split('/');
            if (arr_.Length == 3)
            {
                var id_ = arr_[1].Trim();
                var val_ = arr_[2].Trim();
                UpdateWidget<string>(id_, val_);
            }
        }

        public void UpdateWidget<T>(string WidgetId, T Value)
        {
            var command = String.Format("xCommand UserInterface Extensions Widget SetValue WidgetId: \"{0}\" Value: \"{1}\"\r\n", WidgetId, Value);
            _coms.SendText(command);
        }
    }
}