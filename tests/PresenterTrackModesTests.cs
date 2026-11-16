using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>
/// The camera manager's presenterTrackMode config value becomes the codec's spelling of the mode for
/// "xCommand Cameras PresenterTrack Set Mode"; anything else is ignored.
/// </summary>
public class PresenterTrackModesTests
{
    [Theory]
    [InlineData("follow", "Follow")]
    [InlineData("Follow", "Follow")]
    [InlineData("FOLLOW", "Follow")]
    [InlineData(" follow ", "Follow")]
    [InlineData("persistent", "Persistent")]
    [InlineData("background", "Background")]
    [InlineData("off", "Off")]
    public void A_Mode_Is_Spelled_As_The_Codec_Expects(string configured, string expected)
    {
        PresenterTrackModes.Normalize(configured).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("on")]
    [InlineData("setup")]
    [InlineData("follow; xCommand SystemUnit Boot")]
    public void Anything_Else_Is_Not_A_Mode(string? configured)
    {
        PresenterTrackModes.Normalize(configured).Should().BeNull();
    }
}
