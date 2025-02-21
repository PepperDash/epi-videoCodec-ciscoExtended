using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;

//see comments at bottom for xCommand examples
namespace epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceExtensions.Panels
{

    public class PanelsHandler : ICiscoCodecUiExtensionsPanelClickedEventHandler
    {
        private readonly IKeyed _parent;
        private readonly List<Panel> _panelConfigs;
        private Action<string> EnqueueCommand;

        public PanelsHandler(IKeyed parent, Action<string> enqueueCommand, List<Panel> config)
        {
          Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "Constructing PanelsHandler", parent);
          _parent = parent;
          _panelConfigs = config;
          EnqueueCommand = enqueueCommand;

          if (config == null || config.Count == 0)
          {
            Debug.LogMessage(
              Serilog.Events.LogEventLevel.Information,
              "No Cisco Panels Configured {0}", _parent, config);

            return;
          }
          
          if (config.Any((p) => p.Order == 0))
          {
              Debug.LogMessage(
                Serilog.Events.LogEventLevel.Error,
                "0 is an invalid order value. Must be >= 1 {0}.  PanelHandler will not be registered.  Please update order values in config.", 
                _parent, config);

              return;
          }

          RegisterFeedback();

          foreach (var p in config.Where(c => !string.IsNullOrEmpty(c.FaderDeviceKey)))
          {
            if (DeviceManager.GetDeviceForKey(p.FaderDeviceKey) is IBasicVolumeWithFeedback fader)
            {
              fader.MuteFeedback.OutputChange += (sender, args) => 
              {
                var command = args.BoolValue 
                  ? GetColorCommand(p, "#FF5977")
                  : GetColorCommand(p, "#FFFFFF");

                enqueueCommand(command);
              };
            }
          }
        }

        private static string GetColorCommand(Panel p, string color)
        {
            // xCommand UserInterface Extensions Panel Update Color: value Icon: value IconId: value Location: value Name: value PanelId: value Visibility: value
            var sb = new StringBuilder("xCommand UserInterface Extensions Panel Update Color: ");
            sb.Append(color);
            sb.Append(" PanelId: " + p.PanelId);
            sb.Append(" Icon: " + p.Icon);

            if (!string.IsNullOrEmpty(p.Location))
                sb.Append(" Location: " + p.Location);
            if (!string.IsNullOrEmpty(p.Name))
                sb.Append(" Name: " + "\"" + p.Name + "\"");
            if (!string.IsNullOrEmpty(p.IconId))
                sb.Append(" IconId: " + p.IconId);

            sb.Append(" Visibility: Auto");

            return sb.ToString();
        }

        public void ParseStatus(CiscoCodecEvents.Panel panel)
        {
            Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "PanelsHandler Parse Status Panel Clicked: {0}", _parent, panel.Clicked.PanelId.Value);
            var pconfig = _panelConfigs.FirstOrDefault((p) => p.PanelId == panel.Clicked.PanelId.Value);
            if (pconfig == null)
            {
                Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "Panel not found in config id: {0}", _parent, panel.Id);
                return;
            }
            pconfig.OnClickedEvent();
        }

        public void RegisterFeedback()
        {
            Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "PanelsHandler RegisterFeedback", _parent);
            //detect button changes for panel buttons ROOM OS
            var cmd = "xfeedback register /Event/UserInterface/Extensions/Panel/Clicked" + CiscoCodec.Delimiter;
            //_coms.SendText(cmd);
            EnqueueCommand(cmd);
        }

        public void DeregisterFeedback()
        {
            Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "PanelsHandler DeregisterFeedback", _parent);
            var cmd = "xfeedback deregister /Event/UserInterface/Extensions/Panel/Clicked" + CiscoCodec.Delimiter;
            //_coms.SendText(cmd);
            EnqueueCommand(cmd);
        }

        public void LinkToApi(BasicTriList trilist, CiscoCodecJoinMap joinMap)
        {
            Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, "PanelsHandler LinkToApi. NOT IMPLEMENTED", _parent);
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