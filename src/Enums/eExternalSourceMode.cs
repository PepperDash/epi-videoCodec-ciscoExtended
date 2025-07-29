namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Enums
{
  /// <summary>
  /// Defines the operational modes of external sources connected to a Cisco codec.
  /// </summary>
  public enum eExternalSourceMode
  {
    /// <summary>
    /// External source is ready and available for use
    /// </summary>
    Ready,
    /// <summary>
    /// External source is not ready for use
    /// </summary>
    NotReady,
    /// <summary>
    /// External source is hidden from the user interface
    /// </summary>
    Hidden,
    /// <summary>
    /// External source is in an error state
    /// </summary>
    Error
  }
}
