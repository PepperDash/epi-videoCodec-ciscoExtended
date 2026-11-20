using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>
/// Dropped commands are logged once per episode, with a reminder while it lasts, and the recovery is
/// reported once (#194).
/// </summary>
public class DropEpisodeTrackerTests
{
    private static readonly DateTime T0 = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Reminder = TimeSpan.FromMinutes(5);

    private static DropEpisodeTracker Create() => new DropEpisodeTracker(Reminder);

    [Fact]
    public void First_Drop_Of_An_Episode_Is_Logged()
    {
        var tracker = Create();

        tracker.OnDropped(T0, out var count).Should().BeTrue();
        count.Should().Be(1);
        tracker.DroppedInEpisode.Should().Be(1);
    }

    [Fact]
    public void Further_Drops_Are_Quiet_Until_The_Reminder_Interval()
    {
        var tracker = Create();
        tracker.OnDropped(T0, out _);

        tracker.OnDropped(T0.AddSeconds(30), out _).Should().BeFalse();
        tracker.OnDropped(T0.AddMinutes(4), out _).Should().BeFalse();

        tracker.OnDropped(T0.AddMinutes(5), out var count).Should().BeTrue();
        count.Should().Be(3, "the reminder counts every drop since the last logged one, itself included");
        tracker.DroppedInEpisode.Should().Be(4);
    }

    [Fact]
    public void Reminder_Interval_Restarts_From_The_Last_Logged_Drop()
    {
        var tracker = Create();
        tracker.OnDropped(T0, out _);
        tracker.OnDropped(T0.AddMinutes(5), out _).Should().BeTrue();

        tracker.OnDropped(T0.AddMinutes(9), out _).Should().BeFalse();
        tracker.OnDropped(T0.AddMinutes(10), out var count).Should().BeTrue();
        count.Should().Be(2);
    }

    [Fact]
    public void Success_Ends_The_Episode_And_Reports_The_Total()
    {
        var tracker = Create();
        tracker.OnDropped(T0, out _);
        tracker.OnDropped(T0.AddSeconds(1), out _);
        tracker.OnDropped(T0.AddSeconds(2), out _);

        tracker.OnSucceeded().Should().Be(3);
        tracker.DroppedInEpisode.Should().Be(0);
        tracker.OnSucceeded().Should().Be(0, "a second success reports nothing");
    }

    [Fact]
    public void Next_Episode_Logs_Its_First_Drop_Again()
    {
        var tracker = Create();
        tracker.OnDropped(T0, out _);
        tracker.OnSucceeded();

        tracker.OnDropped(T0.AddSeconds(10), out var count).Should().BeTrue("a new episode starts with a logged drop");
        count.Should().Be(1);
    }

    [Fact]
    public void Success_With_No_Episode_Reports_Nothing()
    {
        Create().OnSucceeded().Should().Be(0);
    }

    [Fact]
    public void Reminder_Interval_Must_Be_Positive()
    {
        var act = () => new DropEpisodeTracker(TimeSpan.Zero);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("xCommand Audio Microphones Mute", "xCommand Audio Microphones ...")]
    [InlineData("xCommand Dial Number: \"1234@example.com\"", "xCommand Dial Number ...")]
    [InlineData("xCommand Dial Number:\"1234@example.com\"", "xCommand Dial Number ...")]
    [InlineData("xCommand Dial Number:1234", "xCommand Dial Number ...")]
    [InlineData("xConfiguration Audio DefaultVolume: 50", "xConfiguration Audio DefaultVolume ...")]
    [InlineData("xStatus | resultId=\"abc\"", "xStatus ...")]
    [InlineData("xCommand Phonebook Search PhonebookType=Corporate", "xCommand Phonebook Search ...")]
    [InlineData(":\"secret\"", "(arguments only) ...")]
    [InlineData("xStatus Cameras Camera", "xStatus Cameras Camera")]
    [InlineData("xFeedback List", "xFeedback List")]
    [InlineData("  echo off  ", "echo off")]
    [InlineData("", "(empty)")]
    [InlineData(null, "(empty)")]
    public void Describe_Keeps_Only_The_First_Words(string? command, string expected)
    {
        DropEpisodeTracker.Describe(command).Should().Be(expected);
    }

    [Fact]
    public void Describe_Caps_A_Long_Word()
    {
        var described = DropEpisodeTracker.Describe(new string('x', 200));
        described.Should().Be(new string('x', 60) + " ...");
    }
}
