using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Devices.Common.Cameras;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras
{
    public class CiscoCamera : CameraBase, IHasCameraPtzControl, IHasCameraFocusControl, IBridgeAdvanced
    {
        /// <summary>
        /// The codec this camera belongs to
        /// </summary>
        [JsonIgnore]
        public CiscoCodec ParentCodec { get; private set; }

        /// <summary>
        /// The ID of the camera on the codec
        /// </summary>
        public uint CameraId { get; private set; }

        private bool maintainConfiguredCameraId = false;

        /// <summary>
        /// Optional property to specify the network switch port the camera is connected to.
        /// This is used by the CameraManager to change port settings when the camera is switched to a different codec.
        /// </summary>
        public string NetworkSwitchPort { get; private set; }

        /// <summary>
        /// Valid range 1-15
        /// </summary>
        protected uint PanSpeed { get; private set; }

        /// <summary>
        /// Valid range 1-15
        /// </summary>
        protected uint TiltSpeed { get; private set; }

        /// <summary>
        /// Valid range 1-15
        /// </summary>
        protected uint ZoomSpeed { get; private set; }

        public uint SourceId { get; private set; }

        public string SerialNumber { get; private set; }

        public string MacAddress { get; private set; }

        private bool isPanning;

        private bool isTilting;

        private bool isZooming;

        private bool isFocusing;

        private bool isMoving
        {
            get
            {
                return isPanning || isTilting || isZooming || isFocusing;

            }
        }

        public CiscoCamera(string key, string name, CiscoCodec codec, uint id)
            : base(key, name)
        {
            // Default to all capabilties
            Capabilities = eCameraCapabilities.Pan | eCameraCapabilities.Tilt | eCameraCapabilities.Zoom | eCameraCapabilities.Focus;

            ParentCodec = codec;

            CameraId = id;
            SourceId = id;

            // Set default speeds
            PanSpeed = 7;
            TiltSpeed = 7;
            ZoomSpeed = 7;

            SetupOutputPort();
        }

        public CiscoCamera(string key, string name, CiscoCodec codec, uint id, uint sourceId)
            : this(key, name, codec, id)
        {
            SourceId = sourceId;
        }


        /// <summary>
        /// Constructor for a camera that is part of a codec with multiple cameras and where camera config may be set by room based on room configuration scenarios
        /// </summary>
        /// <param name="key"></param>
        /// <param name="name"></param>
        /// <param name="props"></param>
        public CiscoCamera(string key, string name, CiscoCodecCameraPropertiesConfig props)
            : base(key, name)
        {
            SerialNumber = props.SerialNumber;
            MacAddress = props.MacAddress;
            NetworkSwitchPort = props.NetworkSwitchPort;
            maintainConfiguredCameraId = props.MaintainConfiguredCameraId ?? false;

            // Default to all capabilties
            Capabilities = eCameraCapabilities.Pan | eCameraCapabilities.Tilt | eCameraCapabilities.Zoom | eCameraCapabilities.Focus;

            CameraId = props.DefaultCameraId;

            SetupOutputPort();

            // add pre activation action to set the codec based on the default parent device key
            AddPreActivationAction(() =>
            {
                var codec = DeviceManager.GetDeviceForKey(props.DefaultParentCodecKey) as CiscoCodec;

                if (codec == null)
                {
                    this.LogError("WARNING: Parent codec with key '{parentCodecKey}' not found for camera '{Key}'", props.DefaultParentCodecKey, Key);
                }
                ParentCodec = codec;
            });
        }

        private void SetupOutputPort()
        {
            OutputPorts.Add(new RoutingOutputPort(RoutingPortNames.AnyOut, eRoutingSignalType.Video, eRoutingPortConnectionType.Hdmi, null, this));
            OutputPorts.Add(new RoutingOutputPort(RoutingPortNames.AnyVideoOut, eRoutingSignalType.Video, eRoutingPortConnectionType.Hdmi, null, this));
        }

        public void SetCameraId(uint id)
        {
            if (!maintainConfiguredCameraId)
            {
                CameraId = id;
            }
            else
            {
                this.LogDebug("Maintaining configured camera ID {CameraId} for camera {Key} as maintainConfiguredCameraId is set to true", CameraId, Key);
            }
        }

        public void SetParentCodec(CiscoCodec codec)
        {
            ParentCodec = codec;
        }

        //  Takes a string from the camera capabilities value and converts from "ptzf" to enum bitmask
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

        #region IHasCameraPtzControl Members

        public void PositionHome()
        {
            // Not supported on Internal Spark Camera


        }

        #endregion

        #region IHasCameraPanControl Members

        public void PanLeft()
        {
            if (!isMoving)
            {
                ParentCodec.EnqueueCommand(string.Format("xCommand Camera Ramp CameraId: {0} Pan: Left PanSpeed: {1}", CameraId, PanSpeed));
                isPanning = true;
            }
        }

        public void PanRight()
        {
            if (!isMoving)
            {
                ParentCodec.EnqueueCommand(string.Format("xCommand Camera Ramp CameraId: {0} Pan: Right PanSpeed: {1}", CameraId, PanSpeed));
                isPanning = true;
            }
        }

        public void PanStop()
        {
            ParentCodec.EnqueueCommand(string.Format("xCommand Camera Ramp CameraId: {0} Pan: Stop", CameraId));
            isPanning = false;
        }

        #endregion



        #region IHasCameraTiltControl Members

        public void TiltDown()
        {
            if (!isMoving)
            {
                ParentCodec.EnqueueCommand(string.Format("xCommand Camera Ramp CameraId: {0} Tilt: Down TiltSpeed: {1}", CameraId, TiltSpeed));
                isTilting = true;
            }
        }

        public void TiltUp()
        {
            if (!isMoving)
            {
                ParentCodec.EnqueueCommand(string.Format("xCommand Camera Ramp CameraId: {0} Tilt: Up TiltSpeed: {1}", CameraId, TiltSpeed));
                isTilting = true;
            }
        }

        public void TiltStop()
        {
            ParentCodec.EnqueueCommand(string.Format("xCommand Camera Ramp CameraId: {0} Tilt: Stop", CameraId));
            isTilting = false;
        }

        #endregion

        #region IHasCameraZoomControl Members

        public void ZoomIn()
        {
            if (!isMoving)
            {
                ParentCodec.EnqueueCommand(string.Format("xCommand Camera Ramp CameraId: {0} Zoom: In ZoomSpeed: {1}", CameraId, ZoomSpeed));
                isZooming = true;
            }
        }

        public void ZoomOut()
        {
            if (!isMoving)
            {
                ParentCodec.EnqueueCommand(string.Format("xCommand Camera Ramp CameraId: {0} Zoom: Out ZoomSpeed: {1}", CameraId, ZoomSpeed));
                isZooming = true;
            }
        }

        public void ZoomStop()
        {
            ParentCodec.EnqueueCommand(string.Format("xCommand Camera Ramp CameraId: {0} Zoom: Stop", CameraId));
            isZooming = false;
        }

        #endregion

        #region IHasCameraFocusControl Members

        public void FocusNear()
        {
            if (!isMoving)
            {
                ParentCodec.EnqueueCommand(string.Format("xCommand Camera Ramp CameraId: {0} Focus: Near", CameraId));
                isFocusing = true;
            }
        }

        public void FocusFar()
        {
            if (!isMoving)
            {
                ParentCodec.EnqueueCommand(string.Format("xCommand Camera Ramp CameraId: {0} Focus: Far", CameraId));
                isFocusing = true;
            }
        }

        public void FocusStop()
        {
            ParentCodec.EnqueueCommand(string.Format("xCommand Camera Ramp CameraId: {0} Focus: Stop", CameraId));
            isFocusing = false;
        }

        public void TriggerAutoFocus()
        {
            ParentCodec.EnqueueCommand(string.Format("xCommand Camera TriggerAutofocus CameraId: {0}", CameraId));
        }

        #endregion

        public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            LinkCameraToApi(this, trilist, joinStart, joinMapKey, bridge);
        }
    }

}