namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Enums
{
  /// <summary>
  /// Defines the camera tracking capabilities available on a Cisco codec.
  /// </summary>
  public enum eCameraTrackingCapabilities
  {
    /// <summary>
    /// No camera tracking capabilities available
    /// </summary>
    None,
    /// <summary>
    /// SpeakerTrack capability available - automatically tracks active speakers
    /// </summary>
    SpeakerTrack,
    /// <summary>
    /// PresenterTrack capability available - automatically tracks presenters
    /// </summary>
    PresenterTrack,
    /// <summary>
    /// Both SpeakerTrack and PresenterTrack capabilities available
    /// </summary>
    Both
  }
}
