using System;


namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Enums
{

  [Flags]
  public enum MediaChannelStatus
  {
    Unknown = 0,
    None = 1,
    Outgoing = 2,
    Incoming = 4,
    Video = 8,
    Audio = 16,
    Main = 32,
    Presentation = 64,
    OutgoingPresentation = 66,
    IncomingPresentation = 68
  }
}
