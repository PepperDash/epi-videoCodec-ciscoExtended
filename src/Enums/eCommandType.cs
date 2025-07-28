namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Enums
{
  /// <summary>
  /// Defines the types of commands that can be sent to a Cisco codec.
  /// </summary>
  internal enum eCommandType
  {
    /// <summary>
    /// Command to start a session
    /// </summary>
    SessionStart,
    /// <summary>
    /// Command to end a session
    /// </summary>
    SessionEnd,
    /// <summary>
    /// Command to execute a specific action or operation
    /// </summary>
    Command,
    /// <summary>
    /// Command to get the status of the codec or its components
    /// </summary>
    GetStatus,
    /// <summary>
    /// Command to get the configuration of the codec
    /// </summary>
    GetConfiguration
  };

}
