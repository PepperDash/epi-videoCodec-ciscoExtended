#nullable enable
using System;
using System.Threading;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// Base for a codec value the plugin reacts to (a status, configuration or event value).
    /// <see cref="ValueChangedAction"/> is how feedbacks and other actions are wired to it.
    /// </summary>
    /// <remarks>
    /// <para><b>Status and configuration values</b> call <see cref="OnValueChanged(object)"/> from their
    /// setters: the action runs only when the codec's raw value actually changed (#129, stage 5). A
    /// re-sync, a poll reply or a repeated feedback with the same value no longer re-runs every
    /// wired action.</para>
    /// <para><b>Event values</b> call <see cref="OnValueChanged()"/>, which always runs the action:
    /// the same event value twice (a button pressed twice) is two events.</para>
    /// <para>Has no Crestron dependencies, so the test project compiles and tests it directly.</para>
    /// </remarks>
    public abstract class ValueProperty
    {
        private static long _notified;
        private static long _unchanged;

        private bool _hasValue;
        private object? _lastValue;

        /// <summary>Triggered when the value changes (status/configuration), or on every set (events).</summary>
        public Action? ValueChangedAction { get; set; }

        /// <summary>
        /// A <see cref="ValueChangedAction"/> threw: (the value, the exception). Actions run inside a
        /// populate of the whole state model (#129, stage 6d), so an exception is reported here
        /// instead of stopping the rest of the document being populated.
        /// </summary>
        public static event Action<ValueProperty, Exception>? ActionFailed;

        /// <summary>Actions run because a status/configuration value changed (all instances).</summary>
        public static long NotifiedCount => Interlocked.Read(ref _notified);

        /// <summary>Sets of a status/configuration value that left it unchanged, so no action ran (all instances).</summary>
        public static long UnchangedCount => Interlocked.Read(ref _unchanged);

        /// <summary>Clears <see cref="NotifiedCount"/> and <see cref="UnchangedCount"/>.</summary>
        public static void ResetCounts()
        {
            Interlocked.Exchange(ref _notified, 0);
            Interlocked.Exchange(ref _unchanged, 0);
        }

        /// <summary>Runs the action every time. For event values.</summary>
        protected void OnValueChanged()
        {
            var action = ValueChangedAction;
            if (action != null)
                Run(action);
        }

        /// <summary>
        /// Runs the action only when <paramref name="newValue"/> differs from the last value set
        /// (or on the first set). For status and configuration values; pass the setter's raw value.
        /// </summary>
        protected void OnValueChanged(object? newValue)
        {
            var unchanged = _hasValue && Equals(_lastValue, newValue);
            _hasValue = true;
            _lastValue = newValue;

            var action = ValueChangedAction;
            if (action == null)
                return; // nothing wired (e.g. a per-message copy of the model); nothing to count

            if (unchanged)
            {
                Interlocked.Increment(ref _unchanged);
                return;
            }

            Interlocked.Increment(ref _notified);
            Run(action);
        }

        private void Run(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                var failed = ActionFailed;
                if (failed == null)
                    throw;
                failed(this, ex);
            }
        }
    }
}
