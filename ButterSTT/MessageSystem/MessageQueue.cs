using ButterSTT.TextProcessing.TextParts;

namespace ButterSTT.MessageSystem
{
    public class MessageQueue
    {
        /// <summary>
        /// The maximum length of the message. Default is 144.
        /// </summary>
        public int MessageLength = 144;

        /// <summary>
        /// The maximum number of words to dequeue at once, regardless of their expiration time. Default is 6.
        /// </summary>
        public int MaxWordsDequeued = 6;

        /// <summary>
        /// The number of characters to allow between the current paragraph and the message length. Default is 36.
        /// </summary>
        public int RealtimeQueuePadding = 36;

        /// <summary>
        /// The amount of time before a word will expire. Default is 3 seconds.
        /// </summary>
        public TimeSpan WordTime = TimeSpan.FromSeconds(3);

        /// <summary>
        /// The amount of time before a word will expire, being dequeued regardless of <see cref="MaxWordsDequeued"/>. Default is 30 seconds.
        /// </summary>
        public TimeSpan HardWordTime = TimeSpan.FromSeconds(30);

        public Paragraph CurParagraph;
        private (int sentence, int word) CurIndex;

        private readonly Queue<string> WordQueue = new();
        private readonly Queue<MessageWord> MessageWordQueue = new();
        private int CurMessageLength;

        private void InternLimitWordIndex()
        {
            CurIndex.word = Math.Max(0, CurParagraph.Sentences[CurIndex.sentence].Words.Length - 1);
        }

        public void LimitParagraphIndex()
        {
            if (CurParagraph.Sentences.Length <= CurIndex.sentence)
            {
                // Move to the end of the last known position
                CurIndex.sentence = Math.Max(0, CurParagraph.Sentences.Length - 1);
                InternLimitWordIndex();
            }
            else if (CurParagraph.Sentences[CurIndex.sentence].Length <= CurIndex.word)
            {
                InternLimitWordIndex();
            }
        }

        public IEnumerable<string> ParagraphWordEnumerator()
        {
            // Limit the index if length has changed since last known
            LimitParagraphIndex();

            // Queue all words after the current displayed ones
            for (var s = CurIndex.sentence; s < CurParagraph.Sentences.Length; s++)
            {
                var sentence = CurParagraph.Sentences[s];
                for (
                    var w = s <= CurIndex.sentence ? CurIndex.word : 0;
                    w < sentence.Words.Length;
                    w++
                )
                {
                    yield return sentence.Words[w].Text;
                }
            }
        }

        public void FinishCurrentParagraph()
        {
            // Queue all words after the current displayed ones
            foreach (var word in ParagraphWordEnumerator())
            {
                WordQueue.Enqueue(word);
            }

            // Reset states
            CurParagraph = default;
            CurIndex = default;
        }

        public int ParagraphLengthFromIndex() => ParagraphWordEnumerator().Sum(w => w.Length);

        public void QueueParagraphToFit(int padding = 0)
        {
            // Limit the index if length has changed since last known
            LimitParagraphIndex();

            var paragraphLen = ParagraphLengthFromIndex();
            // Queue as few words after the current displayed ones
            for (var s = CurIndex.sentence; s < CurParagraph.Sentences.Length; s++)
            {
                var sentence = CurParagraph.Sentences[s];
                for (
                    var w = s <= CurIndex.sentence ? CurIndex.word : 0;
                    w < sentence.Words.Length;
                    w++
                )
                {
                    if (paragraphLen <= MessageLength - padding)
                    {
                        CurIndex = (s, w);
                        return;
                    }

                    var word = sentence.Words[w].Text;
                    WordQueue.Enqueue(word);
                    paragraphLen -= word.Length;
                }
            }
        }

        private void ProgressWordQueue()
        {
            // Make sure there is enough room to fit a new word in the message,
            // if the word is too long then just give up and pass it in anyways
            // TODO: Maybe handle long words better? Or maybe it's not worthwhile
            while (
                WordQueue.TryPeek(out var newWord)
                && (
                    CurMessageLength + newWord.Length <= MessageLength
                    || newWord.Length > MessageLength
                )
            )
            {
                var word = WordQueue.Dequeue();
                MessageWordQueue.Enqueue(
                    new MessageWord(
                        word,
                        WordTime >= TimeSpan.MaxValue
                            ? DateTime.MaxValue
                            : DateTime.UtcNow + WordTime,
                        HardWordTime >= TimeSpan.MaxValue
                            ? DateTime.MaxValue
                            : DateTime.UtcNow + HardWordTime
                    )
                );
                CurMessageLength += word.Length;
            }
        }

        public string GetCurrentMessage()
        {
            // Remove expired words if more space is needed
            if (WordQueue.Count > 0 || CurParagraph.Length > 0)
            {
                var dequeueCount = 0;
                while (
                    MessageWordQueue.TryPeek(out var expiredWord)
                    && (
                        (
                            dequeueCount < MaxWordsDequeued
                            && DateTime.UtcNow >= expiredWord.ExpiryTime
                        )
                        || DateTime.UtcNow >= expiredWord.HardExpiryTime
                    )
                )
                {
                    CurMessageLength -= MessageWordQueue.Dequeue().Text.Length;
                    dequeueCount++;
                }
            }

            if (CurParagraph.Length >= MessageLength)
                QueueParagraphToFit(RealtimeQueuePadding);

            ProgressWordQueue();

            var message = string.Concat(MessageWordQueue.Select(w => w.Text));

            // If there's no queue and there's new words to display
            if (WordQueue.Count <= 0 && CurParagraph.Length > 0)
            {
                var availableLength = MessageLength - CurMessageLength;
                var totalTaken = 0;
                var paragraph = string.Concat(
                    ParagraphWordEnumerator()
                        .TakeWhile(w =>
                        {
                            if (totalTaken + w.Length <= availableLength)
                            {
                                totalTaken += w.Length;
                                return true;
                            }
                            else
                                return false;
                        })
                );
                return (message + paragraph).Trim() + (CurParagraph.Length > totalTaken ? "-" : "");
            }

            return message.Trim() + (WordQueue.Count > 0 ? "-" : "");
        }
    }
}
