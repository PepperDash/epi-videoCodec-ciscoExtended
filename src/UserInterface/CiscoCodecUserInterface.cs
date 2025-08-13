using System;
using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Core.Logging;
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
            this.LogDebug("iconHandler.DumpAllPngsToBase64() called.");

            IconHandler.DumpAllPngsToBase64();

            this.LogDebug("Activating Video Codec UI Extensions");
            Parent = DeviceManager.GetDeviceForKey(ConfigProps.VideoCodecKey) as CiscoCodec;

            if (Parent == null)
            {
                this.LogError("Video codec UserInterface could not find codec with key '{videoCodecKey}'.", ConfigProps.VideoCodecKey);
                return;
            }

            UiExtensions = ConfigProps.Extensions;

            UiExtensionsHandler = new ExtensionsHandler(this, Parent.EnqueueCommand);

            Parent.UiExtensions = UiExtensions;

            Parent.UiExtensionsHandler = UiExtensionsHandler;

            return;
        }
    }
}
