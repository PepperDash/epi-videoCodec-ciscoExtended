#nullable enable
using System;
using System.Collections.Generic;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// Runs <see cref="XapiStreamFramer"/> alongside the plugin's current line-based framing and
    /// compares the documents the two produce (#129, stage 2). Measuring only: the plugin still parses
    /// what the current framing produces.
    /// </summary>
    /// <remarks>
    /// Feed every received line before the current framing sees it, then call <see cref="Compare"/>
    /// with each document the current framing completes. One-line empty documents (<c>{}</c>, the
    /// codec's reply to each feedback registration) are not counted, because the current framing
    /// ignores them. Not thread-safe; has no Crestron dependencies.
    /// </remarks>
    public sealed class FramingShadow
    {
        private const int MaxPending = 16;

        private readonly XapiStreamFramer _framer = new XapiStreamFramer();
        private readonly Queue<string> _pending = new Queue<string>();

        public FramingShadow()
        {
            _framer.DocumentReceived += OnDocument;
            _framer.DocumentAbandoned += _ => Abandoned++;
            _framer.EchoDropped += _ => EchoesDropped++;
        }

        /// <summary>Documents both framings produced identically.</summary>
        public long Matches { get; private set; }

        /// <summary>Documents the two framings produced differently.</summary>
        public long Mismatches { get; private set; }

        /// <summary>Documents only the current framing produced.</summary>
        public long Missing { get; private set; }

        /// <summary>Documents only the new framer produced (for example "{" not alone on its line).</summary>
        public long Extra { get; private set; }

        /// <summary>Empty one-line documents seen ("{}").</summary>
        public long EmptyDocuments { get; private set; }

        public long Abandoned { get; private set; }

        public long EchoesDropped { get; private set; }

        /// <summary>A difference: (current framing's document, new framer's document or null). Line endings removed.</summary>
        public event Action<string, string?>? Mismatch;

        public void Feed(string text) => _framer.Feed(text);

        public void Reset(string reason)
        {
            _framer.Reset(reason);
            _pending.Clear();
        }

        /// <summary>Compares a document the current framing produced; true if the new framer produced the same.</summary>
        public bool Compare(string currentDocument)
        {
            var current = Normalize(currentDocument);

            // Documents only the new framer produced sit ahead of this one in the queue.
            var skipped = 0;
            foreach (var pending in _pending)
            {
                if (pending == current)
                {
                    for (var i = 0; i < skipped; i++)
                        _pending.Dequeue();
                    Extra += skipped;
                    _pending.Dequeue();
                    Matches++;
                    return true;
                }
                skipped++;
            }

            if (_pending.Count == 0)
            {
                Missing++;
                Mismatch?.Invoke(current, null);
                return false;
            }

            Mismatches++;
            Mismatch?.Invoke(current, _pending.Dequeue());
            return false;
        }

        /// <summary>One line for the console.</summary>
        public string Summary() =>
            $"match={Matches} mismatch={Mismatches} missing={Missing} extra={Extra} " +
            $"empty={EmptyDocuments} abandoned={Abandoned} echoesDropped={EchoesDropped}";

        /// <summary>The form both framings' documents are compared in: line endings removed.</summary>
        public static string Normalize(string document) => document.Replace("\r", "").Replace("\n", "");

        private void OnDocument(string document)
        {
            if (document.Trim() == "{}")
            {
                EmptyDocuments++;
                return;
            }

            _pending.Enqueue(Normalize(document));
            while (_pending.Count > MaxPending)
            {
                _pending.Dequeue();
                Extra++;
            }
        }
    }
}
