using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// Finds the codec's own SIP address for the SIP URI feedback (#12). A codec registered to a SIP
    /// server reports it as <c>Status/SIP/Registration[1]/URI</c>. A Webex-registered codec leaves that
    /// registration Inactive with an empty URI; its address is in <c>UserInterface/ContactInfo</c> and
    /// <c>Webex/Account</c> instead.
    /// </summary>
    public static class SipAddress
    {
        /// <summary>
        /// The address a Webex-registered codec publishes, from a status document (the object under
        /// "Status"), or null when the document doesn't carry one.
        /// </summary>
        public static string FromContactInfo(JToken status)
        {
            if (status == null)
                return null;

            var contactMethods = status.SelectToken("UserInterface.ContactInfo.ContactMethod") as JArray;
            var contactNumber = contactMethods?
                .Children<JObject>()
                .Where(o => (string)o["id"] == "1")
                .Select(o => (string)o.SelectToken("Number.Value"))
                .FirstOrDefault();

            return NullIfEmpty(contactNumber)
                ?? NullIfEmpty((string)status.SelectToken("Webex.Account.Shared.SipAddress.Value"))
                ?? NullIfEmpty((string)status.SelectToken("Webex.Account.Personal.SipAddress.Value"));
        }

        /// <summary>
        /// True when the status document carries the address fields (a contact method list or a Webex
        /// account SIP address), so it says what the address is now: <paramref name="address"/> is then
        /// the address, or null when the codec reports none. False when the document doesn't mention
        /// them (a partial update), so a cached address should be kept.
        /// </summary>
        public static bool TryGetContactAddress(JToken status, out string address)
        {
            address = null;
            if (status == null)
                return false;

            var carriesAddress = status.SelectToken("UserInterface.ContactInfo.ContactMethod") != null
                || status.SelectToken("Webex.Account.Shared.SipAddress") != null
                || status.SelectToken("Webex.Account.Personal.SipAddress") != null;
            if (!carriesAddress)
                return false;

            address = FromContactInfo(status);
            return true;
        }

        /// <summary>The SIP registration URI if there is one, else the contact address, else "Unknown".</summary>
        public static string Resolve(string registrationUri, string contactAddress) =>
            NullIfEmpty(registrationUri) ?? NullIfEmpty(contactAddress) ?? "Unknown";

        private static string NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
