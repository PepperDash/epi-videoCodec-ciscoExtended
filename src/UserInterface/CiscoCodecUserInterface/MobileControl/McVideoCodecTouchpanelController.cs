using Crestron.SimplSharpPro.UI;
using epi_videoCodec_ciscoExtended.UserInterface.CiscoCodecUserInterface;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using PepperDash.Essentials.Devices.Common.VideoCodec.Interfaces;
using PepperDash.Essentials.Touchpanel;
using Serilog.Events;
using System;
using System.Linq;
using Feedback = PepperDash.Essentials.Core.Feedback;

namespace epi_videoCodec_ciscoExtended.UserInterface.CiscoCodecUserInterface.MobileControl
{
	public class McVideoCodecTouchpanelController : CiscoCodecUserInterface, IMcCiscoCodecUserInterfaceAppControl, IMobileControlTouchpanelController
	{
		private readonly McVideoCodecUserInterfaceConfig _props;
		private IMobileControlRoomMessenger _bridge;
		private IMobileControl Mc;
		private string _appUrl;

		public StringFeedback AppUrlFeedback { get; private set; }
		private readonly StringFeedback QrCodeUrlFeedback;
		private readonly StringFeedback McServerUrlFeedback;
		private readonly StringFeedback UserCodeFeedback;

		public FeedbackCollection<Feedback> Feedbacks { get; private set; }

		public string DefaultRoomKey => _props.DefaultRoomKey;

		public bool UseDirectServer => _props.UseDirectServer;

		bool IMobileControlTouchpanelController.ZoomRoomController => false;

		//public BoolFeedback WebViewOpenFeedback => throw new NotImplementedException();

		private McVideoCodecUserInterfaceRouter _router;

		public McVideoCodecTouchpanelController(DeviceConfig config) : base(config)
		{
			Debug.LogMessage(LogEventLevel.Debug, "McTouchpanelController Constructor", this);
			_props = ParseConfigProps<McVideoCodecUserInterfaceConfig>(config);

			AddPostActivationAction(PostActivateSubscribeForMobileControlUpdates);

			AppUrlFeedback = new StringFeedback(() => _appUrl);
			QrCodeUrlFeedback = new StringFeedback(() => _bridge?.QrCodeUrl);
			McServerUrlFeedback = new StringFeedback(() => _bridge?.McServerUrl);
			UserCodeFeedback = new StringFeedback(() => _bridge?.UserCode);

			Feedbacks = new FeedbackCollection<Feedback>
			{
				AppUrlFeedback, QrCodeUrlFeedback, McServerUrlFeedback, UserCodeFeedback
			};
		}
		public override bool CustomActivate()
		{
			Debug.LogMessage(LogEventLevel.Debug, "[McTouchpanelController] Activate", this);
			Mc = DeviceManager.AllDevices.OfType<IMobileControl>().FirstOrDefault();

			if (Mc == null)
			{
				return base.CustomActivate();
			}

			var messenger = new McVideoCodecUserInterfaceControlMessenger(string.Format("appControlMessenger-{0}", Key), string.Format("/device/{0}", Key), this);

			Mc.AddDeviceMessenger(messenger);

			//Debug.LogMessage(Serilog.Events.LogEventLevel.Debug, $"mc.ClientAppUrl: {Mc.ClientAppUrl.MaskQParamTokenInUrl()}", this);

			return base.CustomActivate();
		}

		private void PostActivateSubscribeForMobileControlUpdates()
		{
			try
			{

				Debug.LogMessage(LogEventLevel.Debug, "[McTouchpanelController] SubscribeForMobileControlUpdates", this);
				foreach (var dev in DeviceManager.AllDevices)
				{
					Debug.Console(0, this, $"{dev.Key}:{dev.GetType().Name}");
				}

				Debug.LogMessage(LogEventLevel.Debug, $"[McTouchpanelController] GetBridge. DefaultRoomKey: {_props.DefaultRoomKey}", this);
				if (Mc == null)
				{
					Debug.LogMessage(LogEventLevel.Debug, "[McTouchpanelController] Mc is null", this);
					return;
				}
				var bridge = Mc.GetRoomMessenger(_props.DefaultRoomKey);
				Debug.LogMessage(LogEventLevel.Debug, $"[McTouchpanelController] Got Bridge: {bridge.RoomName}", this);
				if (bridge == null)
				{
					Debug.LogMessage(LogEventLevel.Debug, $"[McTouchpanelController] No Mobile Control bridge for {_props.DefaultRoomKey} found ", this);
					return;
				}

				_bridge = bridge;
				Debug.LogMessage(LogEventLevel.Debug, $"[McTouchpanelController] Setting AppUrl", this);

				Debug.LogMessage(LogEventLevel.Debug, $"[McTouchpanelController] Mobile Control Room Bridge Found {_bridge.Key}", this);

				Debug.LogMessage(LogEventLevel.Debug, "[McTouchpanelController] Subscribing to Mobile Control Events: UserCodeChanged", this);
				_bridge.UserCodeChanged += UpdateFeedbacks;

				Debug.LogMessage(LogEventLevel.Debug, "[McTouchpanelController] Subscribing to Mobile Control Events: AppUrlChanged", this);

				//SetAppUrl here fixing AppUrlFeedback.StringValue null after initial event
				_bridge.AppUrlChanged += (s, a) => { Debug.Console(0, this, "[McTouchpanelController] AppURL changed"); UpdateFeedbacks(s, a);
					SetAppUrl(_bridge.AppUrl);
				}; 
				
				Debug.LogMessage(LogEventLevel.Debug, "[McTouchpanelController] Building McVideoCodecUserInterfaceRouter", this);
				_router = new McVideoCodecUserInterfaceRouter(this, _bridge, _props);
				_router.Activate(this);
				Debug.LogMessage(LogEventLevel.Debug, "[McTouchpanelController] SubscribeForMobileControlUpdates success", this);
			}
			catch (Exception e)
			{
				Debug.LogMessage(e, "SubscribeForMobileControlUpdates Error", this);
				Debug.LogMessage(LogEventLevel.Debug, e.StackTrace, this);
			}
		}

		public void SetAppUrl(string url)
		{
			try
			{
				Debug.LogMessage(LogEventLevel.Debug, $"Setting AppUrl to: {url}", this);
				_appUrl = url;
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
			foreach (var feedback in Feedbacks) { feedback.FireUpdate(); }
		}

		public void CloseWebViewController()
		{
			_router.ClearCiscoCodecUiWebViewController();
		}

		public void CloseWebViewOsd()
		{
			_router.ClearCiscoCodecUiWebViewOsd();
		}
	}
}
