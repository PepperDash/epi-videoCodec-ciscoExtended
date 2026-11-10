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
    private static readonly TimeSpan MaxHold = TimeSpan.FromMinutes(30);
    // PTZ-1/PTZ-2 move between codec-1 and codec-2; FIXED-1 is a managed camera that only ever
    // belongs on codec-1. Anything else (e.g. QUAD-1) is unmanaged.
    private static readonly Dictionary<string, HashSet<string>> Owners = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PTZ-1"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "codec-1", "codec-2" },
        ["PTZ-2"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "codec-1", "codec-2" },
        ["FIXED-1"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "codec-1" },
    };

    [Fact]
    public void All_Cameras_Lost_After_An_Anchor_Was_Seen_Is_An_Outage()
    {
        var tracker = new CodecCameraOutageTracker(MaxHold);

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
        var tracker = new CodecCameraOutageTracker(MaxHold);
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
        var tracker = new CodecCameraOutageTracker(MaxHold);
        tracker.Update("codec-2", true, 1, 0, T0);
        tracker.Update("codec-2", true, 0, 0, T0.AddSeconds(1)).Should().Be(CodecOutageTransition.None);

        tracker.IsArmed("codec-2").Should().BeFalse();
        tracker.IsInOutage("codec-2").Should().BeFalse();
    }

    [Fact]
    public void Codec_That_Never_Reported_Cameras_Is_Not_In_Outage()
    {
        // At startup the codec has not reported any camera yet.
        var tracker = new CodecCameraOutageTracker(MaxHold);
        tracker.Update("codec-1", true, 0, 0, T0).Should().Be(CodecOutageTransition.None);
        tracker.IsInOutage("codec-1").Should().BeFalse();
    }

    [Fact]
    public void Codec_Offline_Is_An_Outage_Even_With_Cameras_Listed()
    {
        var tracker = new CodecCameraOutageTracker(MaxHold);
        tracker.Update("codec-1", false, 2, 1, T0).Should().Be(CodecOutageTransition.Started);
        tracker.GetReason("codec-1").Should().Be(CodecCameraOutageTracker.ReasonCodecOffline);

        // Back online with cameras: outage over.
        tracker.Update("codec-1", true, 2, 1, T0.AddSeconds(30)).Should().Be(CodecOutageTransition.Ended);
    }

    [Fact]
    public void Offline_Codec_Does_Not_Arm_From_Stale_Cameras()
    {
        // Cameras listed while the codec is offline are stale; they must not arm the tracker.
        var tracker = new CodecCameraOutageTracker(MaxHold);
        tracker.Update("codec-1", false, 1, 1, T0);
        tracker.IsArmed("codec-1").Should().BeFalse();
    }

    [Fact]
    public void Reason_Follows_The_Current_Cause_Without_A_New_Transition()
    {
        var tracker = new CodecCameraOutageTracker(MaxHold);
        tracker.Update("codec-1", true, 1, 1, T0);
        tracker.Update("codec-1", true, 0, 0, T0.AddSeconds(1)).Should().Be(CodecOutageTransition.Started);
        tracker.Update("codec-1", false, 0, 0, T0.AddSeconds(2)).Should().Be(CodecOutageTransition.None);
        tracker.GetReason("codec-1").Should().Be(CodecCameraOutageTracker.ReasonCodecOffline);
        tracker.GetOutageSinceUtc("codec-1").Should().Be(T0.AddSeconds(1));
    }

    [Fact]
    public void Codecs_Are_Tracked_Independently()
    {
        var tracker = new CodecCameraOutageTracker(MaxHold);
        tracker.Update("codec-1", true, 1, 1, T0);
        tracker.Update("codec-2", true, 1, 1, T0);
        tracker.Update("codec-1", true, 0, 0, T0.AddSeconds(1));

        tracker.IsInOutage("codec-1").Should().BeTrue();
        tracker.IsInOutage("CODEC-1").Should().BeTrue();
        tracker.IsInOutage("codec-2").Should().BeFalse();
        tracker.IsInOutage("unknown").Should().BeFalse();
        tracker.IsInOutage("").Should().BeFalse();
    }
    private static CodecCameraStatusSample Cam(bool connected, string? serial) => new CodecCameraStatusSample(connected, serial);

    private static (int Online, int Anchors) Count(params CodecCameraStatusSample[] cameras) => CountOn("codec-1", cameras);

    private static (int Online, int Anchors) CountOn(string codecKey, params CodecCameraStatusSample[] cameras)
    {
        CodecCameraOutageTracker.CountCameras(cameras, codecKey, Owners, out var online, out var anchors);
        return (online, anchors);
    }

    [Fact]
    public void Fixed_Managed_Camera_Is_An_Anchor_Only_On_Its_Own_Codec()
    {
        CountOn("codec-1", Cam(true, "FIXED-1")).Should().Be((1, 1));
        // Seen on codec-2 during a probe or wrong-codec recovery: online there, but not an anchor.
        CountOn("CODEC-2", Cam(true, "fixed-1")).Should().Be((1, 0));
    }

    [Fact]
    public void Unmanaged_Camera_Is_An_Anchor_On_Any_Codec()
    {
        CountOn("codec-2", Cam(true, "QUAD-9")).Should().Be((1, 1));
    }

    [Fact]
    public void Moving_Camera_Is_Never_An_Anchor()
    {
        CountOn("codec-1", Cam(true, "PTZ-1")).Should().Be((1, 0));
        CountOn("codec-2", Cam(true, "PTZ-2")).Should().Be((1, 0));
    }

    [Fact]
    public void Fixed_Camera_Visiting_Another_Codec_Does_Not_Arm_It()
    {
        // FIXED-1 shows up on codec-2 while being recovered, then goes home: codec-2 is left
        // empty but was never armed, so it must not be put in outage.
        var tracker = new CodecCameraOutageTracker(MaxHold);
        var (online, anchors) = CountOn("codec-2", Cam(true, "FIXED-1"));
        tracker.Update("codec-2", true, online, anchors, T0);

        (online, anchors) = CountOn("codec-2");
        tracker.Update("codec-2", true, online, anchors, T0.AddMinutes(1)).Should().Be(CodecOutageTransition.None);
        tracker.IsArmed("codec-2").Should().BeFalse();
        tracker.IsInOutage("codec-2").Should().BeFalse();
    }

    [Fact]
    public void Count_Treats_A_Known_Non_Moving_Serial_As_An_Anchor()
    {
        Count(Cam(true, "QUAD-1"), Cam(true, "PTZ-1")).Should().Be((2, 1));
    }

    [Fact]
    public void Count_Ignores_Disconnected_Cameras()
    {
        Count(Cam(false, "QUAD-1"), Cam(true, "ptz-1")).Should().Be((1, 0));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Unknown")]
    public void Count_Does_Not_Treat_An_Unidentified_Camera_As_An_Anchor(string? serial)
    {
        // A camera with no serial yet could be a moving PTZ: it counts as online but must not arm.
        Count(Cam(true, serial)).Should().Be((1, 0));
    }

    [Fact]
    public void Unidentified_Camera_Does_Not_Arm_So_Moving_The_Last_Ptz_Away_Is_Not_An_Outage()
    {
        var tracker = new CodecCameraOutageTracker(MaxHold);
        var (online, anchors) = Count(Cam(true, null));
        tracker.Update("codec-2", true, online, anchors, T0);

        (online, anchors) = Count();
        tracker.Update("codec-2", true, online, anchors, T0.AddSeconds(30)).Should().Be(CodecOutageTransition.None);
        tracker.IsInOutage("codec-2").Should().BeFalse();
    }

    [Fact]
    public void Anchor_Returning_While_The_Ptz_Is_Still_Missing_Ends_The_Outage()
    {
        var tracker = new CodecCameraOutageTracker(MaxHold);
        var (online, anchors) = Count(Cam(true, "QUAD-1"), Cam(true, "PTZ-1"));
        tracker.Update("codec-1", true, online, anchors, T0);
        tracker.Update("codec-1", true, 0, 0, T0.AddSeconds(5)).Should().Be(CodecOutageTransition.Started);

        (online, anchors) = Count(Cam(true, "QUAD-1"), Cam(false, "PTZ-1"));
        tracker.Update("codec-1", true, online, anchors, T0.AddMinutes(3)).Should().Be(CodecOutageTransition.Ended);
        tracker.IsHolding("codec-1").Should().BeFalse();
    }

    [Fact]
    public void All_Cameras_Lost_Hold_Expires_At_The_Ceiling()
    {
        var tracker = new CodecCameraOutageTracker(MaxHold);
        tracker.Update("codec-1", true, 1, 1, T0);
        tracker.Update("codec-1", true, 0, 0, T0.AddSeconds(1)).Should().Be(CodecOutageTransition.Started);
        tracker.IsHolding("codec-1").Should().BeTrue();

        tracker.Update("codec-1", true, 0, 0, T0.AddSeconds(1) + MaxHold - TimeSpan.FromSeconds(1)).Should().Be(CodecOutageTransition.None);
        tracker.Update("codec-1", true, 0, 0, T0.AddSeconds(1) + MaxHold).Should().Be(CodecOutageTransition.HoldExpired);
        tracker.Update("codec-1", true, 0, 0, T0.AddHours(2)).Should().Be(CodecOutageTransition.None);

        // Still in outage (for logging), but no longer holding recovery.
        tracker.IsInOutage("codec-1").Should().BeTrue();
        tracker.IsHolding("codec-1").Should().BeFalse();

        // A camera back ends it; a later outage gets a fresh hold.
        tracker.Update("codec-1", true, 1, 1, T0.AddHours(3)).Should().Be(CodecOutageTransition.Ended);
        tracker.Update("codec-1", true, 0, 0, T0.AddHours(4)).Should().Be(CodecOutageTransition.Started);
        tracker.IsHolding("codec-1").Should().BeTrue();
    }

    [Fact]
    public void Codec_Offline_Holds_Without_A_Ceiling()
    {
        var tracker = new CodecCameraOutageTracker(MaxHold);
        tracker.Update("codec-1", false, 0, 0, T0).Should().Be(CodecOutageTransition.Started);
        tracker.Update("codec-1", false, 0, 0, T0 + MaxHold + MaxHold).Should().Be(CodecOutageTransition.None);
        tracker.IsHolding("codec-1").Should().BeTrue();
    }
}
