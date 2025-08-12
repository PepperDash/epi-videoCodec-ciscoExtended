using System;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Enums;


namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
  #region

  public class CameraTrackingCapabilitiesArgs : EventArgs
  {
    public eCameraTrackingCapabilities CameraTrackingCapabilites { get; set; }

    public CameraTrackingCapabilitiesArgs(bool speakerTrack, bool presenterTrack)
    {
      CameraTrackingCapabilites = SetCameraTrackingCapabilities(speakerTrack, presenterTrack);
    }

    public CameraTrackingCapabilitiesArgs(Func<bool> speakerTrack, Func<bool> presenterTrack)
    {
      CameraTrackingCapabilites = SetCameraTrackingCapabilities(
        speakerTrack(),
        presenterTrack()
      );
    }

    private eCameraTrackingCapabilities SetCameraTrackingCapabilities(
      bool speakerTrack,
      bool presenterTrack
    )
    {
      var trackingType = eCameraTrackingCapabilities.None;

      if (speakerTrack && presenterTrack)
      {
        trackingType = eCameraTrackingCapabilities.Both;
        return trackingType;
      }
      if (!speakerTrack && presenterTrack)
      {
        trackingType = eCameraTrackingCapabilities.PresenterTrack;
        return trackingType;
      }
      if (speakerTrack && !presenterTrack)
      {
        trackingType = eCameraTrackingCapabilities.SpeakerTrack;
        return trackingType;
      }
      return trackingType;
    }
  }

  #endregion
}
