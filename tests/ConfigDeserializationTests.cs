using System.Reflection;
using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

public class ConfigDeserializationTests
{
    public static readonly string[] ConfigClasses =
    {
        "CiscoCodecConfig",
        "CameraManagerPropertiesConfig",
        "CiscoCodecCameraPropertiesConfig",
        "UserInterfaceConfig",
        "NavigatorConfig",
        "WebViewDisplayConfig",
    };

    // (config class, expected [JsonProperty("name")] on that type)
    public static IEnumerable<object[]> JsonProperties() => new[]
    {
        new object[] { "CiscoCodecConfig", "phonebookMode" },
        new object[] { "CiscoCodecConfig", "favorites" },
        new object[] { "CiscoCodecConfig", "showSelfViewByDefault" },
        new object[] { "CiscoCodecConfig", "externalSourceListEnabled" },
        new object[] { "CiscoCodecConfig", "communicationMonitorProperties" },
    };

    private static Type? FindType(string simpleName) =>
        AssemblyFixture.PluginAssembly.GetTypes().FirstOrDefault(t => t.Name == simpleName);

    [Theory]
    [InlineData("CiscoCodecConfig")]
    [InlineData("CameraManagerPropertiesConfig")]
    [InlineData("CiscoCodecCameraPropertiesConfig")]
    [InlineData("UserInterfaceConfig")]
    [InlineData("NavigatorConfig")]
    [InlineData("WebViewDisplayConfig")]
    public void Config_Class_Exists(string className)
    {
        FindType(className).Should().NotBeNull($"config class '{className}' must exist in the plugin");
    }

    [Theory]
    [InlineData("CiscoCodecConfig")]
    [InlineData("CameraManagerPropertiesConfig")]
    [InlineData("CiscoCodecCameraPropertiesConfig")]
    [InlineData("UserInterfaceConfig")]
    [InlineData("NavigatorConfig")]
    [InlineData("WebViewDisplayConfig")]
    public void Config_Has_Parameterless_Constructor(string className)
    {
        var type = FindType(className);
        type.Should().NotBeNull();
        type!.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Any(c => c.GetParameters().Length == 0)
            .Should().BeTrue($"{className} needs a parameterless constructor for Newtonsoft.Json deserialization");
    }

    [Theory]
    [MemberData(nameof(JsonProperties))]
    public void Config_Property_Has_JsonPropertyAttribute(string className, string jsonName)
    {
        var type = FindType(className);
        type.Should().NotBeNull();

        var hasAttribute = type!.GetProperties()
            .Any(p => p.CustomAttributes.Any(a =>
                a.AttributeType.Name == "JsonPropertyAttribute"
                && a.ConstructorArguments.Any(arg =>
                    string.Equals(arg.Value?.ToString(), jsonName, StringComparison.Ordinal))));

        hasAttribute.Should().BeTrue($"{className} must expose a [JsonProperty(\"{jsonName}\")]");
    }
}
