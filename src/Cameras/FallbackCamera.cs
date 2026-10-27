namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras
{
    /// <summary>
    /// Picks the camera id and video source id for the near-end camera the codec creates itself when
    /// none of the cameras it reports matched a camera device.
    /// </summary>
    /// <remarks>Has no Crestron dependencies, so the test project compiles and tests it directly.</remarks>
    public static class FallbackCamera
    {
        /// <summary>
        /// Camera id used when the codec reports no cameras: the integrated camera.
        /// </summary>
        public const uint DefaultCameraId = 1;

        /// <summary>
        /// The first reported camera's number, and its source id falling back to that number. Camera 1
        /// on source 1 when the codec reports no cameras.
        /// </summary>
        /// <param name="reportedCameraNumber">The first camera the codec reports, or null for none.</param>
        /// <param name="reportedSourceId">That camera's detected connector, or 0 when unknown.</param>
        public static (uint CameraId, uint SourceId) Resolve(int? reportedCameraNumber, int reportedSourceId)
        {
            if (reportedCameraNumber == null || reportedCameraNumber.Value <= 0)
                return (DefaultCameraId, DefaultCameraId);

            var cameraId = (uint)reportedCameraNumber.Value;
            var sourceId = reportedSourceId > 0 ? (uint)reportedSourceId : 0u;
            return (cameraId, CameraSourceId.Resolve(sourceId, cameraId));
        }
    }
}
