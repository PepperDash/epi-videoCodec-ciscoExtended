using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>
/// The codec exposes its self-view (PIP) position through Essentials' IHasSelfviewPosition and a Mobile Control
/// messenger on /device/{key}, with the names mobile-control-react-app-core's useIHasSelfviewPosition hook uses:
/// /toggleSelfviewPosition, /setSelfviewPosition, selfviewPipPosition and availablePositions.
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

        source.Should().Contain("new IHasSelfviewPositionMessenger($\"selfviewPosition-{Key}\", $\"/device/{Key}\", this, SelfviewPipPositions)");
    }

    [Fact]
    public void The_messenger_uses_the_app_core_hook_names()
    {
        var source = AssemblyFixture.FindSourceForClass("IHasSelfviewPositionMessenger");

        source.Should().NotBeNull();
        source!.Should().Contain("AddAction(\"/toggleSelfviewPosition\"");
        source.Should().Contain("AddAction(\"/setSelfviewPosition\"");
        source.Should().Contain("AddAction(\"/fullStatus\"");
        source.Should().Contain("[JsonProperty(\"selfviewPipPosition\"");
        source.Should().Contain("[JsonProperty(\"availablePositions\"");
        source.Should().Contain("[JsonProperty(\"command\")]");
        source.Should().Contain("[JsonProperty(\"label\")]");
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
