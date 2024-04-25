using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Core.JsonStandardObjects;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.Queues;

//see comments at bottom for xCommand examples
namespace epi_videoCodec_ciscoExtended.UserInterfaceExtensions.Panels
{

    public class PanelsHandler: ICiscoCodecUiExtensionsHandler
    {
        private readonly IKeyed _parent;
        private readonly IBasicCommunication _coms;
        private readonly List<Panel> _panelConfigs;

        public PanelsHandler(IKeyed parent, IBasicCommunication coms, List<Panel> config)
        {
            _parent = parent;
            _coms = coms;
            _panelConfigs = config;
            if (config == null || config.Count == 0)
            {
                Debug.LogMessage(
                    Serilog.Events.LogEventLevel.Information, 
                    "No Cisco Panels Configured {0}", _parent, config);
                return;
            }
            RegisterFeedback();
        }

        public void ParseStatus(CiscoCodecEvents.Panel panel)
        {
            var pconfig = _panelConfigs.FirstOrDefault((Panel p) => p.PanelId == panel.Clicked.PanelId.Value);
            if (pconfig == null) return;
            pconfig.OnClickedEvent();
        }

        public void RegisterFeedback()
        {
            //detect button changes for panel buttons ROOM OS
            _coms.SendText("xfeedback register /Event/UserInterface/Extensions/Panel/Clicked" + CiscoCodec.Delimiter);
        }

        public void DeregisterFeedback()
        {
            _coms.SendText("xfeedback deregister /Event/UserInterface/Extensions/Panel/Clicked" + CiscoCodec.Delimiter);
        }

        public void LinkToApi(BasicTriList trilist, CiscoCodecJoinMap joinMap)
        {
            //add simpl stuff later
        }
    }
}

/*
 * 
xCommand UserInterface Extensions Set ConfigId: value
Raw data here...
Must end with line with single dot
.

xCommand UserInterface Extensions Set ConfigId: 1
<Extensions>
  <Version>1.11</Version>
  <Panel>
    <Order>1</Order>
    <PanelId>panel_1</PanelId>
    <Origin>local</Origin> 
    <Location>ControlPanel</Location>
    <Icon>Lightbulb</Icon>
    <Name>Mobile Control</Name>
    <ActivityType>Custom</ActivityType>
  </Panel>
  <Panel>
    <Order>2</Order>
    <PanelId>panel_2</PanelId>
    <Origin>local</Origin>
    <Location>ControlPanel</Location>
    <Icon>Handset</Icon>
    <Name>Mobile Control 2</Name>
    <ActivityType>Custom</ActivityType>
  </Panel>
</Extensions>
.

//period on last line required for command

seems like for action button origin is always local and not required and activity type is always custom and not required.
They appear when exporting from web ui but removing them does not affect the set command, and they are not in the api doc. 

 
 {
		  "Event": {
			"UserInterface": {
			  "Extensions": {
				"Panel": {
				  "Clicked": {
					"PanelId": {
					  "Value": "panel_1",
					  "id": "1"
					},
					"id": "1"
				  },
				  "id": "1"
				},
				"id": "1"
			  },
			  "id": "1"
			}
		  }
		}
 
 */