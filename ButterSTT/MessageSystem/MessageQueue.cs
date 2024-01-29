using ButterSTT.TextProcessing.TextParts;

namespace ButterSTT.MessageSystem
{
    public class MessageQueue
    {
        public int MessageLength = 144;
        public TimeSpan WordTime = TimeSpan.FromSeconds(3);

        public Paragraph CurParagraph;
        private (int sentence, int word) CurIndex;

        private readonly Queue<string> WordQueue = new();
        private readonly Queue<MessageWord> MessageWordQueue = new();
        private int CurMessageLength;

        private void InternLimitWordIndex()
        {
            CurIndex.word = CurParagraph.Sentences[CurIndex.sentence].Words.Length - 1;
        }

        public void LimitParagraphIndex()
        {
            if (CurParagraph.Sentences.Length <= CurIndex.sentence)
            {
                // Move to the end of the last known position
                CurIndex.sentence = CurParagraph.Sentences.Length - 1;
                InternLimitWordIndex();
            }
            else if (CurParagraph.Sentences[CurIndex.sentence].Length <= CurIndex.word)
            {
                InternLimitWordIndex();
            }
        }

        public void FinishCurrentParagraph()
        {
            // Limit the index if length has changed since last known
            LimitParagraphIndex();

            // Queue all words after the current displayed ones
            for (var s = CurIndex.sentence; s < CurParagraph.Sentences.Length; s++)
            {
                var wordCount = CurParagraph.Sentences[s].Words.Length;
                for (var w = CurIndex.word; w < wordCount; w++)
                {
                    var word = CurParagraph.Sentences[s].Words[w];
                    WordQueue.Enqueue(
                        $"{word.Text}{(w + 1 >= wordCount && !word.Text.EndsWith(' ') ? " " : "")}"
                    );
                }
                // Reset word index to 0 for following sentences
                CurIndex.word = 0;
            }

            // Reset states
            CurParagraph = default;
            CurIndex = default;
        }

        public string GetCurrentMessage()
        {
            // Remove expired words if more space is needed
            if (WordQueue.Count > 0 || CurParagraph.Length > 0)
            {
                while (
                    MessageWordQueue.TryPeek(out var expiredWord)
                    && DateTime.UtcNow - expiredWord.DisplayTime >= WordTime
                )
                {
                    CurMessageLength -= MessageWordQueue.Dequeue().Text.Length;
                }
            }

            // Make sure there is enough room to fit a new word in the message and
            // allow space for a dash after the current text if there is already more
            while (
                WordQueue.TryPeek(out var newWord)
                && CurMessageLength + newWord.Length + (WordQueue.Count > 1 ? 1 : 0) < MessageLength
            )
            {
                var word = WordQueue.Dequeue();
                MessageWordQueue.Enqueue(new MessageWord(word, DateTime.UtcNow));
                CurMessageLength += word.Length;
            }

            // If there's no queue and there's new words to display
            if (WordQueue.Count <= 0 && CurParagraph.Length > 0)
            {
                // If there's no message to display besides the one in progress, display what we have now
                if (MessageWordQueue.Count <= 0)
                {
                    return CurParagraph
                        .Sentences.SelectMany(x => x.Words, (x, y) => y.Text)
                        .Aggregate("", (x, y) => x + y)
                        .Trim();
                }
            }

            var message = MessageWordQueue
                .Select(w => w.Text)
                .Aggregate("", (x, y) => x + y)
                .Trim();
            return $"{message}{(WordQueue.Count > 0 ? "-" : "")}";
        }
    }
}
