using System.Text.RegularExpressions;

namespace ButterSTT.TextProcessing
{
    public static partial class EnglishCapitalization
    {
        public static string Capitalize(string message, Regex? regex = null)
        {
            return (regex ?? BasicCapitals()).Replace(message.ToLower(), c => c.Value.ToUpper());
        }

        // Capitalizes starts of sentences and standalone "I"s, must be run on a lowercase string
        [GeneratedRegex(
            @"(^|[?!.])\s*?([a-z])|(^|[^a-z])i($|[^a-z])",
            RegexOptions.Multiline & RegexOptions.Singleline & RegexOptions.NonBacktracking
        )]
        public static partial Regex BasicCapitals();
    }
}
