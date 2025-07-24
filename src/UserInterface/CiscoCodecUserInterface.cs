using System;
using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Config;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.RoomCombiner;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.UserInterfaceExtensions.Icons;
using Serilog.Events;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface
{
    public class CiscoCodecUserInterface : ReconfigurableDevice
    {
        public CiscoCodec Parent { get; private set; }
        public UserInterfaceConfig ConfigProps { get; }
        public ExtensionsHandler UiExtensionsHandler { get; set; }
        public UiExtensions UiExtensions { get; private set; }

        public RoomCombinerHandler RoomCombinerHandler { get; private set; }

        public bool EnableLockoutPoll { get; set; } = false;

        public bool LockedOut { get; set; } = false;

        #region Custom Activate
        private readonly List<Action> CustomActivateActions = new List<Action>();

        public override bool CustomActivate()
        {

            foreach (var action in CustomActivateActions)
            {
                action();
            }
            return base.CustomActivate();
        }

        public void AddCustomActivationAction(Action a)
        {
            CustomActivateActions.Add(a);
        }
        #endregion

        public virtual void BuildRoomCombinerHandler()
        {
            RoomCombinerHandler = new RoomCombinerHandler(this);
        }

        public CiscoCodecUserInterface(DeviceConfig config) : base(config)
        {
            ConfigProps = config.Properties.ToObject<UserInterfaceConfig>();
            EnableLockoutPoll = ConfigProps.EnableLockoutPoll ?? false;
            AddPreActivationAction(PreActivateAction);
            BuildRoomCombinerHandler();
        }

        public void PreActivateAction()
        {
            // Create an instance of IconHandler to call the method  
            Debug.LogMessage(LogEventLevel.Debug, "iconHandler.DumpAllPngsToBase64() called.", this);

            IconHandler.DumpAllPngsToBase64();

            Debug.LogMessage(LogEventLevel.Debug, "Activating Video Codec UI Extensions", this);
            Parent = DeviceManager.GetDeviceForKey(ConfigProps.VideoCodecKey) as CiscoCodec;

            if (Parent == null)
            {
                var msg = $"Video codec UserInterface could not find codec with key '{ConfigProps.VideoCodecKey}'.";
                Debug.LogMessage(new NullReferenceException(msg), msg, this);
                return;
            }

            UiExtensions = ConfigProps.Extensions;

            UiExtensionsHandler = new ExtensionsHandler(this, Parent.EnqueueCommand);

            Parent.UiExtensions = UiExtensions;

            Parent.UiExtensionsHandler = UiExtensionsHandler;

            Parent.IsReadyChange += (s, a) =>
            {
                if (!Parent.IsReady) return;

                var msg = UiExtensions != null ? "Initializing Video Codec UI Extensions" : "No Ui Extensions in config";

                Debug.LogMessage(LogEventLevel.Debug, msg, this);

                UiExtensions.Initialize(this, Parent.EnqueueCommand);

                Debug.LogMessage(LogEventLevel.Debug, "Video Codec UI Extensions Handler Initilizing", this);
            };
            return;
        }
    }
}
