using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>
/// The Navigator lists its Mobile Control app URL ("appUrl") through Essentials' IHasFeedback, so a room plugin can
/// read it (to open Mobile Control pages on this Navigator) without referencing this plugin.
/// </summary>
public class NavigatorAppUrlFeedbackTests
{
    [Fact]
    public void NavigatorController_implements_IHasFeedback()
    {
        var navigator = AssemblyFixture.PluginAssembly.GetTypes().Single(t => t.Name == "NavigatorController");

        navigator.GetInterfaces().Select(i => i.FullName)
            .Should().Contain("PepperDash.Essentials.Core.IHasFeedback");
    }

    [Fact]
    public void NavigatorController_lists_the_appUrl_feedback()
    {
        var source = AssemblyFixture.FindSourceForClass("NavigatorController");

        source.Should().NotBeNull();
        source!.Should().Contain("new StringFeedback(\"appUrl\"", "other plugins look the feedback up by this key");
        source.Should().Contain("new FeedbackCollection<Feedback> { AppUrlFeedback }",
            "the app URL feedback must be in the IHasFeedback list");
    }
}
