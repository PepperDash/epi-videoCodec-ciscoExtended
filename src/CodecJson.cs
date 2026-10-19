#nullable enable
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// Helpers for reading the codec's JSON once (#129, stage 1).
    /// </summary>
    /// <remarks>
    /// Has no Crestron dependencies, so the test project compiles and tests it directly.
    /// </remarks>
    public static class CodecJson
    {
        /// <summary>
        /// A reader for codec messages. Dates are left as the text the codec sent
        /// (<see cref="DateParseHandling.None"/>), so populating from a token gives every property the
        /// same value it got when the token was written back to text and parsed again. With the default
        /// handling, a date going into a <c>string</c> property would be formatted with the current
        /// culture instead.
        /// </summary>
        public static JsonTextReader CreateMessageReader(TextReader reader) =>
            new JsonTextReader(reader) { DateParseHandling = DateParseHandling.None };

        /// <summary>
        /// Populates <paramref name="target"/> from an already-parsed token. Same result as
        /// <c>JsonConvert.PopulateObject(token.ToString(), target, settings)</c>, without writing the
        /// token back to text and parsing it a second time.
        /// </summary>
        public static void PopulateFromToken(JToken token, object target, JsonSerializerSettings? settings = null)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));
            if (target == null) throw new ArgumentNullException(nameof(target));

            // CreateDefault on each call, as JsonConvert.PopulateObject does, so JsonConvert.DefaultSettings
            // is honoured the same way.
            var serializer = JsonSerializer.CreateDefault(settings);
            using (var reader = token.CreateReader())
                serializer.Populate(reader, target);
        }

        /// <summary>
        /// The message's top-level <c>ResultId</c> (the request's <c>| resultId="…"</c>), or an empty
        /// GUID when it has none, which is most messages. No exception on the common path.
        /// </summary>
        public static string ResultIdOrEmpty(JObject message)
        {
            var token = message?["ResultId"];
            return token != null ? token.ToString() : Guid.Empty.ToString();
        }
    }
}
