using FluentAssertions;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Cameras;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>
/// Floating recovery must not feed itself: a camera rebooting from the manager's own port action
/// is not lost, and a camera the codec reports connected (in any slot) is never probed.
/// </summary>
public class CameraBootWindowAndPresenceTests
{
    private static readonly DateTime T0 = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(180);

    [Fact]
    public void Port_Is_Booting_For_The_Window_After_A_Manager_Port_Action()
    {
        var boot = new CameraBootWindow(Window);
        boot.RecordPortAction("gi1/0/14", T0);

        boot.IsBooting("gi1/0/14", T0.AddSeconds(90), out var until).Should().BeTrue();
        until.Should().Be(T0 + Window);
        boot.IsBooting("GI1/0/14 ", T0.AddSeconds(179), out _).Should().BeTrue();
        boot.IsBooting("gi1/0/14", T0 + Window, out _).Should().BeFalse();
    }

    [Fact]
    public void A_Later_Port_Action_Restarts_The_Window()
    {
        // Field case: probe PoE cycle, then a VLAN change 12 s later: the camera boots from the last one.
        var boot = new CameraBootWindow(Window);
        boot.RecordPortAction("gi1/0/14", T0);
        boot.RecordPortAction("gi1/0/14", T0.AddSeconds(12));

        boot.IsBooting("gi1/0/14", T0.AddSeconds(185), out var until).Should().BeTrue();
        until.Should().Be(T0.AddSeconds(12) + Window);
    }

    [Fact]
    public void Untouched_Ports_And_Zero_Window_Are_Not_Booting()
    {
        var boot = new CameraBootWindow(Window);
        boot.RecordPortAction("gi1/0/14", T0);
        boot.IsBooting("gi1/0/15", T0.AddSeconds(1), out _).Should().BeFalse();
        boot.IsBooting("", T0.AddSeconds(1), out _).Should().BeFalse();

        var none = new CameraBootWindow(TimeSpan.Zero);
        none.RecordPortAction("gi1/0/14", T0);
        none.IsBooting("gi1/0/14", T0, out _).Should().BeFalse();
    }

    [Fact]
    public void Boot_Window_Is_Longer_Than_The_First_Sighting_Grace()
    {
        // A PTZ takes 90 s+ to boot; the 60 s first-sighting grace alone let the next probe restart it.
        new CameraBootWindow(Window).Window.Should().BeGreaterThan(TimeSpan.FromSeconds(90));
    }

    private static KeyValuePair<string, IReadOnlyList<CodecCameraStatusSample>> Codec(string key, params CodecCameraStatusSample[] cameras)
        => new(key, cameras);

    [Fact]
    public void Camera_Connected_On_Its_Target_In_A_Different_Slot_Is_Found()
    {
        // Field case: configured for slot 9, codec reports it connected in slot 7.
        var codecs = new[]
        {
            Codec("codecA", new CodecCameraStatusSample(true, "QUAD-A", 1)),
            Codec("codecC", new CodecCameraStatusSample(true, "ptz-c", 7)),
        };

        CameraPresence.TryFindConnected(codecs, "PTZ-C", "codecC", out var match).Should().BeTrue();
        match.CodecKey.Should().Be("codecC");
        match.CameraId.Should().Be(7u);
    }

    [Fact]
    public void Target_Codec_Is_Preferred_When_Several_Report_The_Serial()
    {
        var codecs = new[]
        {
            Codec("codecB", new CodecCameraStatusSample(true, "PTZ-C", 8)),
            Codec("codecC", new CodecCameraStatusSample(true, "PTZ-C", 9)),
        };

        CameraPresence.TryFindConnected(codecs, "PTZ-C", "codecC", out var match).Should().BeTrue();
        match.CodecKey.Should().Be("codecC");
        match.CameraId.Should().Be(9u);
    }

    [Fact]
    public void Camera_Connected_Only_On_Another_Codec_Is_Found_There()
    {
        var codecs = new[]
        {
            Codec("codecB", new CodecCameraStatusSample(true, "PTZ-C", 8)),
            Codec("codecC", new CodecCameraStatusSample(true, "QUAD-C", 1)),
        };

        CameraPresence.TryFindConnected(codecs, "PTZ-C", "codecC", out var match).Should().BeTrue();
        match.CodecKey.Should().Be("codecB");
    }

