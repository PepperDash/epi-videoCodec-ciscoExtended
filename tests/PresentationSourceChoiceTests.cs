using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>
/// sharingStart presents the selected source, else sharing.defaultPresentationSource, so it works at boot and
/// after a stop without a route first. A camera connector is never presented.
/// </summary>
public class PresentationSourceChoiceTests
{
    private static bool NoCameras(int connector) => false;

    [Fact]
    public void Selected_source_wins_over_the_default()
    {
        PresentationSourceChoice.Resolve(selected: 3, configuredDefault: 2, NoCameras).Should().Be(3);
    }

    [Fact]
    public void Default_is_used_when_nothing_is_selected()
    {
        PresentationSourceChoice.Resolve(selected: 0, configuredDefault: 2, NoCameras).Should().Be(2);
    }

    [Fact]
    public void Nothing_to_present_without_a_selection_or_a_default()
    {
        PresentationSourceChoice.Resolve(selected: 0, configuredDefault: 0, NoCameras)
            .Should().Be(0, "sharingStart then does nothing, as before");
    }

    [Fact]
    public void A_default_that_is_a_camera_connector_is_not_presented()
    {
        PresentationSourceChoice.Resolve(selected: 0, configuredDefault: 1, connector => connector == 1)
            .Should().Be(0);
    }

    [Fact]
    public void A_negative_default_counts_as_none()
    {
        PresentationSourceChoice.Resolve(selected: 0, configuredDefault: -1, NoCameras).Should().Be(0);
    }
}
