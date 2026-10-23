using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>
/// XapiStreamFramer (#129, stage 2). It has no Crestron dependencies, so it is compiled into this project.
/// </summary>
public class XapiStreamFramerTests
{
    private const string Crlf = "\r\n";

    private static string Lines(params string[] lines) => string.Join(Crlf, lines) + Crlf;

    private sealed class Capture
    {
        public readonly List<string> Documents = new();
        public readonly List<(XapiLineKind Kind, string Text)> Lines = new();
        public readonly List<string> Abandoned = new();
        public readonly List<string> Echoes = new();

        public Capture(XapiStreamFramer f)
        {
            f.DocumentReceived += Documents.Add;
            f.LineReceived += (k, t) => Lines.Add((k, t));
            f.DocumentAbandoned += Abandoned.Add;
            f.EchoDropped += Echoes.Add;
        }
    }

    private static Capture Run(string text, int chunk = 0, int seed = 1)
    {
        var f = new XapiStreamFramer();
        var c = new Capture(f);
        if (chunk == 0)
        {
            f.Feed(text);
            return c;
        }

        var rnd = new Random(seed);
        for (var i = 0; i < text.Length;)
        {
            var n = Math.Min(text.Length - i, chunk < 0 ? rnd.Next(1, -chunk + 1) : chunk);
            f.Feed(text.Substring(i, n));
            i += n;
        }
        return c;
    }

    private static readonly string Volume = Lines(
        "{",
        "  \"Status\": {",
        "    \"Audio\": {",
        "      \"Volume\": {",
        "        \"Value\": \"49\"",
        "      }",
        "    }",
        "  }",
        "}");

    [Fact]
    public void A_Pretty_Printed_Document_Is_Found()
    {
        var c = Run(Volume);

        c.Documents.Should().ContainSingle();
        JObject.Parse(c.Documents[0])["Status"]!["Audio"]!["Volume"]!["Value"]!.Value<string>().Should().Be("49");
        c.Lines.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(-40)]
    public void Chunk_Boundaries_Do_Not_Matter(int chunk)
    {
        var stream = Lines("*r Login successful", "OK") + Volume + Lines("{}", "{}") + Volume;

        var whole = Run(stream);
        var split = Run(stream, chunk);

        split.Documents.Should().Equal(whole.Documents);
        split.Lines.Should().Equal(whole.Lines);
        whole.Documents.Should().HaveCount(4);
    }

    [Fact]
    public void Braces_And_Quotes_Inside_Strings_Are_Ignored()
    {
        var c = Run(Lines("{", "  \"A\": \"x}y{ [\",", "  \"B\": \"say \\\"}\\\" now\"", "}"));

        c.Documents.Should().ContainSingle();
        var o = JObject.Parse(c.Documents[0]);
        o["A"]!.Value<string>().Should().Be("x}y{ [");
        o["B"]!.Value<string>().Should().Be("say \"}\" now");
    }

    [Fact]
    public void One_Line_Documents_Are_Documents()
    {
        var c = Run(Lines("{}", "{}{}", "{ \"Event\": { \"X\": {} } }"));

        c.Documents.Should().Equal("{}", "{}", "{}", "{ \"Event\": { \"X\": {} } }");
    }

    [Fact]
    public void Lines_Between_Documents_Are_Classified()
    {
        var c = Run(Lines(
            "*s Audio Volume: 6", "** end", "OK", "ERROR",
            "/Status/Audio", "xStatus Audio Volume", "", "Welcome to"));

        c.Lines.Select(l => l.Kind).Should().Equal(
            XapiLineKind.TextModeOutput, XapiLineKind.CommandEnd, XapiLineKind.CommandEnd, XapiLineKind.CommandEnd,
            XapiLineKind.FeedbackListPath, XapiLineKind.Echo, XapiLineKind.Empty, XapiLineKind.Other);
    }

    [Fact]
    public void An_Echoed_Command_Line_Inside_A_Document_Is_Dropped()
    {
        var c = Run(Lines("{", "  \"Status\": {", "xFeedback List", "    \"A\": 1", "  }", "}"));

        c.Echoes.Should().Equal("xFeedback List");
        c.Documents.Should().ContainSingle();
        JObject.Parse(c.Documents[0])["Status"]!["A"]!.Value<int>().Should().Be(1);
    }

    [Fact]
    public void An_Echoed_Tagged_Command_With_Quoted_Values_Is_Dropped()
    {
        const string echo = "xCommand Phonebook Search SearchString: \"Smith\" Limit: 10 | resultId=\"0b8f\"";
        var c = Run(Lines("{", "  \"CommandResponse\": {", echo, "    \"status\": \"OK\"", "  }", "}"));

        c.Echoes.Should().Equal(echo);
        JObject.Parse(c.Documents.Single())["CommandResponse"]!["status"]!.Value<string>().Should().Be("OK");
        XapiStreamFramer.Classify(echo).Should().Be(XapiLineKind.Echo);
    }

