using System;


namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
  #region

  public class CodecInfoChangedEventArgs : EventArgs
  {
    public bool MultiSiteOptionIsEnabled { get; set; }
    public string IpAddress { get; set; }
    public string SipPhoneNumber { get; set; }
    public string E164Alias { get; set; }
    public string H323Id { get; set; }
    public string SipUri { get; set; }
    public bool AutoAnswerEnabled { get; set; }
    public string Firmware { get; set; }
    public string SerialNumber { get; set; }

    public eCodecInfoChangeType InfoChangeType { get; private set; }

    public CodecInfoChangedEventArgs()
    {
      InfoChangeType = eCodecInfoChangeType.Unknown;
    }

    public CodecInfoChangedEventArgs(eCodecInfoChangeType changeType)
    {
      InfoChangeType = changeType;
    }
  }

  #endregion
}
