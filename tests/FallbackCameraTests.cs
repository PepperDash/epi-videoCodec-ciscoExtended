using FluentAssertions;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>The near-end camera the codec creates itself must follow the reported camera, not assume camera 1.</summary>
public class FallbackCameraTests
{
    [Fact]
    public void No_Reported_Camera_Is_The_Integrated_Camera_1()
    {
        FallbackCamera.Resolve(null, 0).Should().Be((1u, 1u));
    }

    [Fact]
    public void Single_Network_Camera_Without_A_Connector_Uses_Its_Camera_Number()
    {
        // The codec reported only camera 7, with DetectedConnector 0.
        FallbackCamera.Resolve(7, 0).Should().Be((7u, 7u));
    }

    [Fact]
    public void Detected_Connector_Is_The_Source()
    {
        FallbackCamera.Resolve(7, 8).Should().Be((7u, 8u));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Invalid_Camera_Number_Is_The_Integrated_Camera_1(int cameraNumber)
    {
        FallbackCamera.Resolve(cameraNumber, 0).Should().Be((1u, 1u));
    }

    [Fact]
    public void Negative_Connector_Falls_Back_To_The_Camera_Number()
    {
        FallbackCamera.Resolve(7, -1).Should().Be((7u, 7u));
    }
}
