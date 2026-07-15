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

        /// <summary>
        /// The camera ID defined in config (DefaultCameraId). Unlike <see cref="CameraId"/>, this value
        /// never changes after construction and represents the slot the camera is intended to occupy
        /// on its codec. Use this when assigning the camera's serial number to a codec slot
        /// (e.g. after migration); use <see cref="CameraId"/> when targeting the camera's current
        /// live slot (e.g. for factory reset on the source codec).
        /// </summary>
        public uint DefaultCameraId { get; private set; }

        /// <summary>
        /// The maintain-configured-camera-id value as declared in config. Immutable after
        /// construction. <see cref="effectiveMaintain"/> is what the self-heal logic actually reads;
        /// it starts equal to this value and is temporarily forced on while a scenario pins an
        /// explicit camera id, then restored to this baseline when no scenario id applies.
        /// </summary>
        private readonly bool configMaintainCameraId = false;

        /// <summary>
        /// Whether this camera maintains its configured camera id (slot) as declared in config.
        /// Read by the CameraManager's effective-id fallback. Reflects the immutable config value;
        /// per-scenario pinning is handled separately via <see cref="SetScenarioCameraId"/>.
        /// </summary>
        public bool MaintainConfiguredCameraId => configMaintainCameraId;

        /// <summary>
        /// The live maintain flag used by <see cref="SetCameraId"/> self-heal. Equals
        /// <see cref="configMaintainCameraId"/> unless a scenario is currently pinning an explicit
        /// camera id (in which case it is forced true so the manager-chosen slot is enforced).
        /// </summary>
        private bool effectiveMaintain = false;

        /// <summary>
        /// The <see cref="SourceId"/> captured at construction. Restored when a scenario no longer
        /// pins an explicit camera id, so a prior scenario's mirrored source id never leaks forward.
        /// </summary>
        private uint baselineSourceId;

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
            DefaultCameraId = id;
            SourceId = id;
            baselineSourceId = id;

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
            baselineSourceId = sourceId;
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
            configMaintainCameraId = props.MaintainConfiguredCameraId ?? false;
            effectiveMaintain = configMaintainCameraId;

            // Default to all capabilties
            Capabilities = eCameraCapabilities.Pan | eCameraCapabilities.Tilt | eCameraCapabilities.Zoom | eCameraCapabilities.Focus;

            CameraId = props.DefaultCameraId;
            DefaultCameraId = props.DefaultCameraId;
            baselineSourceId = SourceId;

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
            if (!effectiveMaintain)
            {
                CameraId = id;
                return;
            }

            if (id == CameraId)
            {
                this.LogDebug("Maintaining configured camera ID {CameraId} for camera {Key} as maintainConfiguredCameraId is set to true", CameraId, Key);
                return;
            }

            // Codec has the camera at a slot other than the configured DefaultCameraId.
            // With maintainConfiguredCameraId enabled, clear any stale AssignedSerialNumber at the
            // current slot (so a persistent assignment there doesn't conflict) and push the
            // AssignedSerialNumber for the configured slot so the codec moves the camera.
            // Common cases:
            //   - codec auto-paired the camera at startup with no assignment (clear is a no-op,
            //     set moves the camera)
            //   - prior session left an AssignedSerialNumber for this serial at a different slot
            //     (clear removes that binding, set creates the correct one)
            this.LogDebug("Camera {Key}: codec reports camera at slot {actualSlot} but config requires slot {configuredSlot}. Clearing slot {actualSlot} and pushing AssignedSerialNumber for slot {configuredSlot}.", Key, id, CameraId);
            if (ParentCodec == null)
            {
                this.LogWarning("Camera {Key}: cannot enforce configured slot {configuredSlot} — ParentCodec is null", Key, CameraId);
                return;
            }
            if (string.IsNullOrEmpty(SerialNumber))
            {
                this.LogWarning("Camera {Key}: cannot enforce configured slot {configuredSlot} — SerialNumber is null/empty", Key, CameraId);
                return;
            }
            ParentCodec.ClearCameraAssignedSerialNumber(id);
            ParentCodec.SetCameraAssignedSerialNumber(CameraId, SerialNumber);
        }

        /// <summary>
        /// Applies (or clears) a per-scenario camera id override. Called by the CameraManager on
        /// every scenario apply so state never leaks between scenarios:
        /// <list type="bullet">
        /// <item>When <paramref name="id"/> has a value, the camera is pinned to that slot
        /// (<see cref="CameraId"/> = id), self-heal enforcement is forced on, and
        /// <see cref="SourceId"/> is mirrored to the same id.</item>
        /// <item>When <paramref name="id"/> is null, the camera is reset to its configured baseline
        /// (<see cref="CameraId"/> = <see cref="DefaultCameraId"/>, maintain flag restored to the
        /// config value, and <see cref="SourceId"/> restored to its construction-time value). This
        /// makes a no-id scenario behave byte-for-byte like today.</item>
        /// </list>
        /// </summary>
        /// <param name="id">Explicit scenario camera id, or null to use the configured default.</param>
        public void SetScenarioCameraId(uint? id)
        {
            if (id.HasValue)
            {
                if (CameraId != id.Value)
                {
                    this.LogDebug("Camera {Key}: scenario pins camera id {scenarioId} (default {defaultId})", Key, id.Value, DefaultCameraId);
                }
                CameraId = id.Value;
                effectiveMaintain = true;
                SourceId = id.Value;
            }
            else
            {
                if (CameraId != DefaultCameraId)
                {
                    this.LogDebug("Camera {Key}: scenario has no camera id, resetting to default {defaultId}", Key, DefaultCameraId);
                }
                CameraId = DefaultCameraId;
                effectiveMaintain = configMaintainCameraId;
                SourceId = baselineSourceId;
            }
        }

        /// <summary>
        /// Sets the camera's video input source id (the codec video-input connector used by
        /// SetMainVideoSource). Set-only; never used to determine camera placement.
        /// </summary>
        public void SetSourceId(uint sourceId)
        {
            SourceId = sourceId;
        }

        public void SetParentCodec(CiscoCodec codec)
        {
            ParentCodec = codec;
        }

        /// <summary>
        /// True only when the codec's live <c>xStatus Cameras Camera Connected</c> value reports this
        /// camera as connected. Presence of a serial number in a codec's camera list is NOT sufficient
        /// to consider a camera reachable; callers that need to confirm a camera is actually reachable
        /// (e.g. before issuing a migration/factory reset) must check this flag.
        /// </summary>
        [JsonIgnore]
        public bool IsOnline { get; private set; }

        /// <summary>
        /// Updates the live online status of the camera as reported by the codec's Connected status.
        /// </summary>
        public void SetOnlineStatus(bool isOnline)
        {
            IsOnline = isOnline;
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