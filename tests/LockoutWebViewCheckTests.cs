using System;
using System.Linq;
using FluentAssertions;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Navigator;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

public class LockoutWebViewCheckTests
{
    private const string Lockout = "http://10.0.0.1:50002/mc/app/lockout?token=abc&primaryRoomName=Room%20B";
    private const string Audio = "http://10.0.0.1:50002/mc/app/audio?token=abc";

    private static LockoutWebViewState Eval(params (string Status, string Target, string Url)[] views) =>
        LockoutWebViewCheck.Evaluate(views, "/mc/app/lockout");

    [Fact]
    public void Lockout_On_The_Controller_Is_Showing() =>
        Eval(("Visible", "Controller", Lockout)).Should().Be(LockoutWebViewState.Showing);

    [Fact]
    public void Another_Visible_Page_Alone_Is_Not_Showing() =>
        // The field case: an audio page the codec still reports Visible, nothing else.
        Eval(("Visible", "Controller", Audio)).Should().Be(LockoutWebViewState.NotShowing);

    [Fact]
    public void Lockout_On_Top_Of_Another_Page_Is_Showing() =>
        Eval(("Visible", "Controller", Audio), ("Visible", "Controller", Lockout)).Should().Be(LockoutWebViewState.Showing);

    [Fact]
    public void A_NotVisible_Lockout_Does_Not_Count() =>
        Eval(("NotVisible", "Controller", Lockout), ("Visible", "Controller", Audio)).Should().Be(LockoutWebViewState.NotShowing);

    [Fact]
    public void A_Lockout_Shown_On_Its_Configured_Target_Counts() =>
        LockoutWebViewCheck.Evaluate(new[] { ("Visible", "OSD", Lockout) }, "/mc/app/lockout", "OSD").Should().Be(LockoutWebViewState.Showing);

    [Fact]
    public void A_Lockout_On_The_Room_Display_Does_Not_Count_For_The_Navigator() =>
        Eval(("Visible", "OSD", Lockout), ("Visible", "Controller", Audio)).Should().Be(LockoutWebViewState.NotShowing);

    [Fact]
    public void Nothing_On_The_Controller_Is_Not_Showing() =>
        Eval(("Visible", "OSD", Audio)).Should().Be(LockoutWebViewState.NotShowing);

    [Fact]
    public void Partial_Feedback_Without_A_Url_Is_Unknown() =>
        // /Status/UserInterface/WebView/Status feedback carries id and Status only.
        Eval(("Visible", null!, null!)).Should().Be(LockoutWebViewState.Unknown);

    [Fact]
    public void A_Missing_Target_Means_Controller() =>
        // Firmware that doesn't report Target in a full reply.
        Eval(("Visible", null!, Lockout)).Should().Be(LockoutWebViewState.Showing);

    [Fact]
    public void Any_Entry_Without_A_Url_Makes_The_Message_Partial() =>
        Eval(("NotVisible", null!, null!), ("Visible", "Controller", Lockout)).Should().Be(LockoutWebViewState.Unknown);

    [Fact]
    public void A_Lockout_Still_Loading_Counts_But_No_Status_Does_Not()
    {
        Eval(("Loading", "Controller", Lockout)).Should().Be(LockoutWebViewState.Showing);
        Eval((null!, "Controller", Lockout)).Should().Be(LockoutWebViewState.NotShowing);
    }

    [Fact]
    public void A_Longer_Path_Ending_The_Same_Does_Not_Match() =>
        Eval(("Visible", "Controller", "http://h/mc/app/combinationlockout?token=x")).Should().Be(LockoutWebViewState.NotShowing);

    [Fact]
    public void A_Slashless_Configured_Path_Is_Matched_As_Built_Not_As_A_Suffix()
    {
        // mcPath "lockout" is appended without a slash: GetMobileControlUrl builds /mc/applockout.
        LockoutWebViewCheck.Evaluate(new[] { ("Visible", "Controller", "http://h/mc/app/combinationlockout?token=x") }, "/mc/applockout")
            .Should().Be(LockoutWebViewState.NotShowing);
        LockoutWebViewCheck.Evaluate(new[] { ("Visible", "Controller", "http://h/mc/applockout?token=x") }, "/mc/applockout")
            .Should().Be(LockoutWebViewState.Showing);
    }

    [Fact]
    public void The_Same_Page_Under_Another_App_Path_Does_Not_Match() =>
        Eval(("Visible", "Controller", "http://h/other/app/lockout?token=x")).Should().Be(LockoutWebViewState.NotShowing);

