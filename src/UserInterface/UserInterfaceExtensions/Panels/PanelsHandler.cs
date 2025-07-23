using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Timers;
using PepperDash.Core;
using PepperDash.Essentials.Core;

//see comments at bottom for xCommand examples
namespace epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceExtensions.Panels
{

  /// <summary>
  /// Handles the configuration and management of UI extension panels for Cisco codecs.
  /// This class manages panel feedback, click events, and dynamic panel updates based on device state.
  /// </summary>
  /// <remarks>
  /// The PanelsHandler is responsible for:
  /// - Managing panel configurations and their display states
  /// - Handling panel click events and routing them to appropriate handlers
  /// - Updating panel feedback based on device status changes
  /// - Validating panel configurations (e.g., order values must be >= 1)
  /// </remarks>
  public class PanelsHandler : ICiscoCodecUiExtensionsPanelClickedEventHandler
  {
    private readonly IKeyed _parent;
    private readonly List<Panel> _panelConfigs;
    private readonly Action<string> EnqueueCommand;

    private readonly Timer feedbackTimer;

    /// <summary>
    /// Initializes a new instance of the PanelsHandler class with the specified configuration.
    /// </summary>
    /// <param name="parent">The parent device that owns this panels handler.</param>
    /// <param name="enqueueCommand">Action to enqueue commands for sending to the codec.</param>
    /// <param name="config">List of panel configurations to manage.</param>
    /// <remarks>
    /// The constructor validates that all panel configurations have valid order values (>= 1).
    /// If any panel has an invalid order value, the handler will not be registered and an error will be logged.
    /// </remarks>
    public PanelsHandler(IKeyed parent, Action<string> enqueueCommand, List<Panel> config)
    {
      _parent = parent;
      _panelConfigs = config;
      EnqueueCommand = enqueueCommand;
      if (config == null || config.Count == 0)
      {
        Debug.LogInformation(_parent, "No Cisco Panels Configured {config}", config);
        return;
      }
      else if (config.Any((p) => p.Order == 0))
      {
        Debug.LogError(_parent, "0 is an invalid order value. Must be >= 1 {config}.  PanelHandler will not be registered.  Please update order values in config.", config);
        return;
      }

      feedbackTimer = new Timer(1000)
      {
        AutoReset = false,
        Enabled = false
      };

      feedbackTimer.Elapsed += UpdateExtension;

      RegisterFeedback();
      RegisterForDeviceFeedback();
    }

    private void UpdateExtension(object sender, ElapsedEventArgs args)
    {
      if (!(_parent is ICiscoCodecUiExtensions extensions))
      {
        Debug.LogError(_parent, "Parent is not ICiscoCodecUiExtensions, cannot update panels");
        return;
      }

      extensions.Update(EnqueueCommand);
    }

    private void RegisterForDeviceFeedback()
    {
      var panelsWithFeedback = _panelConfigs.Where(p => p.PanelFeedback != null);

      if (!panelsWithFeedback.Any())
      {
        Debug.LogDebug(_parent, "No panels with feedback to register for");
        return;
      }

      foreach (var panel in panelsWithFeedback)
      {
        if (!(DeviceManager.GetDeviceForKey(panel.PanelFeedback.DeviceKey) is IHasFeedback device))
        {
          Debug.LogError(_parent, "Panel {panelId} has feedback but device {deviceKey} not found", panel.PanelId, panel.PanelFeedback.DeviceKey);
          continue;
        }

        var feedback = device.Feedbacks[panel.PanelFeedback.FeedbackKey];

        if (feedback == null)
        {
          Debug.LogError(_parent, "Panel {panelId} has feedback but feedback {feedbackKey} not found on device {deviceKey}", panel.PanelId, panel.PanelFeedback.FeedbackKey, panel.PanelFeedback.DeviceKey);
          continue;
        }

        feedback.OutputChange += (sender, args) =>
        {
          switch (panel.PanelFeedback.FeedbackEventType)
          {
            case eFeedbackEventType.TypeBool:
              {
                var value = args.BoolValue;
                Debug.LogDebug(_parent, "Panel {panelId} feedback changed: {feedbackKey} = {value}", panel.PanelId, panel.PanelFeedback.FeedbackKey, value);

                UpdatePanelProperty(panel, panel.PanelFeedback, value);

                break;
              }
            case eFeedbackEventType.TypeString:
              {
                var value = args.StringValue;
                Debug.LogDebug(_parent, "Panel {panelId} feedback changed: {feedbackKey} = {value}", panel.PanelId, panel.PanelFeedback.FeedbackKey, value);

                UpdatePanelProperty(panel, panel.PanelFeedback, value);

                break;
              }
            case eFeedbackEventType.TypeInt:
              {
                var value = args.IntValue;
                Debug.LogDebug(_parent, "Panel {panelId} feedback changed: {feedbackKey} = {value}", panel.PanelId, panel.PanelFeedback.FeedbackKey, value);

                UpdatePanelProperty(panel, panel.PanelFeedback, value);

                break;
              }
          }

          feedbackTimer.Stop();
          feedbackTimer.Start();
        };
      }
    }

