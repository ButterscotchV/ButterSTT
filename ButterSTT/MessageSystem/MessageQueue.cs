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

        private readonly object _syncParagraph = new();
        public Paragraph _curParagraph;
        private (int sentence, int word) _curIndex;

        public Paragraph CurParagraph
        {
            get
            {
                lock (_syncParagraph)
                    return _curParagraph;
            }
            set
            {
                lock (_syncParagraph)
                    _curParagraph = value;
            }
        }

        private readonly Queue<string> _wordQueue = new();
        private readonly Queue<MessageWord> _messageWordQueue = new();
        private int _curMessageLength;

        public bool IsFinished
        {
            get
            {
                lock (_syncParagraph)
                    return _wordQueue.Count <= 0 && _curParagraph.Length <= 0;
            }
        }

        private void InternLimitWordIndex()
        {
            lock (_syncParagraph)
            {
                _curIndex.word = Math.Max(
                    0,
                    _curParagraph.Sentences[_curIndex.sentence].Words.Length - 1
                );
            }
        }

        public void LimitParagraphIndex()
        {
            lock (_syncParagraph)
            {
                if (_curParagraph.Sentences.Length <= _curIndex.sentence)
                {
                    // Move to the end of the last known position
                    _curIndex.sentence = Math.Max(0, _curParagraph.Sentences.Length - 1);
                    InternLimitWordIndex();
                }
                else if (_curParagraph.Sentences[_curIndex.sentence].Length <= _curIndex.word)
                {
                    InternLimitWordIndex();
                }
            }
        }

        public IEnumerable<string> ParagraphWordEnumerator()
        {
            lock (_syncParagraph)
            {
                // Limit the index if length has changed since last known
                LimitParagraphIndex();

                // Queue all words after the current displayed ones
                for (var s = _curIndex.sentence; s < _curParagraph.Sentences.Length; s++)
                {
                    var sentence = _curParagraph.Sentences[s];
                    for (
                        var w = s <= _curIndex.sentence ? _curIndex.word : 0;
                        w < sentence.Words.Length;
                        w++
                    )
                    {
                        yield return sentence.Words[w].Text;
                    }
                }
            }
        }

        public void FinishCurrentParagraph()
        {
            lock (_syncParagraph)
            {
                // Queue all words after the current displayed ones
                foreach (var word in ParagraphWordEnumerator())
                {
                    _wordQueue.Enqueue(word);
                }

                // Reset states
                _curParagraph = default;
                _curIndex = default;
            }
        }

        public int ParagraphLengthFromIndex()
        {
            lock (_syncParagraph)
                return ParagraphWordEnumerator().Sum(w => w.Length);
        }

        public void QueueParagraphToFit(int padding = 0)
        {
            lock (_syncParagraph)
            {
                // Limit the index if length has changed since last known
                LimitParagraphIndex();

                var paragraphLen = ParagraphLengthFromIndex();
                // Queue as few words after the current displayed ones
                for (var s = _curIndex.sentence; s < _curParagraph.Sentences.Length; s++)
                {
                    var sentence = _curParagraph.Sentences[s];
                    for (
                        var w = s <= _curIndex.sentence ? _curIndex.word : 0;
                        w < sentence.Words.Length;
                        w++
                    )
                    {
                        if (paragraphLen <= MessageLength - padding)
                        {
                            _curIndex = (s, w);
                            return;
                        }

                        var word = sentence.Words[w].Text;
                        _wordQueue.Enqueue(word);
                        paragraphLen -= word.Length;
                    }
                }
            }
        }

        private static DateTime ComputeExpiration(TimeSpan span) =>
            span >= TimeSpan.MaxValue ? DateTime.MaxValue : DateTime.UtcNow + span;

        private void ProgressWordQueue()
        {
            // Make sure there is enough room to fit a new word in the message,
            // if the word is too long then just give up and pass it in anyways
            // TODO: Maybe handle long words better? Or maybe it's not worthwhile
            while (
                _wordQueue.TryPeek(out var newWord)
                && (
                    _curMessageLength + newWord.Length <= MessageLength
                    || newWord.Length > MessageLength
                )
            )
            {
                var word = _wordQueue.Dequeue();
                _messageWordQueue.Enqueue(
                    new MessageWord(
                        word,
                        ComputeExpiration(WordTime),
                        ComputeExpiration(HardWordTime)
                    )
                );
                _curMessageLength += word.Length;
            }
        }

        public string GetCurrentMessage()
        {
            lock (_syncParagraph)
            {
                // Remove expired words if more space is needed
                if (_wordQueue.Count > 0 || _curParagraph.Length > 0)
                {
                    var dequeueCount = 0;
                    while (
                        _messageWordQueue.TryPeek(out var expiredWord)
                        && (
                            (
                                dequeueCount < MaxWordsDequeued
                                && DateTime.UtcNow >= expiredWord.ExpiryTime
                            )
                            || DateTime.UtcNow >= expiredWord.HardExpiryTime
                        )
                    )
                    {
                        _curMessageLength -= _messageWordQueue.Dequeue().Text.Length;
                        dequeueCount++;
                    }
                }

                if (_curParagraph.Length >= MessageLength)
                    QueueParagraphToFit(RealtimeQueuePadding);

                ProgressWordQueue();

                var message = string.Concat(_messageWordQueue.Select(w => w.Text));

                // If there's no queue and there's new words to display
                if (_wordQueue.Count <= 0 && _curParagraph.Length > 0)
                {
                    var availableLength = MessageLength - _curMessageLength;
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
                    return (message + paragraph).Trim()
                        + (_curParagraph.Length > totalTaken ? "-" : "");
                }

                return message.Trim() + (_wordQueue.Count > 0 ? "-" : "");
            }
        }
    }
}