    [Fact]
    public void Echo_Like_Text_Inside_A_String_Is_Kept()
    {
        var c = Run(Lines("{", "  \"Text\": \"line one", "xStatus inside a string\"", "}"));

        c.Echoes.Should().BeEmpty();
        c.Documents.Should().ContainSingle();
    }

    [Fact]
    public void Text_Mode_Output_Abandons_A_Partial_Document()
    {
        var c = Run(Lines("{", "  \"Status\": {") + Lines("*r Login successful", "OK") + Volume);

        c.Abandoned.Should().ContainSingle();
        c.Lines.Select(l => l.Kind).Should().Equal(XapiLineKind.TextModeOutput, XapiLineKind.CommandEnd);
        c.Documents.Should().ContainSingle().Which.Should().Contain("\"49\"");
    }

    [Fact]
    public void A_Lost_Quote_Does_Not_Throw_Framing_Out_Of_Step()
    {
        // A quote lost on the wire leaves a string open at the end of its line. JSON strings can't
        // contain a raw line break, so the framer closes it there and keeps counting braces.
        var unterminated = new List<string>();
        var f = new XapiStreamFramer();
        var c = new Capture(f);
        f.UnterminatedString += unterminated.Add;

        f.Feed(Lines("{", "  \"Booking\": {", "    \"Title\": \"Stand-up,", "    \"Extension\": {}", "  }", "}") + Volume);

        unterminated.Should().ContainSingle().Which.Should().Contain("Stand-up");
        c.Documents.Should().HaveCount(2, "the damaged document still ends on its closing brace, and the next one is found");
        c.Documents[1].Should().Contain("\"49\"");
    }

    [Fact]
    public void DocumentStarted_Fires_Once_Per_Document_Before_It_Is_Received()
    {
        var f = new XapiStreamFramer();
        var order = new List<string>();
        f.DocumentStarted += () => order.Add("started");
        f.DocumentReceived += _ => order.Add("received");

        f.Feed(Volume + Lines("{}"));

        order.Should().Equal("started", "received", "started", "received");
    }

    [Fact]
    public void A_Login_Banner_Inside_A_Document_Abandons_It()
    {
        var c = Run(Lines("{", "  \"Status\": {", "Login successful") + Volume);

        c.Abandoned.Should().ContainSingle();
        c.Lines.Should().ContainSingle().Which.Text.Should().Be("Login successful");
        c.Documents.Should().ContainSingle();
    }

    [Fact]
    public void Text_After_A_Document_On_The_Same_Line_Is_Its_Own_Line()
    {
        var c = Run(Lines("{ \"A\": 1 }xStatus Audio"));

        c.Documents.Should().Equal("{ \"A\": 1 }");
        c.Lines.Should().ContainSingle().Which.Should().Be((XapiLineKind.Echo, "xStatus Audio"));
    }

    [Fact]
    public void Nothing_Is_Reported_Until_A_Line_Ends()
    {
        var f = new XapiStreamFramer();
        var c = new Capture(f);

        f.Feed("{ \"A\": 1 }");
        c.Documents.Should().BeEmpty();
        f.Feed("\r\n");
        c.Documents.Should().ContainSingle();
    }

    [Fact]
    public void Reset_Abandons_A_Partial_Document()
    {
        var f = new XapiStreamFramer();
        var c = new Capture(f);

        f.Feed(Lines("{", "  \"Status\": {"));
        f.InDocument.Should().BeTrue();
        f.Reset("connection to codec changed");
        f.InDocument.Should().BeFalse();
        f.Feed(Volume);

        c.Abandoned.Should().Equal("connection to codec changed");
        c.Documents.Should().ContainSingle();
    }

    [Fact]
    public void Matches_The_Current_Line_Based_Framing_On_A_Mixed_Stream()
    {
        var stream = Lines("*r Login successful", "OK", "xStatus") + Volume +
                     Lines("{}", "/Status/Audio", "/Status/Call") + Volume;

        Run(stream, -30).Documents.Where(d => d != "{}").Select(Normalize)
            .Should().Equal(CurrentFraming(stream).Select(Normalize));
    }

    // ---- real codec output (scrubbed fixtures from the RoomOS emulator) --------------------------

    /// <summary>
    /// Folder of scrubbed RoomOS reply fixtures (one reply per file, as the codec sends it). They live in
    /// the internal epi-emulators repo, so these tests only run where ROOMOS_FIXTURES points at them.
    /// </summary>
    private static string[] Fixtures()
    {
        var dir = Environment.GetEnvironmentVariable("ROOMOS_FIXTURES");
        return string.IsNullOrEmpty(dir) || !Directory.Exists(dir)
            ? Array.Empty<string>()
            : Directory.GetFiles(dir, "*.txt").OrderBy(f => f, StringComparer.Ordinal).ToArray();
    }

