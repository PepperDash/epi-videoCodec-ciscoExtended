using System;
using System.Collections.Generic;
using PepperDash.Essentials.Devices.Common.Cameras;

namespace epi_videoCodec_ciscoExtended.V2
{
    internal class CiscoFarEndCamera : CameraBase, IHasCameraPtzControl, IAmFarEndCamera, IHasCameraPresets
    {
        private readonly CiscoRoomOsDevice parent;

        public CiscoFarEndCamera(string key, string name, CiscoRoomOsDevice parent)
            : base(key, name)
        {
            this.parent = parent;
            Presets = new List<CameraPreset>();
        }

        public void PanLeft()
        {
            var command = string.Format("xCommand Call FarEndControl Camera Move Value: Left CallId: {0}", parent.CallStatus.GetActiveCallId());
            parent.SendText(command);
        }

        public void PanRight()
        {
            var command = string.Format("xCommand Call FarEndControl Camera Move Value: Right CallId: {0}", parent.CallStatus.GetActiveCallId());
            parent.SendText(command);
        }

        public void PanStop()
        {
            var command = string.Format("xCommand Call FarEndControl Camera Stop CallId: {0}", parent.CallStatus.GetActiveCallId());
            parent.SendText(command);
        }

        public void TiltUp()
        {
            var command = string.Format("xCommand Call FarEndControl Camera Move Value: Up CallId: {0}", parent.CallStatus.GetActiveCallId());
            parent.SendText(command);
        }

        public void TiltDown()
        {
            var command = string.Format("xCommand Call FarEndControl Camera Move Value: Down CallId: {0}", parent.CallStatus.GetActiveCallId());
            parent.SendText(command);
        }

        public void TiltStop()
        {
            //"xCommand Call FarEndControl Camera Stop CallId: {0}"
            var command = string.Format("xCommand Call FarEndControl Camera Stop CallId: {0}", parent.CallStatus.GetActiveCallId());
            parent.SendText(command);
        }

        public void ZoomIn()
        {
            var command = string.Format("xCommand Call FarEndControl Camera Move Value: ZoomIn CallId: {0}", parent.CallStatus.GetActiveCallId());
            parent.SendText(command);
        }

        public void ZoomOut()
        {
            var command = string.Format("xCommand Call FarEndControl Camera Move Value: ZoomOut CallId: {0}", parent.CallStatus.GetActiveCallId());
            parent.SendText(command);
        }

        public void ZoomStop()
        {
            var command = string.Format("xCommand Call FarEndControl Camera Stop CallId: {0}", parent.CallStatus.GetActiveCallId());
            parent.SendText(command);
        }

        public void PositionHome()
        {

        }

        public void PresetSelect(int preset)
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

        public void PresetStore(int preset, string description)
        {
            if (preset == 0)
                return;

            // xCommand Call FarEndControl RoomPreset Activate CallId: value ParticipantId: value PresetId: value
            var activeCall = parent.CallStatus.GetActiveCallId();

            var command =
                string.Format(
                    "xCommand Call FarEndControl RoomPreset Store CallId: {0} PresetId: {1}", activeCall, preset);

            parent.SendText(command);
        }

        public List<CameraPreset> Presets { get; private set; }
        public event EventHandler<EventArgs> PresetsListHasChanged;
    }
}