using epi_videoCodec_ciscoExtended.UserInterface.UserInterfaceExtensions;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Devices.Common.VideoCodec.Interfaces;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace epi_videoCodec_ciscoExtended.UserInterface.CiscoCodecUserInterface
{
    public class CiscoCodecUserInterface : ReconfigurableDevice, ICiscoCodecUserInterface, IReconfigurableDevice
    {
        public CiscoCodec UisCiscoCodec { get; private set; }
        public CiscoCodecUserInterfaceConfig ConfigProps { get; }
        public IVideoCodecUiExtensionsHandler VideoCodecUiExtensionsHandler { get; private set; }

        public ICiscoCodecUiExtensions UiExtensions { get; private set; }

        public T ParseConfigProps<T>(DeviceConfig config)
        {
            return JsonConvert.DeserializeObject<T>(config.Properties.ToString());
        }

        public CiscoCodecUserInterface(DeviceConfig config) : base(config)
        {
            ConfigProps = ParseConfigProps<CiscoCodecUserInterfaceConfig>(config);

        }

        public override bool CustomActivate()
        {
            Debug.LogMessage(LogEventLevel.Debug, "[DEBUG] Activating Video Codec UI Extensions", this);
			UisCiscoCodec = DeviceManager.GetDeviceForKey(ConfigProps.VideoCodecKey) as CiscoCodec;

            if (UisCiscoCodec == null)
            {
                var msg = $"Video codec UserInterface could not find codec with key '{ConfigProps.VideoCodecKey}'.";
                Debug.LogMessage(new NullReferenceException(msg), msg, this);
                return base.CustomActivate();
            }

            UiExtensions = ConfigProps.Extensions;
            VideoCodecUiExtensionsHandler = new UiExtensionsHandler(this, UisCiscoCodec.EnqueueCommand);

            // update the codec props which will be overwritten if they exist from codec config.
            // #TODO Remove codec config later probably not needed with UI device. 
            UisCiscoCodec.UiExtensions = UiExtensions;
            UisCiscoCodec.VideoCodecUiExtensionsHandler = VideoCodecUiExtensionsHandler;

            UisCiscoCodec.IsReadyChange += (s, a) =>
            {
                if (!UisCiscoCodec.IsReady) return;
                var msg = UiExtensions != null ? "[DEBUG] Initializing Video Codec UI Extensions" : "[DEBUG] No Ui Extensions in config";
                Debug.LogMessage(LogEventLevel.Debug, msg, this);
                UiExtensions.Initialize(this, UisCiscoCodec.EnqueueCommand);
                Debug.LogMessage(LogEventLevel.Debug, "[DEBUG] Video Codec UI Extensions Handler Initilizing", this);
            };
            return base.CustomActivate();
        }
    }
}
