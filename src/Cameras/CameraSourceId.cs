namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras
{
    /// <summary>
    /// Works out the video input source id a camera selects with <c>SetMainVideoSource</c>.
    /// </summary>
    /// <remarks>Has no Crestron dependencies, so the test project compiles and tests it directly.</remarks>
    public static class CameraSourceId
    {
        /// <summary>
        /// The configured <c>sourceId</c> when set, otherwise the camera id. Matches the fallback used
        /// for cameras built from the codec's camera info.
        /// </summary>
        public static uint Resolve(uint configuredSourceId, uint cameraId)
        {
            return configuredSourceId > 0 ? configuredSourceId : cameraId;
        }

        /// <summary>
        /// False for 0, which RoomOS rejects with "SourceId Invalid value".
        /// </summary>
        public static bool IsValid(uint sourceId)
        {
            return sourceId > 0;
        }
    }
}