    private void UpdatePanelProperty(Panel panel, PanelFeedback feedbackConfig, bool value)
    {
      switch (feedbackConfig.PropertyToChange)
      {
        case EPanelProperty.Text:
          {
            panel.Name = value ? feedbackConfig.TruePropertyValue : feedbackConfig.FalsePropertyValue;
            break;
          }
        case EPanelProperty.Color:
          {
            var color = value ? feedbackConfig.TruePropertyValue : feedbackConfig.FalsePropertyValue;
            // Regex pattern for hex colors: # followed by exactly 3 or 6 hex digits
            var hexColorPattern = @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$";
            if (!Regex.IsMatch(color, hexColorPattern))
            {
              Debug.LogWarning(_parent, "Panel {panelId} feedback color value is not a valid hex color: {value}", panel.PanelId, color);
              return;
            }
            panel.Color = color;
            break;
          }
      }
    }

    private void UpdatePanelProperty(Panel panel, PanelFeedback feedbackConfig, string value)
    {
      if (!(feedbackConfig.StringFeedbackPropertyValues != null && feedbackConfig.StringFeedbackPropertyValues.TryGetValue(value, out var propertyValue)))
      {
        Debug.LogWarning(_parent, "Panel {panelId} feedback string value not found: {value}", panel.PanelId, value);
        return;
      }

      switch (feedbackConfig.PropertyToChange)
      {
        case EPanelProperty.Text:
          {
            panel.Name = propertyValue;
            break;
          }
        case EPanelProperty.Color:
          {
            // Regex pattern for hex colors: # followed by exactly 3 or 6 hex digits
            var hexColorPattern = @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$";
            if (!Regex.IsMatch(propertyValue, hexColorPattern))
            {
              Debug.LogWarning(_parent, "Panel {panelId} feedback color value is not a valid hex color: {value}", panel.PanelId, propertyValue);
              return;
            }
            panel.Color = propertyValue;
            break;
          }
      }
    }

    private void UpdatePanelProperty(Panel panel, PanelFeedback feedbackConfig, int value)
    {
      if (!(feedbackConfig.StringFeedbackPropertyValues != null && feedbackConfig.IntFeedbackPropertyValues.TryGetValue(value, out var propertyValue)))
      {
        Debug.LogWarning(_parent, "Panel {panelId} feedback string value not found: {value}", panel.PanelId, value);
        return;
      }

      switch (feedbackConfig.PropertyToChange)
      {
        case EPanelProperty.Text:
          {
            panel.Name = propertyValue;
            break;
          }
        case EPanelProperty.Color:
          {
            // Regex pattern for hex colors: # followed by exactly 3 or 6 hex digits
            var hexColorPattern = @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$";
            if (!Regex.IsMatch(propertyValue, hexColorPattern))
            {
              Debug.LogWarning(_parent, "Panel {panelId} feedback color value is not a valid hex color: {value}", panel.PanelId, propertyValue);
              return;
            }
            panel.Color = propertyValue;
            break;
          }
      }
    }

    public void ParseStatus(CiscoCodecEvents.Panel panel)
    {
      Debug.LogDebug(_parent, "PanelsHandler Parse Status Panel Clicked: {panelId}", panel.Clicked.PanelId.Value);
      var pconfig = _panelConfigs.FirstOrDefault((p) => p.PanelId == panel.Clicked.PanelId.Value);
      if (pconfig == null)
      {
        Debug.LogDebug(_parent, "Panel not found in config id: {panelId}", panel.Id);
        return;
      }
      pconfig.OnClickedEvent();
    }

    public void RegisterFeedback()
    {
      //detect button changes for panel buttons ROOM OS
      var cmd = "xfeedback register /Event/UserInterface/Extensions/Panel/Clicked" + CiscoCodec.Delimiter;
      //_coms.SendText(cmd);
      EnqueueCommand(cmd);
    }

    public void DeregisterFeedback()
    {
      var cmd = "xfeedback deregister /Event/UserInterface/Extensions/Panel/Clicked" + CiscoCodec.Delimiter;
      //_coms.SendText(cmd);
      EnqueueCommand(cmd);
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