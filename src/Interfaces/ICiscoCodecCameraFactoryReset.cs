using System;

namespace PepperDash.Essentials.Core.DeviceTypeInterfaces
{
    /// <summary>
    /// Arguments for the <see cref="ICiscoCodecCameraFactoryReset.CameraDisconnected"/> event.
    /// Fired when a camera status update with ghost=True is received, meaning the physical
    /// camera is no longer paired with the codec.
    /// </summary>
    public class CameraEventArgs : EventArgs
    {
        /// <summary>Camera ID reported by the codec (1-based)</summary>
        public uint CameraId { get; private set; }

        /// <summary>Serial number of the camera, when available</summary>
        public string SerialNumber { get; private set; }


        /// <summary>Initialises a new instance of <see cref="CameraEventArgs"/>.</summary>
        /// <param name="cameraId">Camera ID as reported by the codec (1-based)</param>
        /// <param name="serialNumber">Serial number of the camera, when available</param>
        public CameraEventArgs(uint cameraId, string serialNumber = null)
        {
            CameraId = cameraId;
            SerialNumber = serialNumber;
        }
    }



    /// <summary>
    /// Interface that Cisco codec devices (epi-videoCodec-ciscoExtended) must implement so that
    /// <see cref="PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras.CameraManager"/> can orchestrate the camera factory-reset
    /// and VLAN-migration sequence.
    /// </summary>
    public interface ICiscoCodecCameraFactoryReset
    {
        /// <summary>
        /// Sends the "xCommand Camera FactoryReset CameraId: &lt;cameraId&gt; Confirm: Yes"
        /// command to the codec.
        /// </summary>
        /// <param name="cameraId">Logical camera ID on this codec (1-based)</param>
        void CameraFactoryReset(uint cameraId);

        /// <summary>
        /// Fired when a camera status feedback update includes ghost=True for the given
        /// camera ID, indicating the camera is no longer paired with this codec.
        /// </summary>
        event EventHandler<CameraEventArgs> CameraDisconnected;

        /// <summary>
        /// Fired when a camera becomes connected (or reconnected) on this codec.
        /// </summary>
        event EventHandler<CameraEventArgs> CameraConnected;

        /// <summary>
        /// Fired when camera configuration feedback updates the AssignedSerialNumber
        /// for a camera slot. A blank SerialNumber value means the assignment is cleared.
        /// </summary>
        event EventHandler<CameraEventArgs> CameraAssignedSerialNumberChanged;

        /// <summary>
        /// The VLAN ID that should be assigned to the network switch port for cameras currently paired with this codec.
        /// </summary>
        uint VLanId { get; }


        /// <summary>
        /// Sets the AssignedSerialNumber configuration on the codec for a specific camera slot.
        /// Equivalent to "xConfiguration Cameras Camera[cameraId] AssignedSerialNumber: &lt;serialNumber&gt;".
        /// Pass an empty string to clear the assignment.
        /// </summary>
        /// <param name="cameraId">Logical camera ID on this codec (1-based)</param>
        /// <param name="serialNumber">Serial number string, or empty string to clear</param>
        void SetCameraAssignedSerialNumber(uint cameraId, string serialNumber);

        /// <summary>
        /// Clears the AssignedSerialNumber configuration on the codec for a specific camera slot.
        /// </summary>
        /// <param name="cameraId"></param>
        void ClearCameraAssignedSerialNumber(uint cameraId);

        /// <summary>
        /// Sets the image flip for a specific camera slot.
        /// Equivalent to "xConfiguration Cameras Camera[<paramref name="cameraId"/>] Flip: On|Off".
        /// Used to apply a physical mounting orientation (e.g. ceiling-mounted cameras are flipped)
        /// after a camera attaches to a codec slot.
        /// </summary>
        /// <param name="cameraId">Logical camera ID (slot) on this codec (1-based)</param>
        /// <param name="flip">True to flip the image (On), false for normal (Off)</param>
        void SetCameraFlip(uint cameraId, bool flip);

        /// <summary>
        /// Sets which connector holds the PresenterTrack camera on this codec.
        /// Equivalent to "xConfiguration Cameras PresenterTrack Connector: <paramref name="connector"/>".
        /// </summary>
        /// <param name="connector">Connector/camera id of the PresenterTrack camera</param>
        void SetPresenterTrackConnector(uint connector);

        /// <summary>
        /// Enables or disables the PresenterTrack feature on this codec.
        /// Equivalent to "xConfiguration Cameras PresenterTrack Enabled: True|False".
        /// Must be True for the configured PresenterTrack connector to be usable.
        /// </summary>
        /// <param name="enabled">True to enable PresenterTrack, false to disable</param>
        void SetPresenterTrackEnabled(bool enabled);

        /// <summary>
        /// Returns the serial number of the camera currently paired with the given camera slot,
        /// or null/empty when not available.
        /// </summary>
        /// <param name="cameraId">Logical camera ID on this codec (1-based)</param>
        /// <returns>Serial number string or null</returns>
        string GetCameraSerialNumber(uint cameraId);

        /// <summary>
        /// Configures a video-input connector to route to the specified camera ID.
        /// Equivalent to "xConfiguration Video Input Connector[<paramref name="videoConnectorId"/>]
        /// CameraControl CameraId: <paramref name="cameraId"/>".
        /// Call this after a camera has been verified on the target codec to ensure the
        /// correct connector-to-camera mapping is in effect.
        /// </summary>
        /// <param name="videoConnectorId">Video input connector ID on this codec (1-based)</param>
        /// <param name="cameraId">Camera ID to assign to the connector</param>
        void SetInputCameraId(uint videoConnectorId, uint cameraId);
    }
}
