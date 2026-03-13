using System;
using System.Globalization;

namespace CameraSongScript.Models
{
    internal static class NumericStringParser
    {
        private static readonly NumberStyles FloatStyles = NumberStyles.Float | NumberStyles.AllowLeadingSign;

        public static bool TryParse(string value, out float result)
        {
            result = 0f;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string trimmed = value.Trim();
            if (float.TryParse(trimmed, FloatStyles, CultureInfo.InvariantCulture, out result))
                return true;

            string normalized = trimmed.Replace(',', '.');
            return float.TryParse(normalized, FloatStyles, CultureInfo.InvariantCulture, out result);
        }

        public static float Parse(string value)
        {
            if (!TryParse(value, out float result))
                throw new FormatException($"Invalid numeric value: '{value ?? "(null)"}'");

            return result;
        }
    }
}
