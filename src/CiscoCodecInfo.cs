using PepperDash.Essentials.Devices.Common.Codec;


namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
  #region

  /// <summary>
  /// Provides device information and capabilities for a Cisco codec, extending the base VideoCodecInfo class.
  /// This class contains codec-specific information such as multi-site capabilities, IP address, and other device details.
  /// </summary>
  public class CiscoCodecInfo : VideoCodecInfo
  {
    private readonly CiscoCodec _codec;

    private bool _multiSiteOptionIsEnabled;

    /// <inheritdoc />
    public override bool MultiSiteOptionIsEnabled
    {
      get { return _multiSiteOptionIsEnabled; }
    }

    private string _ipAddress;

    /// <inheritdoc />
    public override string IpAddress
    {
      get { return _ipAddress; }
    }

    private string _sipPhoneNumber;

    /// <inheritdoc />
    public override string SipPhoneNumber
    {
      get { return _sipPhoneNumber; }
    }

    private string _e164Alias;

    /// <inheritdoc />
    public override string E164Alias
    {
      get { return _e164Alias; }
    }

    private string _h323Id;

    public override string H323Id
    {
      get { return _h323Id; }
    }

    private string _sipUri;

    public override string SipUri
    {
      get { return _sipUri; }
    }

    private bool _autoAnswerEnabled;

    public override bool AutoAnswerEnabled
    {
      get { return _autoAnswerEnabled; }
    }

    public CiscoCodecInfo(CiscoCodec codec)
    {
      _codec = codec;
      _codec.CodecInfoChanged += (sender, args) =>
      {
        if (args.InfoChangeType == eCodecInfoChangeType.Unknown)
          return;
        switch (args.InfoChangeType)
        {
          case eCodecInfoChangeType.Network:
            _ipAddress = args.IpAddress;
            break;
          case eCodecInfoChangeType.Sip:
            _sipPhoneNumber = args.SipPhoneNumber;
            _sipUri = args.SipUri;
            break;
          case eCodecInfoChangeType.H323:
            _h323Id = args.H323Id;
            _e164Alias = args.E164Alias;
            break;
          case eCodecInfoChangeType.Multisite:
            _multiSiteOptionIsEnabled = args.MultiSiteOptionIsEnabled;
            break;
          case eCodecInfoChangeType.AutoAnswer:
            _autoAnswerEnabled = args.AutoAnswerEnabled;
            break;
        }
      };
    }
  }

  #endregion
}
