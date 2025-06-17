using Crestron.SimplSharp;
using epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceExtensions;
using epi_videoCodec_ciscoExtended.UserInterface.Utilities;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.Devices;
using Serilog.Events;
using System;
using System.Collections.Generic;

namespace epi_videoCodec_ciscoExtended.UserInterface.CiscoCodecUserInterface
{
    public class CiscoCodecUserInterface : ReconfigurableDevice, ICiscoCodecUserInterface
    {
        public CiscoCodec UisCiscoCodec { get; private set; }
        public CiscoCodecUserInterfaceConfig ConfigProps { get; }
        public ICiscoCodecUiExtensionsHandler CiscoCodecUiExtensionsHandler { get; set; }

        public ICiscoCodecUiExtensions UiExtensions { get; private set; }

        public RoomCombiner.IRoomCombinerHandler RoomCombinerHandler { get; private set; }

        public bool EnableLockoutPoll { get; set; } = false;

        public bool LockedOut { get; set; } = false;

        #region ParseConfigProps
        public T ParseConfigProps<T>(DeviceConfig config)
        {
            return JsonConvert.DeserializeObject<T>(config.Properties.ToString());
        }
        #endregion

        #region Custom Activate
        private List<Action> CustomActivateActions = new List<Action>();

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
            RoomCombinerHandler = new RoomCombiner.RoomCombinerHandler(this);
        }


        public CiscoCodecUserInterface(DeviceConfig config) : base(config)
        {
            ConfigProps = ParseConfigProps<CiscoCodecUserInterfaceConfig>(config);
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
            UisCiscoCodec = DeviceManager.GetDeviceForKey(ConfigProps.VideoCodecKey) as CiscoCodec;

            if (UisCiscoCodec == null)
                {
                var msg = $"Video codec UserInterface could not find codec with key '{ConfigProps.VideoCodecKey}'.";
                Debug.LogMessage(new NullReferenceException(msg), msg, this);
                return;
                }

            UiExtensions = ConfigProps.Extensions;
            CiscoCodecUiExtensionsHandler = new UserInterfaceExtensionsHandler(this, UisCiscoCodec.EnqueueCommand);

            UisCiscoCodec.UiExtensions = UiExtensions;
            UisCiscoCodec.CiscoCodecUiExtensionsHandler = CiscoCodecUiExtensionsHandler;

            UisCiscoCodec.IsReadyChange += (s, a) =>
            {
                if (!UisCiscoCodec.IsReady) return;
                var msg = UiExtensions != null ? "Initializing Video Codec UI Extensions" : "No Ui Extensions in config";
                Debug.LogMessage(LogEventLevel.Debug, msg, this);
                UiExtensions.Initialize(this, UisCiscoCodec.EnqueueCommand);
                Debug.LogMessage(LogEventLevel.Debug, "Video Codec UI Extensions Handler Initilizing", this);
            };
            return;
            }
        }
}
