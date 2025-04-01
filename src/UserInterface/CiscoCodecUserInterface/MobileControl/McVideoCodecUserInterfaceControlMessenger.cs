using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Essentials.AppServer.Messengers;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using System.Security.Cryptography.X509Certificates;

namespace PepperDash.Essentials.Touchpanel
{
    public class McVideoCodecUserInterfaceControlMessenger : MessengerBase
    {
        private readonly IMcCiscoCodecUserInterfaceAppControl _appControl;

        public McVideoCodecUserInterfaceControlMessenger(string key, string messagePath, Device device) : base(key, messagePath, device)
        {
            _appControl = device as IMcCiscoCodecUserInterfaceAppControl;
        }

        protected override void RegisterActions()
        {
            if (_appControl == null)
            {
                Debug.Console(0, this, $"{_device.Key} does not implement ITswAppControl");
                return;
            }

            AddAction($"/closeWebViewController", (id, context) => _appControl.CloseWebViewController());
        }

    }

    //public class VideoCodecUserInterfaceAppStateMessage : DeviceStateMessageBase
    //{
    //    [JsonProperty("appOpen", NullValueHandling = NullValueHandling.Ignore)]
    //    public bool? AppOpen { get; set; }
    //}
}
