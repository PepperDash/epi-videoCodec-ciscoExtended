using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>SIP URI for the bridge on a Webex-registered codec (#12). Shapes as sent by a Room Bar, RoomOS 26.10.</summary>
public class SipAddressTests
{
    private const string Address = "room@example.rooms.webex.com";

    private static JToken Status(string json) => JObject.Parse(json);

    [Fact]
    public void Contact_Info_Number_Is_Used()
    {
        var status = Status("{ \"UserInterface\": { \"ContactInfo\": { \"ContactMethod\": [ { \"Number\": { \"Value\": \"" + Address + "\" }, \"id\": \"1\" } ], \"Name\": { \"Value\": \"Room Bar\" } } } }");
        SipAddress.FromContactInfo(status).Should().Be(Address);
    }

    [Fact]
    public void Webex_Shared_Sip_Address_Is_Used_Without_Contact_Info()
    {
        var status = Status("{ \"Webex\": { \"Account\": { \"Personal\": { \"SipAddress\": { \"Value\": \"\" } }, \"Shared\": { \"SipAddress\": { \"Value\": \"" + Address + "\" } } } } }");
        SipAddress.FromContactInfo(status).Should().Be(Address);
    }

    [Fact]
    public void Document_Without_Contact_Info_Gives_Null()
    {
        SipAddress.FromContactInfo(Status("{ \"Standby\": { \"State\": { \"Value\": \"Off\" } } }")).Should().BeNull();
    }

    [Theory]
    [InlineData("sip:1234@pbx.example.com", Address, "sip:1234@pbx.example.com")]
    [InlineData("", Address, Address)]
    [InlineData(null, Address, Address)]
    [InlineData("", null, "Unknown")]
    public void Registration_Uri_Wins_Then_Contact_Address_Then_Unknown(string registrationUri, string contactAddress, string expected)
    {
        SipAddress.Resolve(registrationUri, contactAddress).Should().Be(expected);
    }

    [Fact]
    public void Document_With_The_Address_Is_Authoritative()
    {
        var status = Status("{ \"UserInterface\": { \"ContactInfo\": { \"ContactMethod\": [ { \"Number\": { \"Value\": \"" + Address + "\" }, \"id\": \"1\" } ] } } }");
        SipAddress.TryGetContactAddress(status, out var address).Should().BeTrue();
        address.Should().Be(Address);
    }

    [Fact]
    public void Document_Reporting_No_Address_Clears_It()
    {
        var status = Status("{ \"UserInterface\": { \"ContactInfo\": { \"ContactMethod\": [ { \"Number\": { \"Value\": \"\" }, \"id\": \"1\" } ] } }, \"Webex\": { \"Account\": { \"Personal\": { \"SipAddress\": { \"Value\": \"\" } }, \"Shared\": { \"SipAddress\": { \"Value\": \"\" } } } } }");
        SipAddress.TryGetContactAddress(status, out var address).Should().BeTrue();
        address.Should().BeNull();
    }

    [Theory]
    [InlineData("{ \"Standby\": { \"State\": { \"Value\": \"Off\" } } }")]
    [InlineData("{ \"UserInterface\": { \"ContactInfo\": { \"Name\": { \"Value\": \"Room Bar\" } } } }")]
    [InlineData("{ \"SIP\": { \"Registration\": [ { \"URI\": { \"Value\": \"\" }, \"id\": \"1\" } ] } }")]
    public void Partial_Update_Without_The_Address_Fields_Keeps_The_Cache(string json)
    {
        SipAddress.TryGetContactAddress(Status(json), out var address).Should().BeFalse();
        address.Should().BeNull();
    }
}
