using FluentAssertions;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Enums;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>Camera tracking capabilities from SpeakerTrack/PresenterTrack availability (#117).</summary>
public class CameraTrackingCapabilitiesTests
{
    [Theory]
    [InlineData(false, false, eCameraTrackingCapabilities.None)]
    [InlineData(true, false, eCameraTrackingCapabilities.SpeakerTrack)]
    [InlineData(false, true, eCameraTrackingCapabilities.PresenterTrack)]
    [InlineData(true, true, eCameraTrackingCapabilities.Both)]
    public void Availability_Maps_To_Capabilities(bool speakerTrack, bool presenterTrack, eCameraTrackingCapabilities expected)
    {
        new CameraTrackingCapabilitiesArgs(speakerTrack, presenterTrack).CameraTrackingCapabilites.Should().Be(expected);
    }
}
