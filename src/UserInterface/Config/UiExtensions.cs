using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Newtonsoft.Json;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions.Panels;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.WebView;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Xml;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Config
{
    /// <summary>
    /// json config to build xml commands
    /// xml config for command raw data to configure UIExtensions via API
    /// Json attributes for config, xml attributes for command structure
    /// </summary>
    [XmlRoot("Extensions")]
    public class UiExtensions
    {
        private CiscoCodec parent;
        [XmlElement("Version")]
        public string Version { get; set; }

        [XmlIgnore]
        [JsonProperty("configId")] //0-40
        public ushort ConfigId { get; set; }

        [XmlElement("Panel")]
        [JsonProperty("panels")]
        public List<Panel> Panels { get; set; }

        [XmlIgnore]
        [JsonProperty("doNotSendXml")]
        public bool SkipXml { get; set; }
        // Lets you skip sending the XML command on init if they are added externally

        //other extensions later

        [JsonIgnore]
        [XmlIgnore]
        public PanelsHandler PanelsHandler { get; set; }

        private static readonly Panel classroomPanel = new Panel
        {
            PanelId = "Classroom",
            Origin = "local",
            ActivityType = "Custom",
            Type = "StatusBar",
            Icon = ECiscoPanelIcons.VideoLayoutPresenter,
            Order = 1,
            Name = "Classroom",
            Location = ECiscoPanelLocation.HomeScreenAndCallControls,
            Page = new Page
            {
                Name = "Classroom",
                Options = "hideRowNames=1",
                PageId = "briefing_page",
                Rows = new List<Row>
                {
                    { new Row
                    {
                            Widgets = new List<Widget>
                            {
                                { new Widget
                                    {
                                        WidgetId = "local_presenter",
                                        Type = WidgetType.Button,
                                        Name = "Local Presenter",
                                        Options = "size=3"
                                    }
                                }
                            }
                        }
                    },
                    { new Row
                        {
                            Widgets = new List<Widget>
                            {
                                { new Widget
                                    {
                                        WidgetId = "remote_presenter",
                                        Type = WidgetType.Button,
                                        Name = "Remote Presenter",
                                        Options = "size=3"
                                    }
                                }
                            }
                        }
                    },
                    { new Row
                        {
                            Widgets = new List<Widget>
                            {
                                { new Widget
                                    {
                                        WidgetId = "discussion",
                                        Type = WidgetType.Button,
                                        Name = "Discussion",
                                        Options = "size=3"
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        /// <summary>
        /// Initializes the Extensions object, setting up the PanelsHandler and sending the XML command if SkipXml is false.
        /// </summary>
        /// <param name="parent">The parent device that this Extensions object belongs to.</param>
        /// <param name="enqueueCommand">Action to enqueue the command for sending.</param>
        /// <remarks>
        /// This method initializes the PanelsHandler with the provided parent and command enqueue action.
        /// If SkipXml is false, it constructs the XML command string using the xCommand method
        /// and enqueues it for sending.
        /// </remarks>
        /// <exception cref="Exception">Thrown if XML serialization fails.</exception>       

        public void Initialize(CiscoCodec parent, Action<string> enqueueCommand)
        {
            this.parent = parent;

            parent.ProvisioningRoomTypeFeedback.OutputChange += (s, e) => Update(enqueueCommand);

            this.parent.LogDebug("Extensions Initialize, Panels from config length: {count}", Panels.Count);

            if (PanelsHandler == null)
            {
                this.parent.LogDebug("Initializing Panels Handler");
                PanelsHandler = new PanelsHandler(this.parent, this, enqueueCommand, Panels);
            }

            if (SkipXml)
            {
                return;
            }

            var xml = XCommand();

            parent.LogDebug("Sending XML data: {xml}", xml);

            enqueueCommand(xml);
        }

        /// <summary>
        /// Updates the Extensions configuration by sending the XML command if SkipXml is false.
        /// </summary>
        /// <param name="enqueueCommand">Action to enqueue the command for sending.</param>
        public void Update(Action<string> enqueueCommand)
        {
            if (SkipXml)
            {
                return;
            }

            var xml = XCommand();

            parent.LogDebug("Sending XML data: {xml}", xml);

            enqueueCommand(xml);
        }



        /// <summary>
        /// Generates the xCommand string for configuring UI Extensions on the Cisco codec.
        /// </summary>
        /// <returns>The complete xCommand string including ConfigId and XML configuration data.</returns>
        public string XCommand() => $"xCommand UserInterface Extensions Set ConfigId: {ConfigId}{CiscoCodec.Delimiter}{ToXmlString()}{CiscoCodec.Delimiter}.{CiscoCodec.Delimiter}";

        /// <summary>
        /// converts the props on this object with xml attributes to 
        /// an xml string for the xCommand
        /// </summary>
        /// <returns></returns>
        private string ToXmlString()
        {
            try
            {
                if (parent.CurrentProvisioningRoomType == CodecProvisionMode.Classroom && Panels.Find(p => p.PanelId == classroomPanel.PanelId) == null)
                {
                    parent.LogWarning("Provisioning Room Type is Classroom, adding Classroom Panel to UI Extensions");
                    // Check if any existing panel has the same order as the classroom panel
                    var classroomOrder = classroomPanel.Order;
                    var conflictingPanels = Panels.FindAll(p => p.Order >= classroomOrder);

                    if (conflictingPanels.Count > 0)
                    {
                        // Increment the order of all panels with order >= classroom panel order
                        foreach (var panel in conflictingPanels)
                        {
                            panel.Order++;
                        }
                        parent.LogDebug("Adjusted order of {count} existing panels to accommodate Classroom panel", conflictingPanels.Count);
                    }

                    Panels.Add(classroomPanel);
                    parent.LogDebug("Added Classroom Panel to UI Extensions based on Provisioning Room Type");
                }
                else if (parent.CurrentProvisioningRoomType != CodecProvisionMode.Classroom)
                {
                    parent.LogWarning("Provisioning Room Type is not Classroom, removing Classroom Panel from UI Extensions");
                    // Remove Classroom panel if it exists and the provisioning mode is not Classroom
                    var existingClassroomPanel = Panels.Find(p => p.PanelId == classroomPanel.PanelId);
                    if (existingClassroomPanel != null)
                    {
                        Panels.Remove(existingClassroomPanel);
                        parent.LogDebug("Removed Classroom Panel from UI Extensions based on Provisioning Room Type");
                    }
                }
                return XmlConverter.SerializeObject(this);
            }
            catch (Exception ex)
            {
                parent.LogError("XML Command Serialize Failed: {message}", ex.Message);
                parent.LogVerbose(ex, "Exception");
                return "";
            }
        }
    }
}