using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>RS-232 login prompt detection (#115). Inputs are as captured on a Room Bar, RoomOS 26.10.</summary>
public class SerialLoginDetectorTests
{
    private static List<SerialLoginSignal> Feed(params string[] chunks)
    {
        var detector = new SerialLoginDetector();
        var signals = new List<SerialLoginSignal>();
        detector.SignalReceived += signals.Add;
        foreach (var chunk in chunks)
            detector.Feed(chunk);
        return signals;
    }

    [Fact]
    public void Login_Prompt_Without_Line_Ending_Is_Detected()
    {
        Feed("\r\r\nlogin: ").Should().Equal(SerialLoginSignal.LoginPrompt);
    }

    [Fact]
    public void Prompt_Split_Across_Chunks_Is_Detected()
    {
        Feed("\r\r\nlog", "in: ").Should().Equal(SerialLoginSignal.LoginPrompt);
    }

    [Fact]
    public void Captured_Failed_Login_Sequence()
    {
        Feed("\r\r\nlogin: ", "xFeedback list\r\r\n", "Password: ", "\r\n", "Login incorrect\r\n\r\n", "login: ")
            .Should().Equal(
                SerialLoginSignal.LoginPrompt,
                SerialLoginSignal.PasswordPrompt,
                SerialLoginSignal.LoginIncorrect,
                SerialLoginSignal.LoginPrompt);
    }

    [Fact]
    public void Lockout_Line_Is_Detected()
    {
        Feed("Account locked due to 7 failed logins\r\n").Should().Equal(SerialLoginSignal.AccountLocked);
    }

    [Fact]
    public void Prompt_Is_Signalled_Once_Until_A_New_Line()
    {
        Feed("login: ", " ").Should().Equal(SerialLoginSignal.LoginPrompt);
    }

    [Theory]
    [InlineData("      \"Value\": \"login: \"\r\n")]
    [InlineData("{ \"Title\": { \"Value\": \"Password: ")]
    [InlineData("*r Login successful\r\n")]
    [InlineData("OK\r\n")]
    public void Codec_Output_Is_Not_A_Login_Signal(string text)
    {
        Feed(text).Should().BeEmpty();
    }

    [Fact]
    public void Long_Unfinished_Line_Ending_In_A_Prompt_Word_Is_Not_A_Prompt()
    {
        Feed(new string(' ', 100), "login: ").Should().BeEmpty();
    }

    [Fact]
    public void Prompt_After_A_Long_Line_Is_Detected()
    {
        Feed(new string('x', 100), "\r\nlogin: ").Should().Equal(SerialLoginSignal.LoginPrompt);
    }
}
