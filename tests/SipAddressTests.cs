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
}