    [Fact]
    public void Completion_Restarts_The_Window_From_When_Power_Returns()
    {
        // PoE-on sent at T0 but queued on a slow console; it completes 150 s later. The camera
        // boots from then, so the window must run to T0+150+180, not T0+180.
        var boot = new CameraBootWindow(Window);
        boot.RecordPortAction("gi1/0/14", T0);
        boot.RefreshOnCompletion("gi1/0/14", T0.AddSeconds(150)).Should().BeTrue();

        boot.IsBooting("gi1/0/14", T0.AddSeconds(300), out var until).Should().BeTrue();
        until.Should().Be(T0.AddSeconds(150) + Window);
    }

    [Fact]
    public void Completion_On_A_Port_The_Manager_Never_Touched_Opens_No_Window()
    {
        // The switch reporting a port's current state (e.g. at startup) is not a manager action.
        var boot = new CameraBootWindow(Window);
        boot.RefreshOnCompletion("gi1/0/20", T0).Should().BeFalse();
        boot.IsBooting("gi1/0/20", T0.AddSeconds(1), out _).Should().BeFalse();
    }

    [Fact]
    public void Late_Completion_Never_Moves_The_Window_Backwards()
    {
        var boot = new CameraBootWindow(Window);
        boot.RecordPortAction("gi1/0/14", T0.AddSeconds(10));
        boot.RefreshOnCompletion("gi1/0/14", T0.AddSeconds(5));
        boot.IsBooting("gi1/0/14", T0.AddSeconds(5) + Window, out var until).Should().BeTrue();
        until.Should().Be(T0.AddSeconds(10) + Window);
    }

    [Fact]
    public void Evaluate_Classifies_Target_Other_And_Absent()
    {
        var codecs = new[]
        {
            Codec("codecB", new CodecCameraStatusSample(true, "PTZ-B", 8)),
            Codec("codecC", new CodecCameraStatusSample(true, "PTZ-C", 7)),
        };

        CameraPresence.Evaluate(codecs, true, "PTZ-C", "codecC", out _).Should().Be(CameraPresenceState.OnTarget);
        CameraPresence.Evaluate(codecs, true, "PTZ-B", "codecC", out var other).Should().Be(CameraPresenceState.OnOtherCodec);
        other.CodecKey.Should().Be("codecB");
        CameraPresence.Evaluate(codecs, true, "PTZ-X", "codecC", out _).Should().Be(CameraPresenceState.Absent);
    }

    [Fact]
    public void Not_Found_With_An_Unsampled_Codec_Is_Unknown_Not_Absent()
    {
        // A codec whose list was changing was left out: the camera may be connected there.
        var codecs = new[] { Codec("codecB", new CodecCameraStatusSample(true, "QUAD-B", 1)) };

        CameraPresence.Evaluate(codecs, false, "PTZ-C", "codecC", out _).Should().Be(CameraPresenceState.Unknown);
    }

    [Fact]
    public void Found_Is_Still_Reported_When_Another_Codec_Was_Not_Sampled()
    {
        var codecs = new[] { Codec("codecC", new CodecCameraStatusSample(true, "PTZ-C", 9)) };
        CameraPresence.Evaluate(codecs, false, "PTZ-C", "codecC", out _).Should().Be(CameraPresenceState.OnTarget);
    }

    [Fact]
    public void Status_Refresh_Is_Rate_Limited_Per_Codec()
    {
        var limiter = new KeyedRateLimiter(TimeSpan.FromSeconds(60));
        limiter.TryAcquire("codecB", T0).Should().BeTrue();
        limiter.TryAcquire("CODECB", T0.AddSeconds(30)).Should().BeFalse();
        limiter.TryAcquire("codecC", T0.AddSeconds(30)).Should().BeTrue();
        limiter.TryAcquire("codecB", T0.AddSeconds(60)).Should().BeTrue();
        limiter.TryAcquire("", T0).Should().BeFalse();
    }

    [Fact]
    public void Disconnected_Or_Unknown_Serials_Are_Not_Found()
    {
        var codecs = new[]
        {
            Codec("codecC", new CodecCameraStatusSample(false, "PTZ-C", 9), new CodecCameraStatusSample(true, null, 7)),
        };

        CameraPresence.TryFindConnected(codecs, "PTZ-C", "codecC", out _).Should().BeFalse();
        CameraPresence.TryFindConnected(codecs, "", "codecC", out _).Should().BeFalse();
    }
}
