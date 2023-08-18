using System.Text.RegularExpressions;

namespace ButterSTT
{
    public static partial class EnglishCapitalization
    {
        public static readonly Regex BasicRegex = BasicCapitals();

        public static string Capitalize(string message)
        {
            return BasicRegex.Replace(message.ToLower(), c => c.Value.ToUpper());
        }

        // Capitalizes starts of sentences and standalone "I"s, must be run on a lowercase string
        [GeneratedRegex(@"(^|[?!.])\s*?([a-z])|(^|[^a-z])i($|[^a-z])", RegexOptions.Multiline & RegexOptions.NonBacktracking)]
        private static partial Regex BasicCapitals();
    }
}
