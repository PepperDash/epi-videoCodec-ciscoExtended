using FluentAssertions;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>Source id for config-defined cameras: an unset sourceId must not become SetMainVideoSource SourceId: 0.</summary>
public class CameraSourceIdTests
{
    [Fact]
    public void Configured_Source_Id_Wins()
    {
        CameraSourceId.Resolve(3, 7).Should().Be(3u);
    }

    [Fact]
    public void Unset_Source_Id_Falls_Back_To_Camera_Id()
    {
        // Laurelhurst: ciscoPtzCameraA has defaultCameraId 7 and no sourceId.
        CameraSourceId.Resolve(0, 7).Should().Be(7u);
    }

    [Fact]
    public void Nothing_Configured_Stays_Invalid()
    {
        var sourceId = CameraSourceId.Resolve(0, 0);

        sourceId.Should().Be(0u);
        CameraSourceId.IsValid(sourceId).Should().BeFalse();
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(7u)]
    public void Non_Zero_Source_Ids_Are_Valid(uint sourceId)
    {
        CameraSourceId.IsValid(sourceId).Should().BeTrue();
    }
}
