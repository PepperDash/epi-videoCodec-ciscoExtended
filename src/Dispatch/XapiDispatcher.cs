#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>One top-level JSON object from the codec, as handed to dispatcher handlers.</summary>
    public sealed class XapiMessage
    {
        public XapiMessage(JObject root, string? resultId)
        {
            Root = root;
            ResultId = resultId;
        }

        /// <summary>The whole object (for example <c>{"Status": {...}, "ResultId": "..."}</c>).</summary>
        public JObject Root { get; }

        /// <summary>The request's <c>| resultId="…"</c>, or null when the codec sent none (most messages).</summary>
        public string? ResultId { get; }
    }

    /// <summary>
    /// Routes each top-level JSON object from the codec to the handlers registered for the paths it
    /// contains (#129, stage 4). Each message is parsed once; handlers get the subtree at their path.
    /// </summary>
    /// <remarks>
    /// <para>Paths are slash-separated property names from the root, for example <c>Status</c>,
    /// <c>Event/UserInterface/Extensions</c> or <c>CommandResponse/PhonebookSearchResult</c>.
    /// A handler is called only when its path is present. Handlers run in registration order.</para>
    /// <para>A handler that throws doesn't stop the others; <see cref="HandlerFailed"/> reports it.
    /// Invalid JSON throws from <see cref="Dispatch"/> (a <see cref="JsonReaderException"/>).</para>
    /// <para>Not thread-safe for registration; register everything before the first dispatch. Has no
    /// Crestron dependencies, so the test project compiles and tests it directly.</para>
    /// </remarks>
    public sealed class XapiDispatcher
    {
        private readonly List<Registration> _registrations = new List<Registration>();

        /// <summary>A handler threw: (its path, the exception).</summary>
        public event Action<string, Exception>? HandlerFailed;

        /// <summary>Register a handler for a path. The same path may have several handlers.</summary>
        public void On(string path, Action<JToken, XapiMessage> handler)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A path is required.", nameof(path));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _registrations.Add(new Registration(path, path.Split('/'), handler));
        }

        /// <summary>The registered paths, in order (for diagnostics and tests).</summary>
        public IReadOnlyList<string> Paths
        {
            get
            {
                var paths = new List<string>(_registrations.Count);
                foreach (var r in _registrations)
                    paths.Add(r.Path);
                return paths;
            }
        }

        /// <summary>
        /// Parses a framed document (one or more top-level objects) and calls the handlers whose paths
        /// are present. Returns the number of handler calls.
        /// </summary>
        public int Dispatch(string document)
        {
            var calls = 0;
            using (var reader = CodecJson.CreateMessageReader(new StringReader(document)))
            {
                // The framer passes one document at a time; accept several objects anyway.
                reader.SupportMultipleContent = true;
                while (reader.Read())
                {
                    if (reader.TokenType != JsonToken.StartObject)
                        continue;

                    var root = JObject.Load(reader);
                    var resultIdToken = root["ResultId"];
                    var message = new XapiMessage(root, resultIdToken?.ToString());
                    calls += Dispatch(message);
                }
            }

            return calls;
        }

        /// <summary>Calls the handlers whose paths are present in an already-parsed message.</summary>
        public int Dispatch(XapiMessage message)
        {
            var calls = 0;
            foreach (var registration in _registrations)
            {
                var token = Find(message.Root, registration.Segments);
                if (token == null)
                    continue;

                calls++;
                try
                {
                    registration.Handler(token, message);
                }
                catch (Exception ex)
                {
                    HandlerFailed?.Invoke(registration.Path, ex);
                }
            }

            return calls;
        }

        private static JToken? Find(JObject root, string[] segments)
        {
            JToken? current = root;
            foreach (var segment in segments)
            {
                if (!(current is JObject obj))
                    return null;
                current = obj[segment];
                if (current == null)
                    return null;
            }

            return current;
        }

        private sealed class Registration
        {
            public Registration(string path, string[] segments, Action<JToken, XapiMessage> handler)
            {
                Path = path;
                Segments = segments;
                Handler = handler;
            }

            public string Path { get; }
            public string[] Segments { get; }
            public Action<JToken, XapiMessage> Handler { get; }
        }
    }
}
