using System;
using System.Linq;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using Serilog.Events;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.RoomCombiner
{
    public class RoomCombinerHandler
    {
        public EssentialsRoomCombiner EssentialsRoomCombiner { get; private set; }

        public RoomCombinerHandler(CiscoCodecUserInterface ui)
        {
            ui.AddCustomActivationAction(() => GetDeviceForType(ui));
        }

        private void GetDeviceForType(CiscoCodecUserInterface ui)
        {
            //company room null check
            if (ui == null)
            {
                Debug.LogMessage(LogEventLevel.Debug, $"[Error]: {ui.Key} is not a companyRoom", ui);
                return;
            }
            try
            {
                var combiners = DeviceManager.AllDevices.OfType<EssentialsRoomCombiner>().ToList();

                if (combiners == null || combiners.Count == 0)
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
