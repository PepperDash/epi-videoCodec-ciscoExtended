using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Devices.Common.VideoCodec.Interfaces;
using PepperDash.Essentials.Devices.Common.VideoCodec;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PepperDash.Core;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.CiscoCodecUserInterface.RoomCombiner
{
	public interface IRoomCombinerHandler
	{
		EssentialsRoomCombiner EssentialsRoomCombiner { get; }
	}

	public class RoomCombinerHandler: IRoomCombinerHandler
	{
        public EssentialsRoomCombiner EssentialsRoomCombiner { get; private set; }

        public RoomCombinerHandler(ICiscoCodecUserInterface ui)
        {
            ui.AddCustomActivationAction(() => GetDeviceForType(ui));
        }

        private void GetDeviceForType(ICiscoCodecUserInterface ui)
        {
            //company room null check
            if (ui == null)
            {
                Debug.LogMessage(LogEventLevel.Debug, $"[Error]: {ui.Key} is not a companyRoom", ui);
                return;
            }
            try
            {
                Debug.LogMessage(
                    LogEventLevel.Debug,
                    "Setting up RoomCombinerHandler for {0}",
                    ui,
                    ui.Key
                );
                Debug.LogMessage(
                    LogEventLevel.Debug,
                    "Get Interface: IEssentialsRoomCombiner",
                    ui
                );

                var combiners = DeviceManager.AllDevices.OfType<EssentialsRoomCombiner>().ToList();

                if (combiners == null)
                {
                    Debug.LogMessage(LogEventLevel.Debug, $"[Warning]: {ui.Key} could not find RoomCombiner", ui);
                    return;
                }

                if (combiners.Count == 0)
                {
                    Debug.LogMessage(LogEventLevel.Debug, $"[Warning]: {ui.Key} could not find RoomCombiner", ui);
                    return;
                }
                if (combiners.Count > 1)
                {
                    Debug.LogMessage(LogEventLevel.Debug, $"[Warning]: {ui.Key} found more than one RoomCombiner", ui);
                    return;
                }

                EssentialsRoomCombiner = combiners[0];

                Debug.LogMessage(LogEventLevel.Debug, "RoomCombinerHandler setup for {0}", ui, ui.Key);
            }
            catch (Exception e)
            {
                Debug.LogMessage(
                    e,
                    $"[ERROR] setting up RoomCombinerHandler for {ui.Key}",
                    ui
                );
            }
        }
    }
}
