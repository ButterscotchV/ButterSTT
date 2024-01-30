using System.Text.RegularExpressions;
using ButterSTT.TextProcessing.TextParts;

namespace ButterSTT.TextProcessing
{
    public static partial class EnglishTextParser
    {
        public static Paragraph ParseParagraph(
            string text,
            Regex? regex = null,
            Regex? wordRegex = null,
            bool addSpaces = true
        )
        {
            Sentence[] sentences = (regex ?? SentenceKeepUrl())
                .Matches(text)
                .Select(m => ParseSentence(m.Value, regex: wordRegex, addSpaces: addSpaces))
                .ToArray();
            return new Paragraph(sentences);
        }

        public static Sentence ParseSentence(
            string text,
            Regex? regex = null,
            bool addSpaces = true
        )
        {
            Word[] words = (regex ?? WordOnlyCompleteKeepUrl())
                .Matches(text)
                .Select(m => new Word(
                    addSpaces && !m.Value.EndsWith(' ') ? m.Value + " " : m.Value
                ))
                .ToArray();
            return new Sentence(words);
        }

        // Splits sentences
        [GeneratedRegex(
            @".*?([.?!]+\s*|$)",
            RegexOptions.Multiline & RegexOptions.Singleline & RegexOptions.NonBacktracking
        )]
        public static partial Regex BasicSentence();

        // Splits sentences, preserving URLs (ex. google.com)
        [GeneratedRegex(
            @".*?(\.+\W+|[?!]+\s*|$)",
            RegexOptions.Multiline & RegexOptions.Singleline & RegexOptions.NonBacktracking
        )]
        public static partial Regex SentenceKeepUrl();

        // Splits sentences, keeping only complete sentences
        [GeneratedRegex(
            @".*?[.?!]+\s*",
            RegexOptions.Multiline & RegexOptions.Singleline & RegexOptions.NonBacktracking
        )]
        public static partial Regex SentenceOnlyComplete();

        // Splits sentences, keeping only complete sentences
        [GeneratedRegex(
            @".*?(\.+\W+|[?!]+\s+|$)",
            RegexOptions.Multiline & RegexOptions.Singleline & RegexOptions.NonBacktracking
        )]
        public static partial Regex SentenceOnlyCompleteKeepUrl();

        // Splits words
        [GeneratedRegex(
            @"(\w|[-'])+\W*",
            RegexOptions.Multiline & RegexOptions.Singleline & RegexOptions.NonBacktracking
        )]
        public static partial Regex BasicWord();

        // Splits words, preserving URLs (ex. google.com)
        [GeneratedRegex(
            @"(\w|[-'.])+\W*",
            RegexOptions.Multiline & RegexOptions.Singleline & RegexOptions.NonBacktracking
        )]
        public static partial Regex WordKeepUrl();

        // Splits words, keeping only complete words
        [GeneratedRegex(
            @"(\w|[-']\w)+[^a-zA-Z0-9_\-']+",
            RegexOptions.Multiline & RegexOptions.Singleline & RegexOptions.NonBacktracking
        )]
        public static partial Regex WordOnlyComplete();

        // Splits words, keeping only complete words and preserving URLs (ex. google.com)
        [GeneratedRegex(
            @"(\w|[-'.]\w)+[^a-zA-Z0-9_\-']+",
            RegexOptions.Multiline & RegexOptions.Singleline & RegexOptions.NonBacktracking
        )]
        public static partial Regex WordOnlyCompleteKeepUrl();
    }
}
