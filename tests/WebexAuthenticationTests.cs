using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec.Tests;

/// <summary>Webex PIN prompts and replies (#111, #42), on the shapes RoomOS sends.</summary>
public class WebexAuthenticationTests
{
    private static JToken Conference(string json) => JObject.Parse(json)["Status"]!["Conference"]!;

    [Fact]
    public void A_Webinar_Prompt_Gives_The_Codec_Call_Id_Not_The_List_Position()
    {
        // From the #111 report (Codec Pro, RoomOS 26.9): the call's id is 9; it is first in the list.
        var request = WebexAuthentication.ReadRequest(Conference(
            @"{""Status"":{""Conference"":{""Call"":[{""AuthenticationRequest"":{""Value"":""PanelistPinOrAttendeePin""},""id"":""9""}]}}}"));

        request.Should().NotBeNull();
        request!.Value.CallId.Should().Be("9");
        request.Value.RequestType.Should().Be("PanelistPinOrAttendeePin");
        WebexAuthentication.IsRequested(request.Value.RequestType).Should().BeTrue();
        WebexAuthentication.IsWebinar(request.Value.RequestType).Should().BeTrue();
    }

    [Theory]
    [InlineData("HostPinOrGuest", true, false)]
    [InlineData("HostPinOrGuestPin", true, false)]
    [InlineData("AnyHostPinOrGuestPin", true, false)]
    [InlineData("GuestPin", true, false)]
    [InlineData("PanelistPin", true, true)]
    [InlineData("PanelistPinOrAttendeePin", true, true)]
    [InlineData("None", false, false)]
    public void Every_Request_Type_But_None_Asks_For_A_Pin(string type, bool requested, bool webinar)
    {
        WebexAuthentication.IsRequested(type).Should().Be(requested);
        WebexAuthentication.IsWebinar(type).Should().Be(webinar);
    }

    [Fact]
    public void The_Call_With_The_Request_Is_Found_Among_Several_And_As_A_Single_Object()
    {
        WebexAuthentication.ReadRequest(Conference(
            @"{""Status"":{""Conference"":{""Call"":[{""id"":""3""},{""AuthenticationRequest"":{""Value"":""HostPinOrGuest""},""id"":""4""}]}}}"))!
            .Value.CallId.Should().Be("4");
        WebexAuthentication.ReadRequest(Conference(
            @"{""Status"":{""Conference"":{""Call"":{""AuthenticationRequest"":{""Value"":""None""},""id"":""4""}}}}"))!
            .Value.RequestType.Should().Be("None");
        WebexAuthentication.ReadRequest(Conference(@"{""Status"":{""Conference"":{""Presentation"":{}}}}")).Should().BeNull();
    }

    [Theory]
    [InlineData("Host")]
    [InlineData("Guest")]
    [InlineData("Panelist")]
    [InlineData("Attendee")]
    public void A_Joined_Response_Gives_The_Role(string role)
    {
        var evt = JObject.Parse(@"{""Call"":{""AuthenticationResponse"":{""PinEntered"":{""ParticipantRole"":{""Value"":""" + role + @"""},""CallId"":{""Value"":""9""}}}}}");

        WebexAuthentication.ReadResponse(evt).Should().Be(role);
    }

    [Fact]
    public void A_Pin_Error_And_Missing_Fields_Are_Handled()
    {
        WebexAuthentication.ReadResponse(JObject.Parse(@"{""Call"":{""AuthenticationResponse"":{""PinError"":{""CallId"":{""Value"":""9""}}}}}"))
            .Should().Be("PinError");
        // #111: ParticipantRole was read without a null check.
        WebexAuthentication.ReadResponse(JObject.Parse(@"{""Call"":{""AuthenticationResponse"":{""PinEntered"":{}}}}")).Should().BeNull();
        WebexAuthentication.ReadResponse(JObject.Parse(@"{""Call"":{}}")).Should().BeNull();
        WebexAuthentication.ReadResponse(null).Should().BeNull();
    }

    [Fact]
    public void Replies_Use_The_Call_Id_The_Role_And_An_Optional_Pin()
    {
        WebexAuthentication.ResponseCommand("9", "Panelist", "123456")
            .Should().Be("xCommand Conference Call AuthenticationResponse CallId: 9 ParticipantRole: Panelist Pin: 123456#");
        WebexAuthentication.ResponseCommand("9", "Attendee", null)
            .Should().Be("xCommand Conference Call AuthenticationResponse CallId: 9 ParticipantRole: Attendee");
        WebexAuthentication.ResponseCommand("9", "Host", "4321#")
            .Should().EndWith("ParticipantRole: Host Pin: 4321#", "a trailing # is not doubled");

        ((Action)(() => WebexAuthentication.ResponseCommand("", "Host", "1"))).Should().Throw<ArgumentException>();
        ((Action)(() => WebexAuthentication.ResponseCommand("9", "Presenter", null))).Should().Throw<ArgumentOutOfRangeException>();
    }
}
