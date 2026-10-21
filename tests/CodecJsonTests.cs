using System.IO;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>
/// Stage 1 of #129 populates the plugin's classes straight from the parsed token instead of writing
/// the token back to text and parsing it again. These tests run both paths on codec-shaped JSON and
/// require the same result. CodecJson has no Crestron dependencies, so it is compiled into this project.
/// </summary>
public class CodecJsonTests
{
    // Shapes taken from RoomOS JSON output mode: values wrapped in {"Value": ...}, lists with "id",
    // DateTime values and ISO dates held in string properties.
    public class ValueString { public string? Value { get; set; } }
    public class ValueInt { public int Value { get; set; } }
    public class ValueDate { public DateTime Value { get; set; } }

    public class Booking
    {
        [JsonProperty("id")] public string? BookingId { get; set; }
        public ValueString? Title { get; set; }
        public ValueDate? StartTime { get; set; }
        public ValueString? StartTimeText { get; set; }
    }

    public class Model
    {
        public ValueString? Name { get; set; }
        public ValueInt? Volume { get; set; }
        public ValueDate? SystemTime { get; set; }
        public ValueString? LastShutdownTime { get; set; }
        public List<Booking>? Booking { get; set; }
        public ValueString? Untouched { get; set; }
    }

    private const string CodecJsonText = @"{
  ""Name"": { ""Value"": ""Room Bar"" },
  ""Volume"": { ""Value"": 45 },
  ""SystemTime"": { ""Value"": ""2026-10-19T14:05:09-0700"" },
  ""LastShutdownTime"": { ""Value"": ""2026-10-06T20:20:52Z"" },
  ""Booking"": [
    { ""id"": ""1"", ""Title"": { ""Value"": ""Stand-up"" },
      ""StartTime"": { ""Value"": ""2026-10-20T16:00:00Z"" },
      ""StartTimeText"": { ""Value"": ""2026-10-20T16:00:00Z"" } },
    { ""id"": ""2"", ""Title"": { ""Value"": ""Review"" },
      ""StartTime"": { ""Value"": ""2026-10-20T18:30:00"" },
      ""StartTimeText"": { ""Value"": ""2026-10-20T18:30:00"" } }
  ],
  ""NotModelled"": { ""Value"": ""ignored"" }
}";

    private static JObject LoadOld(string json)
    {
        using var reader = new JsonTextReader(new StringReader(json));
        return JObject.Load(reader);
    }

    private static JObject LoadNew(string json)
    {
        using var reader = CodecJson.CreateMessageReader(new StringReader(json));
        return JObject.Load(reader);
    }

    private static T PopulateOld<T>(JToken token, T target, JsonSerializerSettings? settings = null) where T : class
    {
        JsonConvert.PopulateObject(token.ToString(), target, settings);
        return target;
    }

    private static T PopulateNew<T>(JToken token, T target, JsonSerializerSettings? settings = null) where T : class
    {
        CodecJson.PopulateFromToken(token, target, settings);
        return target;
    }

    private static string Snapshot(object o) => JsonConvert.SerializeObject(o, new JsonSerializerSettings
    {
        DateFormatString = "o", // includes Kind, so a change of Kind is caught too
    });

    [Fact]
    public void New_Path_Gives_The_Same_Result_As_Text_Round_Trip()
    {
        var oldResult = PopulateOld(LoadOld(CodecJsonText), new Model());
        var newResult = PopulateNew(LoadNew(CodecJsonText), new Model());

        Snapshot(newResult).Should().Be(Snapshot(oldResult));
    }

    [Fact]
    public void DateTime_Values_Keep_Their_Instant_And_Kind()
    {
        var oldResult = PopulateOld(LoadOld(CodecJsonText), new Model());
        var newResult = PopulateNew(LoadNew(CodecJsonText), new Model());

        newResult.SystemTime!.Value.Should().Be(oldResult.SystemTime!.Value);
        newResult.SystemTime.Value.Kind.Should().Be(oldResult.SystemTime.Value.Kind);

        for (var i = 0; i < 2; i++)
        {
            newResult.Booking![i].StartTime!.Value.Should().Be(oldResult.Booking![i].StartTime!.Value);
            newResult.Booking[i].StartTime!.Value.Kind.Should().Be(oldResult.Booking[i].StartTime!.Value.Kind);
        }

        newResult.Booking![0].StartTime!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Dates_In_String_Properties_Stay_As_The_Codec_Sent_Them()
    {
        var newResult = PopulateNew(LoadNew(CodecJsonText), new Model());

        newResult.LastShutdownTime!.Value.Should().Be("2026-10-06T20:20:52Z");
        newResult.Booking![1].StartTimeText!.Value.Should().Be("2026-10-20T18:30:00");
    }

    [Fact]
    public void Populating_Merges_Into_An_Existing_Object()
    {
        var partial = @"{ ""Volume"": { ""Value"": 60 } }";

        var oldResult = PopulateOld(LoadOld(partial), new Model { Untouched = new ValueString { Value = "keep" } });
        var newResult = PopulateNew(LoadNew(partial), new Model { Untouched = new ValueString { Value = "keep" } });

        newResult.Untouched!.Value.Should().Be("keep");
        newResult.Volume!.Value.Should().Be(60);
        Snapshot(newResult).Should().Be(Snapshot(oldResult));
    }

    [Fact]
    public void Settings_Are_Applied_The_Same_Way()
    {
        // The camera merge passes NullValueHandling.Ignore and MissingMemberHandling.Ignore.
        var json = @"{ ""Name"": null, ""Volume"": { ""Value"": 10 }, ""Extra"": 1 }";
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
        };

        var oldResult = PopulateOld(LoadOld(json), new Model { Name = new ValueString { Value = "kept" } }, settings);
        var newResult = PopulateNew(LoadNew(json), new Model { Name = new ValueString { Value = "kept" } }, settings);

        newResult.Name!.Value.Should().Be("kept");
        Snapshot(newResult).Should().Be(Snapshot(oldResult));
    }

    [Fact]
    public void ToObject_Keeps_DateTime_Values_And_Fixes_Dates_In_String_Properties()
    {
        // Call history, cameras, WebView events and the Webex PIN handler use ToObject<T>() on tokens
        // from the message. Their DateTime values are unchanged...
        var oldResult = LoadOld(CodecJsonText).ToObject<Model>()!;
        var newResult = LoadNew(CodecJsonText).ToObject<Model>()!;

        newResult.SystemTime!.Value.Should().Be(oldResult.SystemTime!.Value);
        newResult.Booking![0].StartTime!.Value.Should().Be(oldResult.Booking![0].StartTime!.Value);
        newResult.Booking[0].StartTime!.Value.Kind.Should().Be(oldResult.Booking[0].StartTime!.Value.Kind);

        // ...but a date going into a string property used to come out in the current culture's
        // format, losing the "Z". It now stays as the codec sent it. None of the plugin's ToObject
        // targets has a date in a string property today.
        oldResult.LastShutdownTime!.Value.Should().NotBe("2026-10-06T20:20:52Z");
        newResult.LastShutdownTime!.Value.Should().Be("2026-10-06T20:20:52Z");
    }

    [Fact]
    public void ResultId_Is_Read_When_Present()
    {
        var message = LoadNew(@"{ ""CommandResponse"": {}, ""ResultId"": ""c3b70fb8-36f4-47dc-9086-0b4b00000000"" }");

        CodecJson.ResultIdOrEmpty(message).Should().Be("c3b70fb8-36f4-47dc-9086-0b4b00000000");
    }

    [Fact]
    public void ResultId_Is_An_Empty_Guid_When_Absent()
    {
        CodecJson.ResultIdOrEmpty(LoadNew(@"{ ""Status"": {} }")).Should().Be(Guid.Empty.ToString());
    }

    [Fact]
    public void PopulateFromToken_Rejects_Null_Arguments()
    {
        var act1 = () => CodecJson.PopulateFromToken(null!, new Model());
        var act2 = () => CodecJson.PopulateFromToken(new JObject(), null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    private sealed class BookingTime { public DateTime Value { get; set; } }

    [Fact]
    public void Booking_Times_From_The_Codec_Are_Utc_And_Are_Shown_In_Local_Time()
    {
        // #139: the codec sends "2026-10-21T18:00:00Z". Read through the parse path it is a UTC
        // DateTime; used as-is, meetings showed at the UTC clock time (4 h off in Eastern time).
        var time = new BookingTime();
        CodecJson.PopulateFromToken(JToken.Parse(@"{ ""Value"": ""2026-10-21T18:00:00Z"" }"), time);

        time.Value.Kind.Should().Be(DateTimeKind.Utc);
        var local = CodecJson.ToLocal(time.Value);
        local.Kind.Should().Be(DateTimeKind.Local);
        local.Should().Be(new DateTime(2026, 10, 21, 18, 0, 0, DateTimeKind.Utc).ToLocalTime());
    }

    [Fact]
    public void Local_And_Unspecified_Times_Are_Left_Alone()
    {
        var local = new DateTime(2026, 10, 21, 14, 0, 0, DateTimeKind.Local);
        var unspecified = new DateTime(2026, 10, 21, 14, 0, 0, DateTimeKind.Unspecified);

        CodecJson.ToLocal(local).Should().Be(local);
        CodecJson.ToLocal(unspecified).Should().Be(unspecified);
        CodecJson.ToLocal(unspecified).Kind.Should().Be(DateTimeKind.Unspecified);
    }
}
