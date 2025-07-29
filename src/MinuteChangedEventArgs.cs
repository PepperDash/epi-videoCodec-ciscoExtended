using System;


namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
  #region

  public class MinuteChangedEventArgs : EventArgs
  {
    public DateTime EventTime { get; private set; }

    public MinuteChangedEventArgs(DateTime eventTime)
    {
      EventTime = eventTime;
    }

    public MinuteChangedEventArgs()
    {
      EventTime = DateTime.Now;
    }
  }

  #endregion
}
