using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>
/// The codec exposes its self-view (PIP) position through Essentials' IHasSelfviewPosition and a Mobile Control
/// messenger on /device/{key}: /selfviewPipPositionToggle and the current position in selfviewPipPosition.
/// </summary>
public class SelfviewPositionTests
{
    [Fact]
    public void CiscoCodec_implements_IHasSelfviewPosition()
    {
        var codec = AssemblyFixture.PluginAssembly.GetTypes().Single(t => t.Name == "CiscoCodec");

        codec.GetInterfaces().Select(i => i.FullName)
            .Should().Contain("PepperDash.Essentials.Core.DeviceTypeInterfaces.IHasSelfviewPosition");
    }

    [Fact]
    public void The_codec_registers_the_selfview_position_messenger_on_its_device_path()
    {
        var source = CiscoCodecSource();

        source.Should().Contain("new IHasSelfviewPositionMessenger($\"selfviewPosition-{Key}\", $\"/device/{Key}\", this)");
    }

    [Fact]
    public void The_messenger_offers_the_toggle_and_reports_the_position()
    {
        var source = AssemblyFixture.FindSourceForClass("IHasSelfviewPositionMessenger");

        source.Should().NotBeNull();
        source!.Should().Contain("AddAction(\"/selfviewPipPositionToggle\"");
        source.Should().Contain("AddAction(\"/fullStatus\"");
        source.Should().Contain("[JsonProperty(\"selfviewPipPosition\"");
    }

    [Fact]
    public void The_position_feedback_is_updated_when_the_codec_reports_a_position()
    {
        var source = CiscoCodecSource();
        var compute = source[source.IndexOf("private void ComputeSelfviewPipStatus()", StringComparison.Ordinal)..];
        compute = compute[..compute.IndexOf("\n\t\t}", StringComparison.Ordinal)];

        compute.Should().Contain("SelfviewPipPositionFeedback.FireUpdate()",
            "listeners (the SIMPL position join, the messenger) must see position changes");
    }

    /// <summary>CiscoCodec is partial across several files; the self-view code is in CiscoCodec.cs.</summary>
    private static string CiscoCodecSource() =>
        File.ReadAllText(Path.Combine(AssemblyFixture.SourceDirectory, "CiscoCodec.cs"));
}
