using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Devices.Common.VideoCodec;

namespace epi_videoCodec_ciscoExtended.V2
{
    public class CiscoRoomPresets : CiscoRoomOsFeature, IHasCodecRoomPresets, IHasPolls, IHasEventSubscriptions
    {
        private readonly CiscoRoomOsDevice parent;

        public CiscoRoomPresets(CiscoRoomOsDevice parent) : base(parent.Key + "-presets")
        {
            this.parent = parent;
            Polls = new List<string>
            {
                "xStatus RoomPreset"
            };

            Subscriptions = new List<string>
            {
                "Status/RoomPreset"
            };

            // todo: maybe we need to parse this out someday
            NearEndPresets = new List<CodecRoomPreset>();
            FarEndRoomPresets = new List<CodecRoomPreset>();
        }

        public void CodecRoomPresetSelect(int preset)
        {
            if (preset == 0)
                return;

            var command = "xCommand RoomPreset Activate PresetId:" + preset;
            parent.SendText(command);
        }

        public void CodecRoomPresetStore(int preset, string description)
        {
            if (preset == 0)
                return;

            var command = "xCommand RoomPreset Store PresetId:" + preset + " Description:" + description;
            parent.SendText(command);
        }

        public void SelectFarEndPreset(int preset)
        {
            if (preset == 0)
                return;

            // xCommand Call FarEndControl RoomPreset Activate CallId: value ParticipantId: value PresetId: value
            var activeCall = parent.CallStatus.GetActiveCallId();

            var command =
                string.Format(
                    "xCommand Call FarEndControl RoomPreset Activate CallId: {0} PresetId: {1}", activeCall, preset);

            parent.SendText(command);

        }

        public List<CodecRoomPreset> NearEndPresets { get; private set; }
        public List<CodecRoomPreset> FarEndRoomPresets { get; private set; }

        public event EventHandler<EventArgs> CodecRoomPresetsListHasChanged;

        public IEnumerable<string> Polls { get; private set; }
        public IEnumerable<string> Subscriptions { get; private set; }
    }
}