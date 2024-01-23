using System;
using System.Collections.Generic;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common.Cameras;

namespace epi_videoCodec_ciscoExtended.V2
{
    // xCommand Camera Ramp CameraId: value Focus: value Pan: value PanSpeed: value Tilt: value TiltSpeed: value Zoom: value ZoomSpeed: value
    internal class CiscoNearEndCamera : CameraBase, IHasCameraPtzControl, IHasCameraFocusControl, IHasCameraPresets
    {
        public int CameraId { get; private set; }
        public int Connector { get; set; }
        public bool IsSpeakerTrack { get; set; }
        public bool IsPresenterTrack { get; set; }

        private readonly CiscoRoomOsDevice parent;

        public CiscoNearEndCamera(string key, string name, int cameraId, CiscoRoomOsDevice parent)
            : base(key, name)
        {
            CameraId = cameraId;
            Presets = new List<CameraPreset>();
            this.parent = parent;
        }

        public void PanLeft()
        {
            var command = string.Format("xCommand Camera Ramp CameraId:{0} Pan: Left PanSpeed: 7", CameraId);
            parent.SendText(command);
        }

        public void PanRight()
        {
            var command = string.Format("xCommand Camera Ramp CameraId:{0} Pan: Right PanSpeed: 7", CameraId);
            parent.SendText(command);
        }

        public void PanStop()
        {
            var command = string.Format("xCommand Camera Ramp CameraId:{0} Pan: Stop", CameraId);
            parent.SendText(command);
        }

        public void TiltUp()
        {
            var command = string.Format("xCommand Camera Ramp CameraId:{0} Tilt: Up TiltSpeed: 7", CameraId);
            parent.SendText(command);
        }

        public void TiltDown()
        {
            var command = string.Format("xCommand Camera Ramp CameraId:{0} Tilt: Down TiltSpeed: 7", CameraId);
            parent.SendText(command);
        }

        public void TiltStop()
        {
            var command = string.Format("xCommand Camera Ramp CameraId:{0} Tilt: Stop", CameraId);
            parent.SendText(command);
        }

        public void ZoomIn()
        {
            var command = string.Format("xCommand Camera Ramp CameraId:{0} Zoom: In ZoomSpeed: 7", CameraId);
            parent.SendText(command);
        }

        public void ZoomOut()
        {
            var command = string.Format("xCommand Camera Ramp CameraId:{0} Zoom: Out ZoomSpeed: 7", CameraId);
            parent.SendText(command);
        }

        public void ZoomStop()
        {
            var command = string.Format("xCommand Camera Ramp CameraId:{0} Zoom: Stop", CameraId);
            parent.SendText(command);
        }

        public void PositionHome()
        {
            var command =
                string.Format("xCommand Camera Preset ActivateDefaultPosition CameraId:{1}", CameraId);

            parent.SendText(command);
        }

        public void FocusNear()
        {
            var command = string.Format("xCommand Camera Ramp CameraId:{0} Focus: Near", CameraId);
            parent.SendText(command);
        }

        public void FocusFar()
        {
            var command = string.Format("xCommand Camera Ramp CameraId:{0} Focus: Far", CameraId);
            parent.SendText(command);
        }

        public void FocusStop()
        {
            var command = string.Format("xCommand Camera Ramp CameraId:{0} Focus: Stop", CameraId);
            parent.SendText(command);
        }

        public void TriggerAutoFocus()
        {
            var command = string.Format("xCommand Camera TriggerAutofocus CameraId: {0}", CameraId);
            parent.SendText(command);
        }

        public void SetCapabilites(string capabilites)
        {
            var c = capabilites.ToLower();

            if (c.Contains("p"))
                Capabilities = Capabilities | eCameraCapabilities.Pan;

            if (c.Contains("t"))
                Capabilities = Capabilities | eCameraCapabilities.Tilt;

            if (c.Contains("z"))
                Capabilities = Capabilities | eCameraCapabilities.Zoom;

            if (c.Contains("f"))
                Capabilities = Capabilities | eCameraCapabilities.Focus;
        }

        public void PresetSelect(int preset)
        {
            if (preset == 0)
                return;

            var command =
                string.Format("xCommand Camera Preset Activate PresetId:{1}", preset);

            parent.SendText(command);
        }

        public void PresetStore(int preset, string description)
        {
            if (preset == 0)
                return;

            var command =
                string.Format("xCommand Camera Preset Store PresetId:{1} Description: {2}", preset, description);

            parent.SendText(command);
        }

        public List<CameraPreset> Presets { get; private set; }
        public event EventHandler<EventArgs> PresetsListHasChanged;
    }
}