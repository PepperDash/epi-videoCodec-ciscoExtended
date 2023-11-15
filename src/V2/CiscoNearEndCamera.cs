using System;
using PepperDash.Essentials.Devices.Common.Cameras;

namespace PDT.Plugins.Cisco.RoomOs.V2
{
    internal class CiscoNearEndCamera : CameraBase, IHasCameraPtzControl, IAmFarEndCamera
    {
        public int CameraId { get; private set; }

        private readonly CiscoRoomOsDevice parent;

        public CiscoNearEndCamera(string key, string name, int cameraId, CiscoRoomOsDevice parent)
            : base(key, name)
        {
            CameraId = cameraId;
            this.parent = parent;
        }

        public void PanLeft()
        {
            throw new NotImplementedException();
        }

        public void PanRight()
        {
            throw new NotImplementedException();
        }

        public void PanStop()
        {
            throw new NotImplementedException();
        }

        public void TiltDown()
        {
            throw new NotImplementedException();
        }

        public void TiltUp()
        {
            throw new NotImplementedException();
        }

        public void TiltStop()
        {
            throw new NotImplementedException();
        }

        public void ZoomIn()
        {
            throw new NotImplementedException();
        }

        public void ZoomOut()
        {
            throw new NotImplementedException();
        }

        public void ZoomStop()
        {
            throw new NotImplementedException();
        }

        public void PositionHome()
        {
            throw new NotImplementedException();
        }
    }
}