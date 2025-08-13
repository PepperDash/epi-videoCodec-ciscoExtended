using System;
using Crestron.SimplSharp;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Interfaces;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// Lightweight Cisco codec implementation specifically designed for Navigator-only scenarios.
    /// This implementation provides only the essential functionality needed for Navigator
    /// operation without the overhead of the full video codec features.
    /// </summary>
    /// <remarks>
    /// This lite version includes only:
    /// - Basic device connectivity and communication monitoring
    /// - Command queuing functionality for UI extensions
    /// - UI extensions support for Navigator integration
    /// - Essential logging and debugging capabilities
    /// 
    /// This implementation does NOT include:
    /// - Call management features
    /// - Camera control
    /// - Directory services
    /// - Codec-specific audio/video controls
    /// - Scheduling and booking integration
    /// - Advanced codec status monitoring
    /// </remarks>
    public class CiscoCodecNavigatorLite : Device, ICiscoCodecBase
    {
        private readonly IBasicCommunication _communication;
        private readonly CiscoCodecConfig _config;
        private bool _isReady = false;

        /// <summary>
        /// UI Extensions configuration for the codec
        /// </summary>
        public UiExtensions UiExtensions { get; set; }

        /// <summary>
        /// UI Extensions handler for managing UI extensions
        /// </summary>
        public ExtensionsHandler UiExtensionsHandler { get; set; }

        /// <summary>
        /// Event fired when the device ready status changes
        /// </summary>
        public event EventHandler<EventArgs> IsReadyChange;

        /// <summary>
        /// Gets whether the device is ready for operation
        /// </summary>
        public bool IsReady
        {
            get { return _isReady; }
            private set
            {
                if (value != _isReady)
                {
                    _isReady = value;
                    this.LogInformation("CiscoCodecNavigatorLite IsReady changed to: {0}", value);
                    IsReadyChange?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the CiscoCodecNavigatorLite class
        /// </summary>
        /// <param name="config">Device configuration</param>
        /// <param name="communication">Communication interface</param>
        public CiscoCodecNavigatorLite(DeviceConfig config, IBasicCommunication communication) : base(config.Key)
        {
            _communication = communication;
            _config = JsonConvert.DeserializeObject<CiscoCodecConfig>(config.Properties.ToString());
            
            // Setup communication event handlers for basic connectivity
            if (_communication != null)
            {
                _communication.TextReceived += Communication_TextReceived;
            }
        }

        /// <summary>
        /// Activates the device and establishes communication
        /// </summary>
        public override bool CustomActivate()
        {
            this.LogInformation("Activating CiscoCodecNavigatorLite - Navigator-only mode");

            try
            {
                if (_communication != null)
                {
                    _communication.Connect();
                    this.LogDebug("Communication connect called");
                    
                    // Set ready status based on connection
                    IsReady = _communication.IsConnected;
                }
                
                return base.CustomActivate();
            }
            catch (Exception ex)
            {
                this.LogError("Error activating CiscoCodecNavigatorLite: {0}", ex.Message);
                this.LogVerbose(ex, "Activation exception");
                return false;
            }
        }

        /// <summary>
        /// Queues a command to be sent to the codec - simplified implementation for Navigator use
        /// </summary>
        /// <param name="command">The command to enqueue</param>
        public void EnqueueCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
                return;

            this.LogVerbose("Sending command: {0}", command);
            
            try
            {
                if (_communication != null && _communication.IsConnected)
                {
                    _communication.SendText(command + '\n');
                    this.LogVerbose("Sent command: {0}", command);
                }
                else
                {
                    this.LogWarning("Cannot send command - communication not connected: {0}", command);
                }
            }
            catch (Exception ex)
            {
                this.LogError("Error sending command '{0}': {1}", command, ex.Message);
                this.LogVerbose(ex, "Send command exception");
            }
        }

        /// <summary>
        /// Handles received text from communication
        /// </summary>
        private void Communication_TextReceived(object sender, GenericCommMethodReceiveTextArgs args)
        {
            this.LogVerbose("Received: {0}", args.Text?.Trim());
            // Minimal processing - just log for debugging Navigator issues
            // Full codec implementation would parse status updates here
        }
    }
}