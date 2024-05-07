using epi_videoCodec_ciscoExtended.UserInterface.CiscoCodecUserInterface;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Devices.Common.VideoCodec.Interfaces;
using System;
using System.Linq;
using Feedback = PepperDash.Essentials.Core.Feedback;

namespace epi_videoCodec_ciscoExtended.UserInterface.CiscoCodecUserInterface.MobileControl
{
    public class MobileControlTouchpanelController : CiscoCodecUserInterface, IMobileControlTouchpanelController
    {
        private readonly CiscoCodecUserInterfaceMobileControlConfig McConfigProps;
        private IMobileControlRoomMessenger _bridge;
        private IMobileControl mc;
        private string _appUrl;

        public StringFeedback AppUrlFeedback { get; private set; }
        private readonly StringFeedback QrCodeUrlFeedback;
        private readonly StringFeedback McServerUrlFeedback;
        private readonly StringFeedback UserCodeFeedback;

        public FeedbackCollection<Feedback> Feedbacks { get; private set; }

        public string DefaultRoomKey => McConfigProps.DefaultRoomKey;

        public bool UseDirectServer => McConfigProps.UseDirectServer;

        bool IMobileControlTouchpanelController.ZoomRoomController => false;

        public MobileControlTouchpanelController(DeviceConfig config) : base(config)
        {
            McConfigProps = ParseConfigProps<CiscoCodecUserInterfaceMobileControlConfig>(config);

            AddPostActivationAction(SubscribeForMobileControlUpdates);

            AppUrlFeedback = new StringFeedback(() => _appUrl);
            QrCodeUrlFeedback = new StringFeedback(() => _bridge?.QrCodeUrl);
            McServerUrlFeedback = new StringFeedback(() => _bridge?.McServerUrl);
            UserCodeFeedback = new StringFeedback(() => _bridge?.UserCode);

            Feedbacks = new FeedbackCollection<Feedback>
            {
                AppUrlFeedback, QrCodeUrlFeedback, McServerUrlFeedback, UserCodeFeedback
            };
        }

        private void SubscribeForMobileControlUpdates()
        {
            foreach (var dev in DeviceManager.AllDevices)
            {
                Debug.LogMessage(Serilog.Events.LogEventLevel.Verbose, $"{dev.Key}:{dev.GetType().Name}", this);
            }

            var mcList = DeviceManager.AllDevices.OfType<IMobileControl>().ToList();

            if (mcList.Count == 0)
            {
                Debug.LogMessage(Serilog.Events.LogEventLevel.Verbose, $"No Mobile Control controller found", this);
                return;
            }

            // use first in list, since there should only be one.
            var mc = mcList[0];

            var bridge = mc.GetRoomMessenger(McConfigProps.DefaultRoomKey);

            if (bridge == null)
            {
                Debug.LogMessage(Serilog.Events.LogEventLevel.Verbose, $"No Mobile Control bridge for {McConfigProps.DefaultRoomKey} found ", this);
                return;
            }

            _bridge = bridge;

            _bridge.UserCodeChanged += UpdateFeedbacks;
            _bridge.AppUrlChanged += (s, a) => { Debug.Console(0, this, "AppURL changed"); UpdateFeedbacks(s, a); };
        }

        public void SetAppUrl(string url)
        {
            _appUrl = url;
            AppUrlFeedback.FireUpdate();
        }

        private void UpdateFeedbacks(object sender, EventArgs args)
        {
            UpdateFeedbacks();
        }

        private void UpdateFeedbacks()
        {
            foreach (var feedback in Feedbacks) { feedback.FireUpdate(); }
        }
    }
}
