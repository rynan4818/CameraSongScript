using System;

namespace CameraSongScript.Utilities
{
    internal static class SongScriptDisplayLabelFormatter
    {
        // Change these three values to tune how aggressively long script names are shortened in the UI.
        public const int SongScriptDisplayMaxLength = 35;
        public const int SongScriptDisplayHeadLength = 18;
        public const int SongScriptDisplayTailLength = 15;

        public const string Ellipsis = "...";

        public static string GetDisplayText(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                return string.Empty;

            return displayName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? displayName.Substring(0, displayName.Length - ".json".Length)
                : displayName;
        }

        public static bool NeedsShortening(string displayName)
        {
            string displayText = GetDisplayText(displayName);
            if (string.IsNullOrEmpty(displayText))
                return false;

            if (displayText.Length <= SongScriptDisplayMaxLength)
                return false;

            int headLength = Math.Max(0, SongScriptDisplayHeadLength);
            int tailLength = Math.Max(0, SongScriptDisplayTailLength);
            return headLength + tailLength + Ellipsis.Length < displayText.Length;
        }

        public static string Format(string displayName)
        {
            string displayText = GetDisplayText(displayName);
            if (string.IsNullOrEmpty(displayText))
                return string.Empty;

            if (!NeedsShortening(displayText))
                return displayText;

            int headLength = Math.Max(0, SongScriptDisplayHeadLength);
            int tailLength = Math.Max(0, SongScriptDisplayTailLength);
            if (headLength == 0)
                return Ellipsis + displayText.Substring(displayText.Length - tailLength, tailLength);

            if (tailLength == 0)
                return displayText.Substring(0, headLength) + Ellipsis;

            return displayText.Substring(0, headLength)
                + Ellipsis
                + displayText.Substring(displayText.Length - tailLength, tailLength);
        }
    }
}
