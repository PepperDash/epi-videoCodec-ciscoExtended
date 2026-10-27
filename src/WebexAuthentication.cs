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

        /// <summary>
        /// The roles the plugin offers. The codec's AuthenticationResponse accepts only Host, Guest and
        /// Panelist: a webinar attendee joins as Guest with the webinar password (bench 2026-10-21:
        /// <c>ParticipantRole: Attendee</c> gave "Invalid value"; Guest connected and was reported as Guest).
        /// </summary>
        public static readonly string[] Roles = { "Host", "Guest", "Panelist", "Attendee" };

        /// <summary>The ParticipantRole value the codec expects for a role.</summary>
        public static string CodecRole(string role) => role == "Attendee" ? "Guest" : role;

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
        /// Tracks one PIN prompt from request to outcome. <c>PinEntered</c> only means a PIN was
        /// submitted: the codec follows it with <c>PinError</c> when the PIN is wrong, or clears the
        /// call's AuthenticationRequest to <c>None</c> when it was accepted (~1 s later, with the call
        /// going Connected). "Joined" is reported only then (#42: it fired on <c>PinEntered</c>, so a
        /// rejected host key still showed as joined).
        /// </summary>
        public sealed class PinFlow
        {
            public string CallId { get; private set; } = string.Empty;
            public string RequestType { get; private set; } = None;
            /// <summary>The role of a PIN submitted and not yet accepted or rejected.</summary>
            public string? PendingRole { get; private set; }
            private bool _pendingAtWebinar;
            // The call the pending PIN was submitted on
            private string _pendingCallId = string.Empty;
            public bool Requested => IsRequested(RequestType);

            /// <summary>A Status AuthenticationRequest. Returns the role that has now joined, or null.</summary>
            public string? OnRequest(string callId, string requestType)
            {
                CallId = callId;
                RequestType = requestType;
                // A PIN submitted on a call that ended before the codec accepted it: a later call's
                // status must not report that role as joined
                if (PendingRole != null && callId != _pendingCallId)
                    PendingRole = null;
                if (IsRequested(requestType) || PendingRole == null)
                    return null;
                var joined = PendingRole;
                PendingRole = null;
                // The codec reports a webinar attendee as Guest.
                return joined == "Guest" && _pendingAtWebinar ? "Attendee" : joined;
            }

            /// <summary>An AuthenticationResponse event (from <see cref="ReadResponse"/>). True when the PIN was rejected.</summary>
            public bool OnResponse(string? result)
            {
                if (result == null)
                    return false;
                if (result == "PinError")
                {
                    PendingRole = null;
                    return true;
                }
                PendingRole = result;
                _pendingCallId = CallId;
                _pendingAtWebinar = IsWebinar(RequestType);
                return false;
            }
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
            return "xCommand Conference Call AuthenticationResponse CallId: " + callId + " ParticipantRole: " + CodecRole(role) + pinPart;
        }
    }
}
