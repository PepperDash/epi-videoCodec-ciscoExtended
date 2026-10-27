using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>ExecuteSwitch selectors (#121): JSON gives long or string, routing ports give int.</summary>
public class RoutingSelectorTests
{
    [Theory]
    [InlineData(3)]
    [InlineData(3L)]
    [InlineData(3u)]
    [InlineData((short)3)]
    [InlineData((byte)3)]
    [InlineData(3.0)]
    [InlineData("3")]
    [InlineData(" 3 ")]
    public void Whole_Numbers_Of_Any_Type_Are_Inputs(object selector)
    {
        RoutingSelector.TryGetInput(selector, out var input).Should().BeTrue();
        input.Should().Be(3);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("three")]
    [InlineData("")]
    [InlineData(3.5)]
    [InlineData(5000000000L)]
    [InlineData(true)]
    [InlineData('3')]
    public void Anything_Else_Is_Not(object? selector)
    {
        RoutingSelector.TryGetInput(selector, out _).Should().BeFalse();
    }

    [Fact]
    public void A_Selector_From_Json_Is_A_Long_And_Works()
    {
        // devjson {"params":[3]} deserializes the number as long.
        var selector = Newtonsoft.Json.Linq.JArray.Parse("[3]")[0].ToObject<object>();
        selector.Should().BeOfType<long>();
        RoutingSelector.TryGetInput(selector, out var input).Should().BeTrue();
        input.Should().Be(3);
    }
}
