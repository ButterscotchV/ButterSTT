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

        [GeneratedRegex(@"(^|[?!.])\s*?([a-z])|i([^a-z]|$)", RegexOptions.Multiline & RegexOptions.NonBacktracking)]
        private static partial Regex BasicCapitals();
    }
}
