using FluentAssertions;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>
/// A codec that loses every camera, including cameras the manager never moves, is in outage:
/// floating recovery must wait instead of probing.
/// </summary>
public class CodecCameraOutageTrackerTests
{
    private static readonly DateTime T0 = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void All_Cameras_Lost_After_An_Anchor_Was_Seen_Is_An_Outage()
    {
        var tracker = new CodecCameraOutageTracker();

        // A Quad (anchor) and a PTZ online.
        tracker.Update("codec-1", true, 2, 1, T0).Should().Be(CodecOutageTransition.None);
        // The PTZ drops first: not an outage, the Quad is still there.
        tracker.Update("codec-1", true, 1, 1, T0.AddSeconds(2)).Should().Be(CodecOutageTransition.None);
        tracker.IsInOutage("codec-1").Should().BeFalse();
        // Then the Quad: every camera is gone.
        tracker.Update("codec-1", true, 0, 0, T0.AddSeconds(4)).Should().Be(CodecOutageTransition.Started);

        tracker.IsInOutage("codec-1").Should().BeTrue();
        tracker.GetReason("codec-1").Should().Be(CodecCameraOutageTracker.ReasonAllCamerasLost);
        tracker.GetOutageSinceUtc("codec-1").Should().Be(T0.AddSeconds(4));
    }

    [Fact]
    public void Outage_Is_Reported_Once_And_Ends_When_A_Camera_Returns()
    {
        var tracker = new CodecCameraOutageTracker();
        tracker.Update("codec-1", true, 1, 1, T0);
        tracker.Update("codec-1", true, 0, 0, T0.AddSeconds(1)).Should().Be(CodecOutageTransition.Started);
        tracker.Update("codec-1", true, 0, 0, T0.AddSeconds(2)).Should().Be(CodecOutageTransition.None);
        tracker.Update("codec-1", true, 0, 0, T0.AddMinutes(5)).Should().Be(CodecOutageTransition.None);

        // Any camera back (here the PTZ before the Quad) ends the outage.
        tracker.Update("codec-1", true, 1, 0, T0.AddMinutes(6)).Should().Be(CodecOutageTransition.Ended);
        tracker.IsInOutage("codec-1").Should().BeFalse();
        tracker.GetReason("codec-1").Should().BeNull();
        tracker.GetOutageSinceUtc("codec-1").Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void Codec_With_Only_Moving_Cameras_Is_Never_Armed()
    {
        // A codec whose only cameras are moved by the manager can legitimately end up with none
        // (all moved to another codec). That must not be read as an outage.
        var tracker = new CodecCameraOutageTracker();
        tracker.Update("codec-2", true, 1, 0, T0);
        tracker.Update("codec-2", true, 0, 0, T0.AddSeconds(1)).Should().Be(CodecOutageTransition.None);

        tracker.IsArmed("codec-2").Should().BeFalse();
        tracker.IsInOutage("codec-2").Should().BeFalse();
    }

    [Fact]
    public void Codec_That_Never_Reported_Cameras_Is_Not_In_Outage()
    {
        // At startup the codec has not reported any camera yet.
        var tracker = new CodecCameraOutageTracker();
        tracker.Update("codec-1", true, 0, 0, T0).Should().Be(CodecOutageTransition.None);
        tracker.IsInOutage("codec-1").Should().BeFalse();
    }

    [Fact]
    public void Codec_Offline_Is_An_Outage_Even_With_Cameras_Listed()
    {
        var tracker = new CodecCameraOutageTracker();
        tracker.Update("codec-1", false, 2, 1, T0).Should().Be(CodecOutageTransition.Started);
        tracker.GetReason("codec-1").Should().Be(CodecCameraOutageTracker.ReasonCodecOffline);

        // Back online with cameras: outage over.
        tracker.Update("codec-1", true, 2, 1, T0.AddSeconds(30)).Should().Be(CodecOutageTransition.Ended);
    }

    [Fact]
    public void Offline_Codec_Does_Not_Arm_From_Stale_Cameras()
    {
        // Cameras listed while the codec is offline are stale; they must not arm the tracker.
        var tracker = new CodecCameraOutageTracker();
        tracker.Update("codec-1", false, 1, 1, T0);
        tracker.IsArmed("codec-1").Should().BeFalse();
    }

    [Fact]
    public void Reason_Follows_The_Current_Cause_Without_A_New_Transition()
    {
        var tracker = new CodecCameraOutageTracker();
        tracker.Update("codec-1", true, 1, 1, T0);
        tracker.Update("codec-1", true, 0, 0, T0.AddSeconds(1)).Should().Be(CodecOutageTransition.Started);
        tracker.Update("codec-1", false, 0, 0, T0.AddSeconds(2)).Should().Be(CodecOutageTransition.None);
        tracker.GetReason("codec-1").Should().Be(CodecCameraOutageTracker.ReasonCodecOffline);
        tracker.GetOutageSinceUtc("codec-1").Should().Be(T0.AddSeconds(1));
    }

    [Fact]
    public void Codecs_Are_Tracked_Independently()
    {
        var tracker = new CodecCameraOutageTracker();
        tracker.Update("codec-1", true, 1, 1, T0);
        tracker.Update("codec-2", true, 1, 1, T0);
        tracker.Update("codec-1", true, 0, 0, T0.AddSeconds(1));

        tracker.IsInOutage("codec-1").Should().BeTrue();
        tracker.IsInOutage("CODEC-1").Should().BeTrue();
        tracker.IsInOutage("codec-2").Should().BeFalse();
        tracker.IsInOutage("unknown").Should().BeFalse();
        tracker.IsInOutage("").Should().BeFalse();
    }
}