    [Fact]
    public void A_Query_In_The_Configured_Path_Matches_The_Escaped_Url() =>
        // The configured path is escaped into the URL path: /lockout%3Fmode=fire?token=...
        LockoutWebViewCheck.Evaluate(new[] { ("Visible", "Controller", "http://h/mc/app/lockout%3Fmode=fire?token=x") }, "/mc/app/lockout%3Fmode=fire")
            .Should().Be(LockoutWebViewState.Showing);

    [Theory]
    [InlineData("/mc/app/lockout")]
    [InlineData("/mc/app/lockout/")]
    [InlineData("/MC/APP/LOCKOUT")]
    [InlineData(" /mc/app/lockout ")]
    public void Path_Matching_Ignores_Slashes_Case_And_Query(string lockoutPath) =>
        LockoutWebViewCheck.Evaluate(new[] { ("Visible", "Controller", Lockout) }, lockoutPath).Should().Be(LockoutWebViewState.Showing);

    [Fact]
    public void Percent_Encoded_Paths_Match() =>
        LockoutWebViewCheck.Evaluate(new[] { ("Visible", "Controller", "http://h/mc/app/fire%20alarm?token=x") }, "/mc/app/fire%20alarm")
            .Should().Be(LockoutWebViewState.Showing);

    [Fact]
    public void A_Custom_Lockout_Path_Is_Matched_By_Path()
    {
        LockoutWebViewCheck.Evaluate(new[] { ("Visible", "Controller", Lockout) }, "/mc/app/fireAlarm").Should().Be(LockoutWebViewState.NotShowing);
        LockoutWebViewCheck.Evaluate(new[] { ("Visible", "Controller", "http://h/mc/app/fireAlarm?token=x") }, "/mc/app/fireAlarm").Should().Be(LockoutWebViewState.Showing);
    }

    [Fact]
    public void No_Views_Is_Not_Showing_And_No_Path_Is_Unknown()
    {
        LockoutWebViewCheck.Evaluate(Array.Empty<(string, string, string)>(), "/mc/app/lockout").Should().Be(LockoutWebViewState.NotShowing);
        LockoutWebViewCheck.Evaluate(new[] { ("Visible", "Controller", Lockout) }, "").Should().Be(LockoutWebViewState.Unknown);
        LockoutWebViewCheck.Evaluate(null!, "/mc/app/lockout").Should().Be(LockoutWebViewState.Unknown);
        LockoutWebViewCheck.Evaluate(new[] { ("Visible", "Controller", Lockout) }, "/").Should().Be(LockoutWebViewState.Unknown);
    }
}

public class LockoutRepairThrottleTests
{
    private static readonly DateTime T0 = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void First_Attempts_Go_Out_Then_At_Most_One_Per_Backoff_Interval()
    {
        var throttle = new LockoutRepairThrottle();
        DateTime last = T0;
        for (var i = 0; i < LockoutRepairThrottle.FreeAttempts; i++)
        {
            last = T0.AddSeconds(5 * i);
            throttle.CanAttempt(last).Should().BeTrue();
            throttle.Record(last).Should().Be(i + 1);
        }

        throttle.CanAttempt(last.AddSeconds(5)).Should().BeFalse();
        throttle.CanAttempt(last + LockoutRepairThrottle.BackoffInterval - TimeSpan.FromSeconds(1)).Should().BeFalse();
        throttle.CanAttempt(last + LockoutRepairThrottle.BackoffInterval).Should().BeTrue();
    }

    [Fact]
    public void Checking_Does_Not_Count_Only_Recording_Does()
    {
        // A push the debounce skipped must not use up the free attempts.
        var throttle = new LockoutRepairThrottle();
        for (var i = 0; i < 10; i++) throttle.CanAttempt(T0).Should().BeTrue();
        throttle.Attempts.Should().Be(0);
    }

    [Fact]
    public void Reset_Starts_Counting_Again()
    {
        var throttle = new LockoutRepairThrottle();
        for (var i = 0; i < LockoutRepairThrottle.FreeAttempts; i++) throttle.Record(T0);
        throttle.CanAttempt(T0.AddSeconds(1)).Should().BeFalse();

        throttle.Reset();
        throttle.Attempts.Should().Be(0);
        throttle.CanAttempt(T0.AddSeconds(2)).Should().BeTrue();
    }
}

/// <summary>
/// The codec reports web views as "UserInterface": { "WebView": [ ... ] }. Without these mappings the
/// list was never filled and only web view errors reached the lockout handler (lockout never re-popped
/// while any other page was open).
/// </summary>
public class WebViewStatusMappingTests
{
    private static bool HasJsonProperty(string typeFullName, string property, string jsonName)
    {
        var type = AssemblyFixture.PluginAssembly.GetTypes().FirstOrDefault(t => t.FullName == typeFullName);
        type.Should().NotBeNull($"{typeFullName} should exist in the plugin");
        var prop = type!.GetProperty(property);
        prop.Should().NotBeNull($"{typeFullName}.{property} should exist");
        return prop!.CustomAttributes.Any(a => a.AttributeType.Name == "JsonPropertyAttribute"
            && a.ConstructorArguments.Any(arg => string.Equals(arg.Value?.ToString(), jsonName, StringComparison.Ordinal)));
    }

