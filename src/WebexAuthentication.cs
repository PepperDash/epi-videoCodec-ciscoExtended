#nullable enable
using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// Reads Webex meeting PIN prompts and builds the replies (#111, #42).
    /// </summary>
    /// <remarks>
    /// <para>The codec asks for a PIN with <c>Status Conference Call[n] AuthenticationRequest</c>, for
    /// example <c>HostPinOrGuest</c> (a meeting) or <c>PanelistPinOrAttendeePin</c> (a webinar). The
    /// reply is <c>xCommand Conference Call AuthenticationResponse CallId: &lt;id&gt; ParticipantRole:
    /// &lt;role&gt; [Pin: &lt;pin&gt;#]</c>, which must use the call's <c>id</c>, not its position in the
    /// list. The result arrives as <c>Event Conference Call AuthenticationResponse</c>.</para>
    /// <para>Has no Crestron dependencies, so the test project compiles and tests it directly.</para>
    /// </remarks>
    public static class WebexAuthentication
    {
        public const string None = "None";

        /// <summary>The roles the codec accepts in an AuthenticationResponse.</summary>
        public static readonly string[] Roles = { "Host", "Guest", "Panelist", "Attendee" };

        /// <summary>
        /// The first call in a <c>Status Conference</c> token that carries an AuthenticationRequest:
        /// its codec call id and the request type (<c>None</c> once answered). Null when no call has one.
        /// </summary>
        public static (string CallId, string RequestType)? ReadRequest(JToken? conferenceToken)
        {
            var calls = conferenceToken?["Call"];
            var items = calls is JArray array ? array.Children<JObject>() : calls is JObject single ? new[] { single } : Enumerable.Empty<JObject>();
            foreach (var call in items)
            {
                var type = call.SelectToken("AuthenticationRequest.Value")?.ToString();
                if (type == null)
                    continue;
                return (call["id"]?.ToString() ?? string.Empty, type);
            }

            return null;
        }

        /// <summary>True when the request type asks for a PIN (anything but <c>None</c>).</summary>
        public static bool IsRequested(string? requestType) =>
            !string.IsNullOrEmpty(requestType) && !string.Equals(requestType, None, StringComparison.OrdinalIgnoreCase);

        /// <summary>True when the request is a webinar's (panelist or attendee).</summary>
        public static bool IsWebinar(string? requestType) =>
            IsRequested(requestType) &&
            (requestType!.IndexOf("Panelist", StringComparison.OrdinalIgnoreCase) >= 0 ||
             requestType.IndexOf("Attendee", StringComparison.OrdinalIgnoreCase) >= 0);

        /// <summary>
        /// From an <c>Event Conference</c> token: the role the PIN was accepted for, <c>"PinError"</c>
        /// when it was rejected, or null when the token is neither. Missing fields are tolerated.
        /// </summary>
        public static string? ReadResponse(JToken? conferenceEventToken)
        {
            var response = conferenceEventToken?.SelectToken("Call.AuthenticationResponse");
            if (response == null)
                return null;
            if (response["PinError"] != null)
                return "PinError";
            var role = response.SelectToken("PinEntered.ParticipantRole.Value")?.ToString();
            return Roles.FirstOrDefault(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// The <c>AuthenticationResponse</c> command for a call and role, with the PIN when one is given.
        /// Host and Panelist need a PIN; Guest and Attendee may join without one.
        /// </summary>
        public static string ResponseCommand(string callId, string role, string? pin)
        {
            if (string.IsNullOrEmpty(callId)) throw new ArgumentException("A call id is required.", nameof(callId));
            if (!Roles.Contains(role)) throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown participant role.");
            var pinPart = string.IsNullOrEmpty(pin) ? string.Empty : " Pin: " + pin!.TrimEnd('#') + "#";
            return "xCommand Conference Call AuthenticationResponse CallId: " + callId + " ParticipantRole: " + role + pinPart;
        }
    }
}
