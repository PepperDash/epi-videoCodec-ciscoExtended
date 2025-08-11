using System;
using System.Linq;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Config;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.WebView;
using Serilog.Events;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Navigator
{
    public class NavigatorController : CiscoCodecUserInterface, IMobileControlTouchpanelController
    {
        private readonly NavigatorConfig props;
        private IMobileControlRoomMessenger bridge;
        private IMobileControl mobileControl;
        private string appUrl;

        public StringFeedback AppUrlFeedback { get; private set; }

        public string DefaultRoomKey => props.DefaultRoomKey;

        public bool UseDirectServer => props.UseDirectServer;

        bool IMobileControlTouchpanelController.ZoomRoomController => false;

        //public BoolFeedback WebViewOpenFeedback => throw new NotImplementedException();

        private NavigatorLockoutHandler router;

        public NavigatorController(DeviceConfig config) : base(config)
        {
            props = config.Properties.ToObject<NavigatorConfig>();

            AddPostActivationAction(SubscribeForMobileControlUpdates);

            AppUrlFeedback = new StringFeedback("appUrl", () => appUrl);
        }

        public override bool CustomActivate()
        {
            mobileControl = DeviceManager.AllDevices.OfType<IMobileControl>().FirstOrDefault();

            if (mobileControl == null)
            {
                return base.CustomActivate();
            }

            var messenger = new NavigatorMessenger($"appControlMessenger-{Key}", $"/device/{Key}", this);

            mobileControl.AddDeviceMessenger(messenger);

            return base.CustomActivate();
        }

        private void SubscribeForMobileControlUpdates()
        {
            try
            {
                if (mobileControl == null)
                {
                    this.LogDebug("Mc is null");
                    return;
                }
                var bridge = mobileControl.GetRoomMessenger(props.DefaultRoomKey);
                if (bridge == null)
                {
                    this.LogDebug("No Mobile Control bridge for {defaultRoomKey} found ", props.DefaultRoomKey);
                    return;
                }

                this.LogDebug("Got Bridge: {roomName}", bridge.RoomName);

                this.bridge = bridge;

                SetAppUrl(this.bridge.AppUrl);

                this.bridge.UserCodeChanged += UpdateFeedbacks;

                //SetAppUrl here fixing AppUrlFeedback.StringValue null after initial event
                this.bridge.AppUrlChanged += (s, a) =>
                {
                    this.LogInformation("AppURL changed");

                    UpdateFeedbacks(s, a);

                    SetAppUrl(this.bridge.AppUrl);
                };

                router = new NavigatorLockoutHandler(this, props);

                router.Activate(this);
            }
            catch (Exception e)
            {
                this.LogError("SubscribeForMobileControlUpdates Error: {message}", e.Message);
                this.LogVerbose(e, "Exception");
            }
        }

        public void SetAppUrl(string url)
        {
            try
            {
                this.LogDebug("Setting AppUrl to: {url}", url);
                appUrl = url;
                AppUrlFeedback.FireUpdate();
            }
            catch (Exception e)
            {
                this.LogError("SetAppUrl Error: {message}", e.Message);
                this.LogVerbose(e, "Exception");
            }
        }

        private void UpdateFeedbacks(object sender, EventArgs args)
        {
            UpdateFeedbacks();
        }

        private void UpdateFeedbacks()
        {
            AppUrlFeedback.FireUpdate();
        }

        public void CloseWebViewController()
        {
            router.ClearWebView();
        }

        public void CloseWebViewOsd()
        {
            router.ClearWebViewOsd();
        }

        public void ShowWebViewOsd(string url, WebViewDisplayConfig webviewConfig)
        {
            router.SendWebViewUrl(url, webviewConfig);
        }
    }
}
