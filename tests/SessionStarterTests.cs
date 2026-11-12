using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>
/// A session whose setup is dropped before the connection reports connected is started again on the
/// codec's next output, full status included, instead of waiting for the sync's 60 s retry.
/// </summary>
public class SessionStarterTests
{
    private const string FullStatus = "xStatus | resultId=\"1\"";

    private sealed class Connection
    {
        public bool Connected;
        public bool FullStatusPending; // a tagged request went out and has no reply yet
        public readonly List<string> Written = new List<string>();

        public bool Send(string command)
        {
            if (!Connected)
                return false; // dropped, like SendTextWithoutQueue while the client reports not connected
            Written.Add(command);
            return true;
        }

        public bool RequestFullStatus() => FullStatusPending = Send(FullStatus);

        public SessionStarter Starter() => new SessionStarter(Send, RequestFullStatus, () => FullStatusPending);
    }

    [Fact]
    public void A_Setup_Dropped_Before_Connect_Is_Sent_Again_On_The_Next_Output()
    {
        // The login banner arrives after the SSH client subscribes to data but before it reports
        // connected (2 s on site on 2026-11-12): echo off, JSON mode and the full status are all dropped.
        var conn = new Connection();
        var session = conn.Starter();

        var first = session.Start(jsonModeSet: false, fullStatusReceived: false);
        first.IsNewSession.Should().BeTrue();
        first.SetupDropped.Should().BeTrue();
        session.Claimed.Should().BeFalse("its setup never went out");
        conn.Written.Should().BeEmpty();

        // Connected; the communication monitor's poll is echoed (echo is still on).
        conn.Connected = true;
        var second = session.Start(jsonModeSet: false, fullStatusReceived: false);

        second.IsNewSession.Should().BeTrue();
        second.SetupDropped.Should().BeFalse();
        session.Claimed.Should().BeTrue();
        conn.Written.Should().Equal("echo off", "xPreferences outputmode json", FullStatus);
    }

    [Fact]
    public void A_Normal_Start_Asks_For_The_Full_Status_Once()
    {
        var conn = new Connection { Connected = true };
        var session = conn.Starter();

        session.Start(false, false).SetupDropped.Should().BeFalse();   // the login banner
        session.Start(false, false).IsNewSession.Should().BeFalse();   // then the poll's echo

        conn.Written.Should().Equal("echo off", "xPreferences outputmode json", FullStatus,
            "echo off", "xPreferences outputmode json");
    }

    [Fact]
    public void A_Partly_Sent_Setup_Is_Started_Again()
    {
        // The window can end between two sends (on the emulator only echo off was lost).
        var conn = new Connection();
        var session = new SessionStarter(
            c => { var ok = conn.Send(c); conn.Connected = true; return ok; },
            conn.RequestFullStatus, () => conn.FullStatusPending);

        session.Start(false, false).SetupDropped.Should().BeTrue();
        conn.Written.Should().Equal("xPreferences outputmode json", FullStatus);

        // The poll's echo starts it again before the full status has replied: it is not asked for twice.
        session.Start(false, false).SetupDropped.Should().BeFalse("everything got through this time");
        session.Claimed.Should().BeTrue();
        conn.Written.Should().Equal("xPreferences outputmode json", FullStatus, "echo off", "xPreferences outputmode json");
    }

    [Fact]
    public void A_Partly_Sent_Setup_Restarted_By_The_Full_Status_Reply_Does_Not_Ask_Again()
    {
        // Only echo off was lost; the next output is the full status reply itself (the codec is in JSON
        // mode), which marks the full status received before the JSON step adopts the session.
        var conn = new Connection();
        var session = new SessionStarter(
            c => { var ok = conn.Send(c); conn.Connected = true; return ok; },
            conn.RequestFullStatus, () => conn.FullStatusPending);
        session.Start(false, false).SetupDropped.Should().BeTrue();

        conn.FullStatusPending = false; // the reply arrived
        session.Adopt(fullStatusReceived: true).SetupDropped.Should().BeFalse();
        session.Claimed.Should().BeTrue();
        conn.Written.Should().Equal("xPreferences outputmode json", FullStatus, "echo off");
    }

    [Fact]
    public void An_Existing_Session_Keeps_Its_Claim_When_A_Resend_Is_Dropped()
    {
        // Set up earlier; a later echo off / JSON mode resend is dropped by a disconnect. The claim stays:
        // the disconnect resets the sync anyway, and no new full status was due.
        var conn = new Connection { Connected = true };
        var session = conn.Starter();
        session.Start(false, false);

        conn.Connected = false;
        var again = session.Start(false, false);
        again.IsNewSession.Should().BeFalse();
        again.SetupDropped.Should().BeFalse();
        session.Claimed.Should().BeTrue();
    }

    [Fact]
    public void Json_Mode_Already_Confirmed_Sends_Nothing()
    {
        var conn = new Connection { Connected = true };
        var session = conn.Starter();

        var result = session.Start(jsonModeSet: true, fullStatusReceived: false);
        result.SetupDropped.Should().BeFalse();
        session.Claimed.Should().BeTrue();
        conn.Written.Should().BeEmpty();
    }

    [Fact]
    public void A_Session_That_Already_Has_Its_Full_Status_Does_Not_Ask_Again()
    {
        var conn = new Connection { Connected = true };
        conn.Starter().Start(jsonModeSet: false, fullStatusReceived: true);
        conn.Written.Should().Equal("echo off", "xPreferences outputmode json");
    }

    [Fact]
    public void An_Adopted_Session_Whose_Setup_Was_Dropped_Is_Adopted_Again()
    {
        // JSON from a session set up before a program restart, arriving before connect.
        var conn = new Connection();
        var session = conn.Starter();

        session.Adopt(fullStatusReceived: false).SetupDropped.Should().BeTrue();
        session.Claimed.Should().BeFalse();

        conn.Connected = true;
        session.Adopt(fullStatusReceived: false).SetupDropped.Should().BeFalse();
        session.Claimed.Should().BeTrue();
        conn.Written.Should().Equal("echo off", FullStatus);
    }
}
