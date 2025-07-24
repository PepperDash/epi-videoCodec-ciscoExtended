using System;
using System.Linq;
using PepperDash.Core;
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
                    Debug.LogMessage(LogEventLevel.Debug, "Mc is null", this);
                    return;
                }
                var bridge = mobileControl.GetRoomMessenger(props.DefaultRoomKey);
                if (bridge == null)
                {
                    Debug.LogMessage(LogEventLevel.Debug, "No Mobile Control bridge for {defaultRoomKey} found ", this, props.DefaultRoomKey);
                    return;
                }

                Debug.LogMessage(LogEventLevel.Debug, "Got Bridge: {roomName}", this, bridge.RoomName);

                this.bridge = bridge;

                SetAppUrl(this.bridge.AppUrl);

                this.bridge.UserCodeChanged += UpdateFeedbacks;

                //SetAppUrl here fixing AppUrlFeedback.StringValue null after initial event
                this.bridge.AppUrlChanged += (s, a) =>
                {
                    Debug.LogMessage(LogEventLevel.Information, "AppURL changed", this);

                    UpdateFeedbacks(s, a);

                    SetAppUrl(this.bridge.AppUrl);
                };

                router = new NavigatorLockoutHandler(this, props);

                router.Activate(this);
            }
            catch (Exception e)
            {
                Debug.LogMessage(e, "SubscribeForMobileControlUpdates Error", this);
            }
        }

        public void SetAppUrl(string url)
        {
            try
            {
                Debug.LogMessage(LogEventLevel.Debug, "Setting AppUrl to: {url}", this, url);
                appUrl = url;
                AppUrlFeedback.FireUpdate();
            }
            catch (Exception e)
            {
                Debug.LogMessage(e, "SetAppUrl Error", this);
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
            router.ClearCiscoCodecUiWebViewOsd();
        }

        public void ShowWebViewOsd()
        {
            throw new NotImplementedException();
        }

        public void ShowWebViewOsd(string url)
        {
            throw new NotImplementedException();
        }

        public void ShowWebViewOsd(string url, WebViewDisplayConfig webviewConfig)
        {
            router.SendCiscoCodecUiToWebViewUrl(url, webviewConfig);
        }
    }
}
