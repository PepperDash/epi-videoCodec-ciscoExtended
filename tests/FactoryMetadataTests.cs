using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

public class FactoryMetadataTests
{
    // Bare "3.0.0", NOT the prerelease package version — Global.IsRunningMinimumVersionOrHigher parses
    // this with `new Version(...)`, which throws on a prerelease suffix (caught → gate returns false →
    // plugin silently never loads).
    private const string ExpectedMinimumVersion = "3.0.0";

    public static IEnumerable<object[]> FactoryTypeNames() => new[]
    {
        new object[] { "CiscoNetworkCameraManagerFactory", "ciscocameramanager" },
        new object[] { "CiscoNetworkCameraManagerFactory", "cameramanager" },
        new object[] { "CiscoCameraFactory", "ciscocamera" },
        new object[] { "CiscoCodecFactory", "ciscoRoomOS" },
        new object[] { "CiscoCodecFactory", "ciscoRoomBar" },
        new object[] { "CiscoCodecFactory", "ciscoRoomBarPro" },
        new object[] { "CiscoCodecFactory", "ciscoCodecEq" },
        new object[] { "CiscoCodecFactory", "ciscoCodecPro" },
        new object[] { "UserInterfaceFactory", "ciscoRoomOsMobileControl" },
    };

    [Theory]
    [InlineData("CiscoNetworkCameraManagerFactory")]
    [InlineData("CiscoCameraFactory")]
    [InlineData("CiscoCodecFactory")]
    [InlineData("UserInterfaceFactory")]
    public void Factory_Source_Sets_MinimumEssentialsFrameworkVersion(string factoryName)
    {
        var source = AssemblyFixture.FindSourceForClass(factoryName);
        source.Should().NotBeNull($"source for {factoryName} must exist");
        source!.Should().Contain($"MinimumEssentialsFrameworkVersion = \"{ExpectedMinimumVersion}\"",
            $"{factoryName} must gate on Essentials {ExpectedMinimumVersion} (bare version, no prerelease suffix)");
    }

    [Theory]
    [InlineData("CiscoNetworkCameraManagerFactory")]
    [InlineData("CiscoCameraFactory")]
    [InlineData("CiscoCodecFactory")]
    [InlineData("UserInterfaceFactory")]
    public void Factory_Source_Sets_TypeNames(string factoryName)
    {
        var source = AssemblyFixture.FindSourceForClass(factoryName);
        source.Should().NotBeNull($"source for {factoryName} must exist");
        source!.Should().Contain("TypeNames = new List<string>", $"{factoryName} must register its TypeNames");
    }

    [Theory]
    [MemberData(nameof(FactoryTypeNames))]
    public void Factory_Source_Contains_TypeName(string factoryName, string typeName)
    {
        var source = AssemblyFixture.FindSourceForClass(factoryName);
        source.Should().NotBeNull($"source for {factoryName} must exist");
        source!.Should().Contain($"\"{typeName}\"", $"{factoryName} must register device type '{typeName}'");
    }

    [Fact]
    public void No_Duplicate_TypeNames_Across_Factories()
    {
        FactoryTypeNames().Select(row => (string)row[1]).ToList()
            .Should().OnlyHaveUniqueItems("each device type name must map to exactly one factory");
    }
}
