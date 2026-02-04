using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Timers;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Config;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Navigator;

//see comments at bottom for xCommand examples
namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions.Panels
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
  public class PanelsHandler
  {
    private string defaultRoomKey;

    private string currentScenarioRoomKey;

    private const string hexColorPattern = @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$";
    private readonly CiscoCodec parent;

    private readonly UiExtensions extensionsHandler;
    private readonly List<Panel> panelConfigs;
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
    public PanelsHandler(CiscoCodec parent, UiExtensions extensions, Action<string> enqueueCommand, List<Panel> config)
    {
      this.parent = parent;
      extensionsHandler = extensions;

      panelConfigs = config;
      EnqueueCommand = enqueueCommand;
      if (config == null || config.Count == 0)
      {
        parent.LogInformation("No Cisco Panels Configured {config}", config);
        return;
      }
      else if (config.Any((p) => p.Order == 0))
      {
        parent.LogError("0 is an invalid order value. Must be >= 1 {config}.  PanelHandler will not be registered.  Please update order values in config.", config);
        return;
      }

      feedbackTimer = new Timer(1000)
      {
        AutoReset = false,
        Enabled = false
      };

      feedbackTimer.Elapsed += UpdateExtension;

      RegisterFeedback();
    }

    public void Initialize(string defaultRoomKey)
    {
      this.defaultRoomKey = defaultRoomKey;

      RegisterForDeviceFeedback();

      var combiners = DeviceManager.AllDevices.OfType<EssentialsRoomCombiner>().ToList();

      if (combiners == null || combiners.Count == 0)
      {
        parent.LogWarning("{uiKey} could not find RoomCombiner", parent.Key);
        return;
      }

      if (combiners.Count > 1)
      {
        parent.LogWarning("{uiKey} found more than one RoomCombiner", parent.Key);
        return;
      }

      RegisterForCombinerFeedback(combiners[0]);
    }

    private void RegisterForCombinerFeedback(EssentialsRoomCombiner combiner)
    {
      try
      {
        parent.LogDebug("RoomCombinerHandler setup for {0}", parent, parent.Key);

        combiner.RoomCombinationScenarioChanged += HandleRoomCombineScenarioChanged;

        // Calling event handler directly here to ensure that things are subscribed correctly on startup and the correct room is set up
        HandleRoomCombineScenarioChanged(combiner, EventArgs.Empty);
      }
      catch (Exception e)
      {
        parent.LogError("Error setting up RoomCombinerHandler for {0}: {1}", parent, e);
      }
    }

    private void HandleRoomCombineScenarioChanged(object sender, EventArgs e)
    {
      if (!(sender is EssentialsRoomCombiner combiner))
      {
        parent.LogError("RoomCombiner is null in scenario changed event");
        return;
      }

      var currentScenario = combiner.CurrentScenario;
      if (currentScenario == null)
      {
        parent.LogError("CurrentScenario is null in scenario changed event");
        return;
      }
      var uiMap = currentScenario.UiMap;

      if (!uiMap.TryGetValue(defaultRoomKey, out currentScenarioRoomKey))
      {
        parent.LogError("UiMap default room key: {DefaultRoomKey}. UiMap must have an entry keyed to default room key with value of room connection for room state {ScenarioKey}", defaultRoomKey, currentScenario.Key);
        return;
      }
    }

    private void UpdateExtension(object sender, ElapsedEventArgs args)
    {
      if (!(extensionsHandler is UiExtensions extensions))
      {
        parent.LogError("Parent is not UiExtensions, cannot update panels");
        return;
      }

      extensions.Update(EnqueueCommand);
    }

    void HandleFeedbackOutputChange(object s, FeedbackEventArgs args)
    {
      if (!(s is Feedback feedback))
      {
        parent.LogError("Received feedback event but sender is not a feedback object");
        return;
      }

      parent.LogDebug("Handling feedback output change for feedback {feedbackKey}", feedback.Key);

      // Find all panels and their specific feedbacks that correspond to this feedback AND device combination
      var matchingPanelFeedbacks = new List<(Panel panel, PanelFeedback panelFeedback)>();

      foreach (var panel in panelConfigs)
      {
        foreach (var panelFeedback in panel.GetAllPanelFeedbacks())
        {
          if (panelFeedback.FeedbackKey != feedback.Key)
          {
            parent.LogDebug("Panel {panelId} feedback key does not match feedback sender key",
              panel.PanelId);
            continue;
          }

          var feedbackDevice = DeviceManager.GetDeviceForKey(panelFeedback.DeviceKey) as IHasFeedback;

          if ((feedbackDevice?.Feedbacks[panelFeedback.FeedbackKey]) != feedback)
          {
            parent.LogDebug("Panel {panelId} feedback device key does not match feedback sender device key",
              panel.PanelId);
            continue;
          }

          parent.LogDebug("Found matching panel {panelId} for feedback {feedbackKey}",
            panel.PanelId, feedback.Key);

          matchingPanelFeedbacks.Add((panel, panelFeedback));
        }
      }

      if (!matchingPanelFeedbacks.Any())
      {
        parent.LogWarning("Received feedback event for {feedbackKey} but could not find corresponding panel feedback",
          feedback.Key);
        return;
      }

      // Process feedback for all matching panel-feedback combinations
      foreach (var (panel, panelFeedback) in matchingPanelFeedbacks)
      {
        switch (panelFeedback.FeedbackEventType)
        {
          case eFeedbackEventType.TypeBool:
            {
              var value = args.BoolValue;
              parent.LogDebug("Panel {panelId} feedback changed: {feedbackKey} = {value}",
                panel.PanelId, panelFeedback.FeedbackKey, value);

              UpdatePanelProperty(panel, panelFeedback, value);
              break;
            }
          case eFeedbackEventType.TypeString:
            {
              var value = args.StringValue;
              parent.LogDebug("Panel {panelId} feedback changed: {feedbackKey} = {value}",
                panel.PanelId, panelFeedback.FeedbackKey, value);

              UpdatePanelProperty(panel, panelFeedback, value);
              break;
            }
          case eFeedbackEventType.TypeInt:
            {
              var value = args.IntValue;
              parent.LogDebug("Panel {panelId} feedback changed: {feedbackKey} = {value}",
                panel.PanelId, panelFeedback.FeedbackKey, value);

              UpdatePanelProperty(panel, panelFeedback, value);
              break;
            }
        }
      }

      feedbackTimer.Stop();
      feedbackTimer.Start();
    }

    private void RegisterForDeviceFeedback()
    {
      var panelsWithFeedback = panelConfigs.Where(p => p.GetAllPanelFeedbacks().Any());

      if (!panelsWithFeedback.Any())
      {
        parent.LogDebug("No panels with feedback to register");
        return;
      }

      foreach (var panel in panelsWithFeedback)
      {
        foreach (var panelFeedback in panel.GetAllPanelFeedbacks())
        {
          var deviceKey = panelFeedback.DeviceKey;

          if (!(DeviceManager.GetDeviceForKey(deviceKey) is IHasFeedback device))
          {
            parent.LogError("Panel {panelId} has feedback but device {deviceKey} not found", panel.PanelId, deviceKey);
            continue;
          }

          var feedback = device.Feedbacks[panelFeedback.FeedbackKey];

          if (feedback == null)
          {
            parent.LogError("Panel {panelId} has feedback but feedback {feedbackKey} not found on device {deviceKey}", panel.PanelId, panelFeedback.FeedbackKey, panelFeedback.DeviceKey);
            continue;
          }

          parent.LogDebug("Registering for feedback {feedbackKey} for panel {panelId}", feedback.Key, panel.PanelId);

          feedback.OutputChange += HandleFeedbackOutputChange;
        }
      }
    }

    private void UpdatePanelProperty(Panel panel, PanelFeedback feedbackConfig, bool value)
    {
      parent.LogDebug("Updating panel {panelId} property {property} based on boolean feedback value: {value}",
        panel.PanelId, feedbackConfig.PropertyToChange, value);
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

            if (!Regex.IsMatch(color, hexColorPattern))
            {
              parent.LogWarning("Panel {panelId} feedback color value is not a valid hex color: {value}", panel.PanelId, color);
              return;
            }
            panel.Color = color;
            break;
          }
        case EPanelProperty.Location:
          {
            var locationFromConfig = value ? feedbackConfig.TruePropertyValue : feedbackConfig.FalsePropertyValue;

            if (!Enum.TryParse<ECiscoPanelLocation>(locationFromConfig, out var location))
            {
              parent.LogWarning("Panel {panelId} feedback location value is not a valid enum: {value}", panel.PanelId, locationFromConfig);
              return;
            }

            panel.Location = location;
            break;
          }
      }
    }

    private void UpdatePanelProperty(Panel panel, PanelFeedback feedbackConfig, string value)
    {
      parent.LogDebug("Updating panel {panelId} property {property} based on string feedback value: {value}",
        panel.PanelId, feedbackConfig.PropertyToChange, value);
      if (!(feedbackConfig.StringFeedbackPropertyValues != null && feedbackConfig.StringFeedbackPropertyValues.TryGetValue(value, out var propertyValue)))
      {
        parent.LogWarning("Panel {panelId} feedback string value not found: {value}", panel.PanelId, value);
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
            if (!Regex.IsMatch(propertyValue, hexColorPattern))
            {
              parent.LogWarning("Panel {panelId} feedback color value is not a valid hex color: {value}", panel.PanelId, propertyValue);
              return;
            }
            panel.Color = propertyValue;
            break;
          }
        case EPanelProperty.Location:
          {
            parent.LogWarning("Location is not currently supported for string feedbacks");
            break;
          }
      }
    }

    private void UpdatePanelProperty(Panel panel, PanelFeedback feedbackConfig, int value)
    {
      parent.LogDebug("Updating panel {panelId} property {property} based on integer feedback value: {value}",
        panel.PanelId, feedbackConfig.PropertyToChange, value);
      if (!(feedbackConfig.IntFeedbackPropertyValues != null && feedbackConfig.IntFeedbackPropertyValues.TryGetValue(value, out var propertyValue)))
      {
        parent.LogWarning("Panel {panelId} feedback integer value not found: {value}", panel.PanelId, value);
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
              parent.LogWarning("Panel {panelId} feedback color value is not a valid hex color: {value}", panel.PanelId, propertyValue);
              return;
            }
            panel.Color = propertyValue;
            break;
          }
        case EPanelProperty.Location:
          {
            parent.LogWarning("Location is not currently supported for string feedbacks");
            break;
          }

      }
    }

    public void ParseStatus(CiscoCodecEvents.Panel panel)
    {
      parent.LogDebug("PanelsHandler Parse Status Panel Clicked: {panelId}", panel.Clicked.PanelId.Value);
      var pconfig = panelConfigs.FirstOrDefault((p) => p.PanelId == panel.Clicked.PanelId.Value);
      if (pconfig == null)
      {
        parent.LogDebug("Panel not found in config id: {panelId}", panel.Id);
        return;
      }
      pconfig.OnClickedEvent();
    }

    /// <summary>
    /// Subscribes to or unsubscribes from feedback for a specific panel using its feedback configuration.
    /// </summary>
    /// <param name="panelId">The ID of the panel to subscribe/unsubscribe feedback for.</param>
    /// <param name="subscribe">True to subscribe, false to unsubscribe.</param>
    /// <returns>True if the operation was successful, false if the panel was not found or has no feedback configuration.</returns>
    public bool SubscribeToPanelFeedback(string panelId, bool subscribe)
    {
      if (string.IsNullOrEmpty(panelId))
      {
        parent.LogError("Panel ID cannot be null or empty");
        return false;
      }

      var panel = panelConfigs.FirstOrDefault(p => p.PanelId == panelId);
      if (panel == null)
      {
        parent.LogError("Panel {panelId} not found in configuration", panelId);
        return false;
      }

      var panelFeedbacks = panel.GetAllPanelFeedbacks().ToList();
      if (!panelFeedbacks.Any())
      {
        parent.LogWarning("Panel {panelId} has no feedback configurations", panelId);
        return false;
      }

      bool allSuccessful = true;

      foreach (var panelFeedback in panelFeedbacks)
      {
        var deviceKey = panelFeedback.DeviceKey;

        if (!(DeviceManager.GetDeviceForKey(deviceKey) is IHasFeedback device))
        {
          parent.LogError("Panel {panelId} has feedback but device {deviceKey} not found", panelId, deviceKey);
          allSuccessful = false;
          continue;
        }

        var feedback = device.Feedbacks[panelFeedback.FeedbackKey];
        if (feedback == null)
        {
          parent.LogError("Panel {panelId} has feedback but feedback {feedbackKey} not found on device {deviceKey}",
            panelId, panelFeedback.FeedbackKey, deviceKey);
          allSuccessful = false;
          continue;
        }

        try
        {
          if (subscribe)
          {
            parent.LogDebug("Subscribing to feedback {feedbackKey} for panel {panelId}", feedback.Key, panelId);
            feedback.OutputChange += HandleFeedbackOutputChange;
          }
          else
          {
            parent.LogDebug("Unsubscribing from feedback {feedbackKey} for panel {panelId}", feedback.Key, panelId);
            feedback.OutputChange -= HandleFeedbackOutputChange;
          }
        }
        catch (Exception ex)
        {
          parent.LogError("Error {action} feedback {feedbackKey} for panel {panelId}: {error}",
            subscribe ? "subscribing to" : "unsubscribing from", panelFeedback.FeedbackKey, panelId, ex.Message);
          allSuccessful = false;
        }
      }

      return allSuccessful;
    }

    /// <summary>
    /// Subscribes to feedback for a specific panel using its feedback configuration.
    /// </summary>
    /// <param name="panelId">The ID of the panel to subscribe feedback for.</param>
    /// <returns>True if the subscription was successful, false otherwise.</returns>
    public bool SubscribeToPanelFeedback(string panelId)
    {
      return SubscribeToPanelFeedback(panelId, true);
    }

    /// <summary>
    /// Unsubscribes from feedback for a specific panel.
    /// </summary>
    /// <param name="panelId">The ID of the panel to unsubscribe feedback from.</param>
    /// <returns>True if the unsubscription was successful, false otherwise.</returns>
    public bool UnsubscribeFromPanelFeedback(string panelId)
    {
      return SubscribeToPanelFeedback(panelId, false);
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