    [Fact]
    public void UserInterface_WebViews_Maps_The_Codecs_WebView_List() =>
        HasJsonProperty("PepperDash.Essentials.Plugin.CiscoRoomOsCodec.CiscoCodecStatus+UserInterface", "WebViews", "WebView")
            .Should().BeTrue();

    [Fact]
    public void WebView_Maps_Target() =>
        HasJsonProperty("PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.WebView.WebView", "Target", "Target")
            .Should().BeTrue();
}

/// <summary>
/// Each status message is read on its own, through the resolver the codec's model uses: a one-item list
/// sent as a single object is read, and ghost entries are dropped. Mirrors CiscoCodecStatus.UserInterface
/// and UserInterface.WebView.WebView.
/// </summary>
public class WebViewStatusMessageTests
{
    private class Val { public string Value { get; set; } = null!; }
    private class Wv
    {
        public Val Status { get; set; } = null!;
        [Newtonsoft.Json.JsonProperty("Target")] public Val Target { get; set; } = null!;
        [Newtonsoft.Json.JsonProperty("URL")] public Val URL { get; set; } = null!;
        public string id { get; set; } = null!;
    }
    private class Ui { [Newtonsoft.Json.JsonProperty("WebView")] public System.Collections.Generic.List<Wv> WebViews { get; set; } = null!; }

    private static readonly StateModelContractResolver Resolver = new();

    private static Ui Read(string json)
    {
        var ui = new Ui();
        CodecJson.PopulateFromToken(Newtonsoft.Json.Linq.JToken.Parse(json), ui, Resolver.Settings);
        return ui;
    }

    private static LockoutWebViewState Eval(Ui ui) =>
        LockoutWebViewCheck.Evaluate((ui.WebViews ?? new()).Select(v => (v.Status?.Value, v.Target?.Value, v.URL?.Value)), "/mc/app/lockout");

    [Fact]
    public void A_Full_Reply_With_The_Stale_Page_And_The_Lockout_Shows_The_Lockout() =>
        Eval(Read(@"{ ""WebView"": [
            { ""id"": ""1"", ""Status"": { ""Value"": ""Visible"" }, ""Target"": { ""Value"": ""Controller"" }, ""URL"": { ""Value"": ""http://h/mc/app/audio?token=x"" } },
            { ""id"": ""2"", ""Status"": { ""Value"": ""Visible"" }, ""Target"": { ""Value"": ""Controller"" }, ""URL"": { ""Value"": ""http://h/mc/app/lockout?token=x"" } } ] }"))
        .Should().Be(LockoutWebViewState.Showing);

    [Fact]
    public void A_One_Item_List_Sent_As_An_Object_Is_Read() =>
        // The field case: only the stale audio page, so the lockout is pushed.
        Eval(Read(@"{ ""WebView"": { ""id"": ""1"", ""Status"": { ""Value"": ""Visible"" }, ""Target"": { ""Value"": ""Controller"" }, ""URL"": { ""Value"": ""http://h/mc/app/audio?token=x"" } } }"))
        .Should().Be(LockoutWebViewState.NotShowing);

    [Fact]
    public void Partial_Status_Feedback_Is_Unknown() =>
        Eval(Read(@"{ ""WebView"": [ { ""id"": ""1"", ""Status"": { ""Value"": ""NotVisible"" } } ] }"))
        .Should().Be(LockoutWebViewState.Unknown);

    [Fact]
    public void A_Ghost_Only_Message_Lists_Nothing() =>
        Read(@"{ ""WebView"": [ { ""ghost"": ""True"", ""id"": ""2"" } ] }").WebViews.Should().BeNullOrEmpty();
}

public class WebViewStatusCompatibilityTests
{
    [Fact]
    public void Obsolete_UiWebView_Keeps_Its_Public_Setter()
    {
        var type = AssemblyFixture.PluginAssembly.GetTypes()
            .First(t => t.FullName == "PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.WebView.WebViewStatus");
        var prop = type.GetProperty("UiWebView");
        prop.Should().NotBeNull();
        prop!.SetMethod.Should().NotBeNull("callers that assign UiWebView must keep compiling");
        prop.SetMethod!.IsPublic.Should().BeTrue();
    }
}
