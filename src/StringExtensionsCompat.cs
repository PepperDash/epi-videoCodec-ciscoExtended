namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    // v2 Essentials Core exposed a NullIfEmpty() string extension; it's absent from the pinned
    // v3 build, so this plugin-local shim preserves the call sites.
    internal static class StringExtensionsCompat
    {
        /// <summary>Returns null when the string is null or empty; otherwise the string unchanged.</summary>
        public static string NullIfEmpty(this string value) =>
            string.IsNullOrEmpty(value) ? null : value;
    }
}