    [Fact]
    public void ROOMOS_FIXTURES_When_Set_Points_At_Fixtures()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ROOMOS_FIXTURES")))
            Fixtures().Should().NotBeEmpty("ROOMOS_FIXTURES is set, so the replay tests must have something to replay");
    }

    [Fact]
    public void Every_Fixture_Is_One_Valid_Document_However_It_Is_Chunked_When_Available()
    {
        foreach (var file in Fixtures())
        {
            var text = File.ReadAllText(file);
            foreach (var chunk in new[] { 0, 1, -64, -4096 })
            {
                var c = Run(text, chunk, seed: file.Length);
                c.Documents.Should().ContainSingle($"{Path.GetFileName(file)} with chunk {chunk}");
                var act = () => JToken.Parse(c.Documents[0]);
                act.Should().NotThrow($"{Path.GetFileName(file)} must frame to valid JSON");
            }
        }
    }

    [Fact]
    public void All_Fixtures_Back_To_Back_Match_The_Current_Framing_When_Available()
    {
        var files = Fixtures();
        if (files.Length == 0)
            return;

        var sb = new StringBuilder(Lines("*r Login successful", "OK"));
        foreach (var file in files)
            sb.Append(File.ReadAllText(file)).Append(Lines("{}", "/Status/Audio", "** end"));
        var stream = sb.ToString();

        var framed = Run(stream, -200).Documents.Where(d => d != "{}").Select(Normalize).ToList();

        framed.Should().HaveCount(files.Length);
        framed.Should().Equal(CurrentFraming(stream).Select(Normalize));
    }

    // ---- reference copy of today's framing in CiscoCodec.ProcessResponse ------------------------

    private static readonly Regex EchoedCommandLinePattern = new(
        @"^\s*x(?:Command|Status|Configuration|Feedback|Preferences)\b[^{}\[\]""]*$", RegexOptions.IgnoreCase);

    /// <summary>The JSON documents today's line-based framing passes to DeserializeResponse.</summary>
    private static List<string> CurrentFraming(string stream)
    {
        var docs = new List<string>();
        StringBuilder? message = null;
        var incoming = false;
        foreach (var raw in stream.Split(Crlf))
        {
            var response = raw + Crlf;
            if (!incoming && response.TrimStart().StartsWith("xcommand", StringComparison.OrdinalIgnoreCase))
                continue;
            if (response == "{" + Crlf)
            {
                incoming = true;
                message = new StringBuilder();
            }
            else if (response == "}" + Crlf)
            {
                incoming = false;
                message!.Append(response);
                docs.Add(message.ToString());
                continue;
            }

            if (!incoming)
                continue;
            if (EchoedCommandLinePattern.IsMatch(response))
                continue;
            message!.Append(response);
        }
        return docs;
    }

    // A status document whose array elements each start with "{" on their own line, as RoomOS
    // pretty-prints them. The first widget's value is the text "Login successful".
    private static readonly string Widgets = Lines(
        "{",
        "  \"Status\": {",
        "    \"UserInterface\": { \"Extensions\": { \"Widget\": [",
        "      {",
        "        \"id\": \"1\",",
        "        \"Value\": \"Login successful\"",
        "      },",
        "      {",
        "        \"id\": \"2\",",
        "        \"Value\": \"On\"",
        "      }",
        "    ] } }",
        "  }",
        "}");

    [Fact]
    public void A_Login_Successful_Value_Does_Not_Abandon_The_Document()
    {
        var c = Run(Widgets);

        c.Abandoned.Should().BeEmpty();
        c.Documents.Should().ContainSingle()
            .Which.Should().Contain("\"Login successful\"").And.Contain("\"On\"");
    }

    [Fact]
    public void The_Rest_Of_An_Abandoned_Document_Is_Not_Framed_As_New_Documents()
    {
        // Abandon part-way through the widget array, as a stall reset or the size cap would
        var lines = Widgets.Split(Crlf);
        var f = new XapiStreamFramer();
        var c = new Capture(f);
        var dropped = new List<string>();
        f.RemainderDropped += dropped.Add;

        f.Feed(string.Join(Crlf, lines.Take(5)) + Crlf);
        f.Reset("stalled");
        f.Feed(string.Join(Crlf, lines.Skip(5)) + Volume);

        c.Abandoned.Should().ContainSingle();
        c.Documents.Should().ContainSingle().Which.Should().Contain("\"49\"");
        c.Lines.Should().BeEmpty();
        dropped.Should().Contain("}").And.Contain("        \"id\": \"2\",");
    }

    [Fact]
    public void Text_At_Column_Zero_After_An_Abandon_Is_Still_Reported()
    {
        // An RS-232 session reset mid-document: no remainder arrives, the banner and new output do
        var c = Run(Lines("{", "  \"Status\": {") + Lines("*r Login successful", "OK") + Volume);

        c.Lines.Select(l => l.Kind).Should().Equal(XapiLineKind.TextModeOutput, XapiLineKind.CommandEnd);
        c.Documents.Should().ContainSingle().Which.Should().Contain("\"49\"");
    }

    /// <summary>The same comparison the plugin's shadow mode makes: line endings removed.</summary>
    private static string Normalize(string document) => document.Replace("\r", "").Replace("\n", "");
}
