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

        public static string Format(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                return string.Empty;

            if (displayName.Length <= SongScriptDisplayMaxLength)
                return displayName;

            int headLength = Math.Max(0, SongScriptDisplayHeadLength);
            int tailLength = Math.Max(0, SongScriptDisplayTailLength);
            if (headLength + tailLength + Ellipsis.Length >= displayName.Length)
                return displayName;

            if (headLength == 0)
                return Ellipsis + displayName.Substring(displayName.Length - tailLength, tailLength);

            if (tailLength == 0)
                return displayName.Substring(0, headLength) + Ellipsis;

            return displayName.Substring(0, headLength)
                + Ellipsis
                + displayName.Substring(displayName.Length - tailLength, tailLength);
        }
    }
}
