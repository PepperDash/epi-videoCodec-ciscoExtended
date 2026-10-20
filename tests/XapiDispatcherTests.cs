using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>XapiDispatcher (#129, stage 4). No Crestron dependencies, so it is compiled into this project.</summary>
public class XapiDispatcherTests
{
    private const string StatusDoc = "{ \"Status\": { \"Audio\": { \"Volume\": { \"Value\": \"49\" } } } }";
    private const string PhonebookReply =
        "{ \"CommandResponse\": { \"PhonebookSearchResult\": { \"status\": \"OK\" } }, \"ResultId\": \"abc\" }";

    [Fact]
    public void Handlers_Get_The_Subtree_At_Their_Path()
    {
        var d = new XapiDispatcher();
        JToken? got = null;
        d.On("Status/Audio/Volume", (t, _) => got = t);

        d.Dispatch(StatusDoc).Should().Be(1);

        got!["Value"]!.Value<string>().Should().Be("49");
    }

    [Fact]
    public void Absent_Paths_Are_Not_Called()
    {
        var d = new XapiDispatcher();
        var called = new List<string>();
        d.On("Status", (_, _) => called.Add("Status"));
        d.On("Configuration", (_, _) => called.Add("Configuration"));
        d.On("Event", (_, _) => called.Add("Event"));
        d.On("CommandResponse", (_, _) => called.Add("CommandResponse"));

        d.Dispatch(StatusDoc);

        called.Should().Equal("Status");
    }

    [Fact]
    public void Handlers_Run_In_Registration_Order_And_A_Path_Can_Have_Several()
    {
        var d = new XapiDispatcher();
        var order = new List<int>();
        d.On("Status", (_, _) => order.Add(1));
        d.On("Status/Audio", (_, _) => order.Add(2));
        d.On("Status", (_, _) => order.Add(3));

        d.Dispatch(StatusDoc).Should().Be(3);

        order.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void ResultId_Is_Passed_With_The_Message()
    {
        var d = new XapiDispatcher();
        string? resultId = "unset";
        d.On("CommandResponse/PhonebookSearchResult", (_, m) => resultId = m.ResultId);

        d.Dispatch(PhonebookReply);
        resultId.Should().Be("abc");

        d.Dispatch("{ \"CommandResponse\": { \"PhonebookSearchResult\": {} } }");
        resultId.Should().BeNull("a reply to an untagged request carries no ResultId");
    }

    [Fact]
    public void Every_Top_Level_Object_In_A_Document_Is_Dispatched()
    {
        var d = new XapiDispatcher();
        var n = 0;
        d.On("Status", (_, _) => n++);

        d.Dispatch(StatusDoc + "\n" + StatusDoc).Should().Be(2);

        n.Should().Be(2);
    }

    [Fact]
    public void A_Failing_Handler_Does_Not_Stop_The_Others()
    {
        var d = new XapiDispatcher();
        var failures = new List<string>();
        var ran = false;
        d.HandlerFailed += (path, _) => failures.Add(path);
        d.On("Status", (_, _) => throw new InvalidOperationException("boom"));
        d.On("Status/Audio", (_, _) => ran = true);

        d.Dispatch(StatusDoc);

        failures.Should().Equal("Status");
        ran.Should().BeTrue();
    }

    [Fact]
    public void A_Path_Through_A_Non_Object_Is_Not_Matched()
    {
        var d = new XapiDispatcher();
        var called = false;
        d.On("Status/Audio/Volume/Value/Deeper", (_, _) => called = true);

        d.Dispatch(StatusDoc);

        called.Should().BeFalse();
    }

    [Fact]
    public void Invalid_Json_Throws_For_The_Caller_To_Log()
    {
        var d = new XapiDispatcher();
        d.On("Status", (_, _) => { });

        var act = () => d.Dispatch("{ \"Status\" { } }");

        act.Should().Throw<JsonReaderException>();
    }

    [Fact]
    public void Dates_Stay_As_The_Codec_Sent_Them()
    {
        var d = new XapiDispatcher();
        string? value = null;
        d.On("Status/SystemUnit/LastShutdownTime", (t, _) => value = t["Value"]!.ToString());

        d.Dispatch("{ \"Status\": { \"SystemUnit\": { \"LastShutdownTime\": { \"Value\": \"2026-10-06T20:20:52Z\" } } } }");

        value.Should().Be("2026-10-06T20:20:52Z");
    }

    [Fact]
    public void Registration_Is_Validated()
    {
        var d = new XapiDispatcher();
        ((Action)(() => d.On("", (_, _) => { }))).Should().Throw<ArgumentException>();
        ((Action)(() => d.On("Status", null!))).Should().Throw<ArgumentNullException>();
        d.On("Status", (_, _) => { });
        d.Paths.Should().Equal("Status");
    }
}
