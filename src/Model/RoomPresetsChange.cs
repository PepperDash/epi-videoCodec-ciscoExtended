namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    public partial class CiscoCodecStatus
    {
        /// <summary>
        /// Signals that <c>Status.RoomPresets</c> was replaced. The setter always assigns <c>true</c>, so this is
        /// an event-style value: it must notify on every set. Comparing against the last value would notify once,
        /// on the constructor's first assignment before anything is wired, and never again.
        /// </summary>
        public class RoomPresetsChange : ValueProperty
        {
            private bool _value;

            public bool Value
            {
                get { return _value; }
                set
                {
                    _value = value;
                    OnValueChanged();
                }
            }
        }
    }
}
