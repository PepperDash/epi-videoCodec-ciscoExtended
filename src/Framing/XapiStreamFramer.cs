#nullable enable
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>What a line outside a JSON document is (#129, stage 2).</summary>
    public enum XapiLineKind
    {
        /// <summary>Empty or whitespace only.</summary>
        Empty,
        /// <summary>RoomOS text-mode output: *s, *r, *e or *c (includes "*r Login successful").</summary>
        TextModeOutput,
        /// <summary>"** end", "OK" or "ERROR" closing a text-mode reply.</summary>
        CommandEnd,
        /// <summary>A path in an xFeedback List reply ("/Status/Audio").</summary>
        FeedbackListPath,
        /// <summary>An echo of a command the plugin sent (xCommand, xStatus, ...).</summary>
        Echo,
        /// <summary>Anything else, such as the login banner text.</summary>
        Other,
    }

    /// <summary>
    /// Finds complete top-level JSON documents in the codec's output (JSON output mode over SSH or
    /// RS-232) and sorts the lines between them, however the text was split into chunks (#129).
    /// </summary>
    /// <remarks>
    /// <para>A document starts with a line whose first non-blank character is <c>{</c>, and ends when
    /// its braces balance. Braces and brackets inside JSON strings, including escaped quotes, are
    /// ignored. A one-line document such as <c>{}</c> is a document.</para>
    /// <para>Inside a document, a line that is wholly an echoed command is dropped, as the current
    /// framing does. A text-mode line (<c>*r Login successful</c>, <c>** end</c>, ...) at a line start
    /// outside a string means the codec started over, so the partial document is abandoned.</para>
    /// <para>Not thread-safe: feed it from one thread. Has no Crestron dependencies, so the test
    /// project compiles and tests it directly.</para>
    /// </remarks>
    public sealed class XapiStreamFramer
    {
        /// <summary>A document larger than this is abandoned (a full xConfiguration is about 96 KB).</summary>
        public const int MaxDocumentChars = 4 * 1024 * 1024;

        private static readonly Regex TextModeLine = new Regex(@"^\*[srec] ");
        private static readonly Regex CommandEndLine = new Regex(@"^(\*\* end|OK|ERROR)\s*$");
        // Quoted segments must be balanced: an echoed command quotes whole values (SearchString: "abc",
        // resultId="..."), while the tail of a JSON string broken across lines has an odd quote.
        private static readonly Regex EchoLine = new Regex(
            @"^\s*x(?:Command|Status|Configuration|Feedback|Preferences)\b(?:[^{}\[\]""]|""[^""]*"")*$",
            RegexOptions.IgnoreCase);

        private readonly StringBuilder _partialLine = new StringBuilder();
        private readonly StringBuilder _document = new StringBuilder();
        private int _depth;
        private bool _inString;
        private bool _escape;
        // Set when a document is abandoned part-way through: its remaining (indented) lines are dropped
        // rather than framed, until a line starts at column 0.
        private bool _droppingRemainder;

        /// <summary>A document has started (its first line has arrived).</summary>
        public event Action? DocumentStarted;

        /// <summary>A complete document, lines joined with "\n".</summary>
        public event Action<string>? DocumentReceived;

        /// <summary>A complete line outside any document (without its line ending).</summary>
        public event Action<XapiLineKind, string>? LineReceived;

        /// <summary>A partial document was abandoned; the argument says why.</summary>
        public event Action<string>? DocumentAbandoned;

        /// <summary>An echoed command line was dropped from inside a document.</summary>
        public event Action<string>? EchoDropped;

        /// <summary>A line of an abandoned document arrived after it was abandoned, and was dropped.</summary>
        public event Action<string>? RemainderDropped;

        /// <summary>
        /// A line inside a document ended inside a string. JSON strings can't contain a raw line
        /// break, so the text was damaged (e.g. a lost quote on a serial link). The string is closed
        /// at the line end, so the brace count stays right and framing recovers.
        /// </summary>
        public event Action<string>? UnterminatedString;

        /// <summary>True while a document has started and not yet ended.</summary>
        public bool InDocument => _depth > 0;

        /// <summary>Feed text exactly as it arrived; chunk boundaries don't matter.</summary>
        public void Feed(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var start = 0;
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] != '\n')
                    continue;

                _partialLine.Append(text, start, i - start);
                start = i + 1;
                var line = _partialLine.ToString().TrimEnd('\r');
                _partialLine.Clear();
                ProcessLine(line);
            }

            if (start < text.Length)
                _partialLine.Append(text, start, text.Length - start);
        }

        /// <summary>Drop any partial line or document, e.g. when the connection changes.</summary>
        public void Reset(string reason)
        {
            _partialLine.Clear();
            AbandonDocument(reason);
        }

        /// <summary>The kind of a line outside a document.</summary>
        public static XapiLineKind Classify(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return XapiLineKind.Empty;
            var trimmed = line.TrimStart();
            if (TextModeLine.IsMatch(trimmed)) return XapiLineKind.TextModeOutput;
            if (CommandEndLine.IsMatch(trimmed)) return XapiLineKind.CommandEnd;
            if (trimmed.StartsWith("/", StringComparison.Ordinal)) return XapiLineKind.FeedbackListPath;
            if (EchoLine.IsMatch(trimmed)) return XapiLineKind.Echo;
            return XapiLineKind.Other;
        }

        private void ProcessLine(string line)
        {
            if (_depth == 0 && _droppingRemainder && DropRemainderLine(line))
                return;

            if (_depth == 0)
            {
                var first = FirstNonBlank(line);
                if (first >= 0 && line[first] == '{')
                {
                    DocumentStarted?.Invoke();
                    ScanDocumentText(line, first);
                    return;
                }

                LineReceived?.Invoke(Classify(line), line);
                return;
            }

            if (!_inString)
            {
                if (EchoLine.IsMatch(line))
                {
                    EchoDropped?.Invoke(line);
                    return;
                }

                // A bare "Login successful" banner line; a JSON line can't start with it, so a value
                // such as "Value": "Login successful" doesn't match
                var trimmed = line.TrimStart();
                if (TextModeLine.IsMatch(trimmed) || CommandEndLine.IsMatch(trimmed)
                    || trimmed.StartsWith("login successful", StringComparison.OrdinalIgnoreCase))
                {
                    AbandonDocument("codec sent text-mode output in the middle of a JSON document");
                    LineReceived?.Invoke(Classify(line), line);
                    return;
                }
            }

            ScanDocumentText(line, 0);
        }

        // Adds line[from..] to the current document. If the document closes part-way through the
        // line, whatever follows is handled as a new line.
        private void ScanDocumentText(string line, int from)
        {
            for (var i = from; i < line.Length; i++)
            {
                var c = line[i];
                if (_inString)
                {
                    if (_escape) _escape = false;
                    else if (c == '\\') _escape = true;
                    else if (c == '"') _inString = false;
                    continue;
                }

                if (c == '"') _inString = true;
                else if (c == '{' || c == '[') _depth++;
                else if (c == '}' || c == ']')
                {
                    _depth--;
                    if (_depth == 0)
                    {
                        _document.Append(line, from, i - from + 1);
                        var document = _document.ToString();
                        _document.Clear();
                        DocumentReceived?.Invoke(document);

                        var rest = line.Substring(i + 1);
                        if (rest.Trim().Length > 0)
                            ProcessLine(rest);
                        return;
                    }

                    if (_depth < 0)
                    {
                        _depth = 0;
                        _inString = false;
                        _document.Clear();
                        LineReceived?.Invoke(Classify(line), line);
                        return;
                    }
                }
            }

            _document.Append(line, from, line.Length - from).Append('\n');
            if (_inString)
            {
                _inString = false;
                _escape = false;
                UnterminatedString?.Invoke(line);
            }

            if (_document.Length > MaxDocumentChars)
                AbandonDocument($"document larger than {MaxDocumentChars} characters");
        }

        private void AbandonDocument(string reason)
        {
            if (_depth == 0 && _document.Length == 0)
                return;

            _depth = 0;
            _inString = false;
            _escape = false;
            _document.Clear();
            _droppingRemainder = true;
            DocumentAbandoned?.Invoke(reason);
        }

        // RoomOS starts a top-level document at column 0 and indents everything inside it. After an
        // abandon, an indented line is the rest of the abandoned document: framing it would turn each
        // remaining array element (a "{" on its own line) into a fake top-level document. Returns true
        // when the line was dropped.
        private bool DropRemainderLine(string line)
        {
            var first = FirstNonBlank(line);
            if (first < 0)
                return false;

            if (first > 0)
            {
                RemainderDropped?.Invoke(line);
                return true;
            }

            _droppingRemainder = false;
            if (line[0] == '}' || line[0] == ']')
            {
                // The abandoned document's own closing brace
                RemainderDropped?.Invoke(line);
                return true;
            }

            return false;
        }

        private static int FirstNonBlank(string s)
        {
            for (var i = 0; i < s.Length; i++)
                if (!char.IsWhiteSpace(s[i]))
                    return i;
            return -1;
        }
    }
}
