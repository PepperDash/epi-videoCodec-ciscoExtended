using FluentAssertions;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

public class FactoryDiscoveryTests
{
    public static readonly string[] ExpectedFactories =
    {
        "CiscoNetworkCameraManagerFactory",
        "CiscoCameraFactory",
        "CiscoCodecFactory",
        "UserInterfaceFactory",
    };

    [Fact]
    public void Assembly_Loads_Successfully()
    {
        var act = () => AssemblyFixture.PluginAssembly.GetTypes();
        act.Should().NotThrow("the plugin DLL must load under MetadataLoadContext");
    }

    [Fact]
    public void Assembly_Name_Matches_Expected()
    {
        AssemblyFixture.PluginAssembly.GetName().Name
            .Should().Be(AssemblyFixture.ExpectedAssemblyName);
    }

    [Fact]
    public void Factory_Count_Matches_Expected()
    {
        AssemblyFixture.FindFactoryTypes().Select(f => f.Name)
            .Should().BeEquivalentTo(ExpectedFactories);
    }

    [Theory]
    [InlineData("CiscoNetworkCameraManagerFactory")]
    [InlineData("CiscoCameraFactory")]
    [InlineData("CiscoCodecFactory")]
    [InlineData("UserInterfaceFactory")]
    public void Factory_Exists_ByName(string factoryName)
    {
        AssemblyFixture.FindFactoryTypes().Select(f => f.Name)
            .Should().Contain(factoryName);
    }

    [Fact]
    public void All_Factories_Have_Parameterless_Constructor()
    {
        foreach (var factory in AssemblyFixture.FindFactoryTypes())
        {
            factory.GetConstructors()
                .Any(c => c.GetParameters().Length == 0)
                .Should().BeTrue($"{factory.Name} must have a parameterless constructor for plugin discovery");
        }
    }
}
