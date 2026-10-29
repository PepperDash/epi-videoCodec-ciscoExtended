using FluentAssertions;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>Binding state of a camera the codec lists: connected but DetectedConnector 0 is unbound (#188).</summary>
public class CameraBindingTests
{
    [Theory]
    [InlineData("True", "7")]
    [InlineData("true", "1")]
    public void Connected_With_A_Connector_Is_Bound(string connected, string connector)
    {
        CameraBinding.Evaluate(connected, connector).Should().Be(CameraBindingState.Bound);
    }

    [Fact]
    public void Connected_With_Connector_Zero_Is_Unbound()
    {
        // The PTZ in slot 2 on site: Connected True, DetectedConnector 0.
        CameraBinding.Evaluate("True", "0").Should().Be(CameraBindingState.Unbound);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-1")]
    public void Connected_Without_A_Usable_Connector_Is_Unknown(string connector)
    {
        // Partial feedback often leaves DetectedConnector out; that must not count as unbound.
        CameraBinding.Evaluate("True", connector).Should().Be(CameraBindingState.Unknown);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("7")]
    [InlineData(null)]
    public void Not_Connected_Is_Disconnected(string connector)
    {
        CameraBinding.Evaluate("False", connector).Should().Be(CameraBindingState.Disconnected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Connected_Not_Reported_Is_Unknown(string connected)
    {
        CameraBinding.Evaluate(connected, "0").Should().Be(CameraBindingState.Unknown);
    }
}
