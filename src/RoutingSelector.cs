#nullable enable
using System;
using System.Globalization;

namespace PepperDash.Essentials.Plugin.CiscoRoomOsCodec
{
    /// <summary>
    /// Reads a routing selector as an input number (#121). Essentials routing ports use int selectors,
    /// but a selector from JSON (devjson, a bridge, config) is usually a long or a string.
    /// </summary>
    /// <remarks>Has no Crestron dependencies, so the test project compiles and tests it directly.</remarks>
    public static class RoutingSelector
    {
        /// <summary>
        /// True when <paramref name="selector"/> is a whole number in int range: any integral type, or
        /// a string or other IConvertible holding one.
        /// </summary>
        public static bool TryGetInput(object? selector, out int input)
        {
            input = 0;
            switch (selector)
            {
                case null:
                case bool _:
                case char _:
                    return false;
                case int i:
                    input = i;
                    return true;
                case string s:
                    return int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out input);
                case IConvertible convertible:
                    try
                    {
                        var value = convertible.ToDecimal(CultureInfo.InvariantCulture);
                        if (value != Math.Truncate(value) || value < int.MinValue || value > int.MaxValue)
                            return false;
                        input = (int)value;
                        return true;
                    }
                    catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
                    {
                        return false;
                    }
                default:
                    return false;
            }
        }
    }
}
