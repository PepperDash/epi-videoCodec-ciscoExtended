using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>FramingShadow (#129, stage 2): compares the new framer with the current line-based framing.</summary>
public class FramingShadowTests
{
    private const string Doc = "{\r\n  \"Status\": {}\r\n}\r\n";

    private static void FeedLines(FramingShadow s, string text)
    {
        // The plugin feeds one received line at a time.
        foreach (var line in text.Split("\r\n").Where(l => l.Length > 0))
            s.Feed(line + "\r\n");
    }

    [Fact]
    public void Identical_Documents_Match()
    {
        var s = new FramingShadow();
        FeedLines(s, Doc);

        s.Compare(Doc).Should().BeTrue();
        s.Matches.Should().Be(1);
        s.Summary().Should().StartWith("match=1 mismatch=0 missing=0 extra=0");
    }

    [Fact]
    public void Empty_Registration_Replies_Are_Not_Compared()
    {
        var s = new FramingShadow();
        FeedLines(s, "{}\r\n{}\r\n" + Doc);

        s.Compare(Doc).Should().BeTrue();
        s.EmptyDocuments.Should().Be(2);
        s.Extra.Should().Be(0);
    }

    [Fact]
    public void A_Document_Only_The_New_Framer_Found_Counts_As_Extra()
    {
        var s = new FramingShadow();
        FeedLines(s, "{ \"Event\": { \"A\": 1 } }\r\n" + Doc);

        s.Compare(Doc).Should().BeTrue();
        s.Extra.Should().Be(1);
        s.Matches.Should().Be(1);
    }

    [Fact]
    public void A_Document_Only_The_Current_Framing_Found_Counts_As_Missing()
    {
        var s = new FramingShadow();
        string? current = null, mine = "unset";
        s.Mismatch += (c, m) => { current = c; mine = m; };

        s.Compare(Doc).Should().BeFalse();

        s.Missing.Should().Be(1);
        current.Should().Be(FramingShadow.Normalize(Doc));
        mine.Should().BeNull();
    }

    [Fact]
    public void Different_Documents_Are_Reported()
    {
        var s = new FramingShadow();
        FeedLines(s, "{\r\n  \"Status\": { \"A\": 1 }\r\n}\r\n");
        string? mine = null;
        s.Mismatch += (_, m) => mine = m;

        s.Compare(Doc).Should().BeFalse();

        s.Mismatches.Should().Be(1);
        mine.Should().Contain("\"A\": 1");
    }

    [Fact]
    public void Abandoned_Documents_And_Dropped_Echoes_Are_Counted()
    {
        var s = new FramingShadow();
        FeedLines(s, "{\r\n  \"Status\": {\r\nxStatus Audio\r\n    \"A\": 1\r\n*r Login successful\r\n");

        s.EchoesDropped.Should().Be(1);
        s.Abandoned.Should().Be(1);
    }

    [Fact]
    public void Difference_Shows_Where_And_Both_Sides()
    {
        var d = FramingShadow.Difference("{\"A\": {\"B\": 1}}", "{\"A\" {\"B\": 1}}", 40);

        d.Should().Contain("first difference at character 4").And.Contain("current: ").And.Contain("framer: ");
        FramingShadow.Difference("{}", null).Should().StartWith("only the current framing produced it");
    }

    [Fact]
    public void Reset_Clears_What_Is_Pending()
    {
        var s = new FramingShadow();
        FeedLines(s, Doc);
        s.Reset("connection to codec changed");

        s.Compare(Doc).Should().BeFalse();
        s.Missing.Should().Be(1);
    }
}
