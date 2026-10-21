using System;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>What the codec's serial login sent, as seen by <see cref="SerialLoginDetector"/>.</summary>
    public enum SerialLoginSignal
    {
        None,
        /// <summary>"login: " with no line ending: the session is waiting for a username.</summary>
        LoginPrompt,
        /// <summary>"Password: " with no line ending: the session is waiting for a password.</summary>
        PasswordPrompt,
        /// <summary>"Login incorrect": text sent at the prompt was rejected as credentials.</summary>
        LoginIncorrect,
        /// <summary>"Account locked due to N failed logins".</summary>
        AccountLocked,
    }

    /// <summary>
    /// Spots the codec's serial login prompts in the raw receive text (#115). With
    /// <c>xConfiguration SerialPort LoginRequired: On</c> the RS-232 session drops to a getty-style
    /// login. Its prompts have no line ending, so the line gather never delivers them; this looks at
    /// the unfinished text after the last line feed instead. Not thread-safe: feed it from one thread.
    /// </summary>
    public sealed class SerialLoginDetector
    {
        // Longer than any prompt; an unfinished line past this is a JSON or text line still arriving.
        private const int MaxTailLength = 64;

        private string _tail = string.Empty;
        private bool _tailIsMidLine;

        /// <summary>Raised for each signal found, in order.</summary>
        public event Action<SerialLoginSignal> SignalReceived;

        /// <summary>Feeds raw text exactly as received from the port.</summary>
        public void Feed(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var start = 0;
            int lf;
            while ((lf = text.IndexOf('\n', start)) >= 0)
            {
                var line = _tailIsMidLine ? string.Empty : _tail + text.Substring(start, lf - start);
                _tail = string.Empty;
                _tailIsMidLine = false;
                start = lf + 1;

                var signal = ClassifyLine(line);
                if (signal != SerialLoginSignal.None)
                    Raise(signal);
            }

            if (_tailIsMidLine)
                return;

            _tail += text.Substring(start);
            if (_tail.Length > MaxTailLength)
            {
                _tail = string.Empty;
                _tailIsMidLine = true;
                return;
            }

            var prompt = ClassifyPrompt(_tail);
            if (prompt != SerialLoginSignal.None)
            {
                // Signal a prompt once; the next text starts a new line as far as prompts go.
                _tail = string.Empty;
                _tailIsMidLine = true;
                Raise(prompt);
            }
        }

        /// <summary>Forgets any unfinished line (for example after a reconnect).</summary>
        public void Reset()
        {
            _tail = string.Empty;
            _tailIsMidLine = false;
        }

        /// <summary>Classifies an unfinished line: a login or password prompt, or nothing.</summary>
        public static SerialLoginSignal ClassifyPrompt(string tail)
        {
            var trimmed = tail.Trim();
            if (string.Equals(trimmed, "login:", StringComparison.OrdinalIgnoreCase))
                return SerialLoginSignal.LoginPrompt;
            if (string.Equals(trimmed, "Password:", StringComparison.OrdinalIgnoreCase))
                return SerialLoginSignal.PasswordPrompt;
            return SerialLoginSignal.None;
        }

        /// <summary>Classifies a complete line: a rejected login, a lockout, or nothing.</summary>
        public static SerialLoginSignal ClassifyLine(string line)
        {
            var trimmed = line.Trim();
            if (string.Equals(trimmed, "Login incorrect", StringComparison.OrdinalIgnoreCase))
                return SerialLoginSignal.LoginIncorrect;
            if (trimmed.StartsWith("Account locked", StringComparison.OrdinalIgnoreCase))
                return SerialLoginSignal.AccountLocked;
            return SerialLoginSignal.None;
        }

        private void Raise(SerialLoginSignal signal) => SignalReceived?.Invoke(signal);
    }
}
