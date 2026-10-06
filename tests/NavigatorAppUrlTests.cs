using System.Web;
using FluentAssertions;
using PepperDash.Essentials.Plugin.CiscoRoomOsCodec.UserInterface.Navigator;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>
/// The Navigator's Mobile Control app URL carries the configured <c>appUrlQueryParams</c>; every web
/// view it opens is built from that URL.
/// </summary>
public class NavigatorAppUrlTests
{
    private const string AppUrl = "http://10.0.0.5:50003/mc/app?token=abc-123";

    private static Dictionary<string, string> Params(params (string key, string value)[] items) =>
        items.ToDictionary(i => i.key, i => i.value);

    [Fact]
    public void AddsTheParameters_AndKeepsTheToken()
    {
        var result = NavigatorAppUrl.AddQueryParams(AppUrl, Params(("perfLabel", "navigator")));

        var uri = new Uri(result);
        uri.GetLeftPart(UriPartial.Path).Should().Be("http://10.0.0.5:50003/mc/app");
        var query = HttpUtility.ParseQueryString(uri.Query);
        query["token"].Should().Be("abc-123");
        query["perfLabel"].Should().Be("navigator");
    }

    [Fact]
    public void NeverOverwritesAParameterTheUrlAlreadyHas()
    {
        var result = NavigatorAppUrl.AddQueryParams(AppUrl, Params(("token", "replaced"), ("perfLabel", "navigator")));

        var query = HttpUtility.ParseQueryString(new Uri(result).Query);
        query["token"].Should().Be("abc-123");
        query["perfLabel"].Should().Be("navigator");
    }

    [Fact]
    public void WorksOnAUrlWithNoQueryString()
    {
        NavigatorAppUrl.AddQueryParams("http://10.0.0.5:50003/mc/app", Params(("perfLabel", "navigator")))
            .Should().Be("http://10.0.0.5:50003/mc/app?perfLabel=navigator");
    }

    [Fact]
    public void EncodesValues()
    {
        var result = NavigatorAppUrl.AddQueryParams(AppUrl, Params(("label", "board room & lobby")));

        HttpUtility.ParseQueryString(new Uri(result).Query)["label"].Should().Be("board room & lobby");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/mc/app?token=abc")]
    [InlineData("not a url")]
    public void LeavesAnythingButAnAbsoluteHttpUrlUnchanged(string? url)
    {
        NavigatorAppUrl.AddQueryParams(url!, Params(("perfLabel", "navigator"))).Should().Be(url);
    }

    [Fact]
    public void WithNoParameters_ReturnsTheUrlUnchanged()
    {
        NavigatorAppUrl.AddQueryParams(AppUrl, null!).Should().Be(AppUrl);
        NavigatorAppUrl.AddQueryParams(AppUrl, new Dictionary<string, string>()).Should().Be(AppUrl);
    }
}
