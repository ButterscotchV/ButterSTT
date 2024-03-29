using ButterSTT.Config;
using ButterSTT.TextProcessing.TextParts;

namespace ButterSTT.MessageSystem
{
    public class MessageQueue
    {
        /// <summary>
        /// The maximum length of the message.
        /// </summary>
        public int MessageLength = STTConfig.Default.MessageLength;

        /// <summary>
        /// Which of the <see cref="DequeueSystems"/> to use.
        /// </summary>
        public DequeueSystems DequeueSystem = STTConfig.Default.DequeueSystem.EnumValue;

        /// <summary>
        /// The maximum number of words to dequeue at once, regardless of their expiration time.
        /// </summary>
        public int MaxWordsDequeued = STTConfig.Default.MaxWordsDequeued;

        /// <summary>
        /// The number of characters to allow between the current paragraph and the message length.
        /// </summary>
        public int RealtimeQueuePadding = STTConfig.Default.RealtimeQueuePadding;

        /// <summary>
        /// The amount of time before a word will expire.
        /// </summary>
        public TimeSpan WordTime = STTConfig.Default.WordTime;

        /// <summary>
        /// The amount of time before a word will expire, being dequeued regardless of <see cref="MaxWordsDequeued"/>.
        /// </summary>
        public TimeSpan HardWordTime = STTConfig.Default.HardWordTime;

        /// <summary>
        /// The number of words to keep after removing a page.
        /// </summary>
        public int PageContext = STTConfig.Default.PageContext;

        /// <summary>
        /// Whether to use the message prefix from pagination.
        /// </summary>
        public bool UsePagePrefix = STTConfig.Default.UsePagePrefix;

        private readonly object _syncParagraph = new();
        public Paragraph _curParagraph;
        private (int sentence, int word) _curIndex;
        private bool _atCurEnd = false;

        /// <summary>
        /// Keeps track of whether the current message should have a prefix.
        /// </summary>
        private bool _pagePrefix = false;

        /// <summary>
        /// Keeps track of whether text is currently being written, if true then don't hard expire the current message.
        /// </summary>
        private bool _hadTextLast = false;

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

        public int AvailableMessageLength => MessageLength - _curMessageLength;

        public (int sentence, int word)? NextIndex()
        {
            if (_curParagraph.Sentences.Length <= 0)
                return null;

            // If there's another word, take it
            if (_curParagraph.Sentences[_curIndex.sentence].Words.Length > _curIndex.word + 1)
                return (_curIndex.sentence, _curIndex.word + 1);

            // If there's another sentence with any words, take that
            for (var s = _curIndex.sentence; s < _curParagraph.Sentences.Length; s++)
            {
                if (_curParagraph.Sentences[s].Words.Length > 0)
                    return (s, 0);
            }

            return null;
        }

        private void LimitWordIndex()
        {
            lock (_syncParagraph)
            {
                if (_curParagraph.Sentences.Length <= 0)
                    return;
                _curIndex.word = Math.Max(
                    0,
                    _curParagraph.Sentences[_curIndex.sentence].Words.Length - 1
                );

                if (_atCurEnd)
                {
                    var nextIndex = NextIndex();
                    if (nextIndex != null)
                    {
                        _curIndex = nextIndex.Value;
                        _atCurEnd = false;
                    }
                }
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
                    LimitWordIndex();
                }
                else if (_curParagraph.Sentences[_curIndex.sentence].Length <= _curIndex.word)
                {
                    LimitWordIndex();
                }
            }
        }

        public IEnumerable<string> ParagraphWordEnumerator()
        {
            lock (_syncParagraph)
            {
                // Limit the index if length has changed since last known
                LimitParagraphIndex();

                // If it's at the end, don't return anything
                if (_atCurEnd)
                    yield break;

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
                _atCurEnd = false;
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
                if (_curParagraph.Length <= 0)
                {
                    _curIndex = default;
                    _atCurEnd = false;
                    return;
                }

                switch (DequeueSystem)
                {
                    case DequeueSystems.Scrolling:
                        // If the full current paragraph doesn't exceed the padded
                        // mesage length, we can just ignore it entirely
                        if (_curParagraph.Length > MessageLength - padding)
                            return;
                        break;
                }

                // Limit the index if length has changed since last known
                LimitParagraphIndex();

                // If it's at the end, we don't need to do anything
                if (_atCurEnd)
                    return;

                var paragraphLen = ParagraphLengthFromIndex();
                var availableSpace = AvailableMessageLength;

                switch (DequeueSystem)
                {
                    case DequeueSystems.Pagination:
                        // Don't run if the new paragraph doesn't fill all the
                        // available space
                        if (availableSpace > paragraphLen)
                            return;
                        break;
                }

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
                        var word = sentence.Words[w].Text;

                        if (paragraphLen <= MessageLength - padding)
                        {
                            switch (DequeueSystem)
                            {
                                case DequeueSystems.Scrolling:
                                    _curIndex = (s, w);
                                    return;
                                case DequeueSystems.Pagination:
                                    // Only finish if the available space is filled to
                                    // make a full page to dequeue
                                    if (availableSpace < word.Length)
                                    {
                                        _curIndex = (s, w);
                                        return;
                                    }
                                    break;
                            }
                        }

                        _wordQueue.Enqueue(word);
                        paragraphLen -= word.Length;
                        availableSpace -= word.Length;
                    }
                }

                _atCurEnd = true;
            }
        }

        private void ExpireWords()
        {
            var dequeueCount = 0;
            while (
                _messageWordQueue.TryPeek(out var expiredWord)
                && (
                    (dequeueCount < MaxWordsDequeued && DateTime.UtcNow >= expiredWord.ExpiryTime)
                    || DateTime.UtcNow >= expiredWord.HardExpiryTime
                )
            )
            {
                _curMessageLength -= _messageWordQueue.Dequeue().Text.Length;
                dequeueCount++;
            }
        }

        private bool IsMessageFull =>
            (_wordQueue.TryPeek(out var word) && word.Length > AvailableMessageLength)
            || ParagraphWordEnumerator().Select(w => w.Length).FirstOrDefault(0)
                > AvailableMessageLength;

        private void PaginateWords()
        {
            var lastWord = _messageWordQueue.Last();
            var hardExpired = DateTime.UtcNow >= lastWord.HardExpiryTime;
            // Wait until the last word on the full page is expired
            if (
                _hadTextLast
                || (!hardExpired && (DateTime.UtcNow < lastWord.ExpiryTime || !IsMessageFull))
            )
                return;

            var removedAny = false;
            while (
                _messageWordQueue.Count > 0
                && (
                    hardExpired
                    || _messageWordQueue.Count > PageContext
                    || (
                        _messageWordQueue.TryPeek(out var expiredWord)
                        && DateTime.UtcNow >= expiredWord.HardExpiryTime
                    )
                )
            )
            {
                _curMessageLength -= _messageWordQueue.Dequeue().Text.Length;
                removedAny = true;
            }

            if (_messageWordQueue.Count <= 0)
            {
                if (removedAny && !hardExpired && UsePagePrefix)
                {
                    _pagePrefix = true;
                    _curMessageLength = 1;
                }
                else
                {
                    _pagePrefix = false;
                    _curMessageLength = 0;
                }
            }
            else if (removedAny && !_pagePrefix && UsePagePrefix)
            {
                _pagePrefix = true;
                _curMessageLength += 1;
            }
        }

        private void DequeueMessages()
        {
            // Only dequeue words if more space is needed
            if (
                _messageWordQueue.Count <= 0
                || (_wordQueue.Count <= 0 && _curParagraph.Length <= 0)
            )
                return;
            switch (DequeueSystem)
            {
                case DequeueSystems.Scrolling:
                    ExpireWords();
                    break;
                case DequeueSystems.Pagination:
                    PaginateWords();
                    break;
            }
        }

        private static DateTime ComputeExpiration(DateTime time, TimeSpan span) =>
            time >= DateTime.MaxValue || span >= TimeSpan.MaxValue
                ? DateTime.MaxValue
                : time + span;

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
                var lastTime = _messageWordQueue
                    .Select(w => w.ExpiryTime)
                    .LastOrDefault(DateTime.UtcNow);
                var baseTime = DateTime.UtcNow > lastTime ? DateTime.UtcNow : lastTime;
                var wordTime = ComputeExpiration(baseTime, WordTime);
                _messageWordQueue.Enqueue(
                    new MessageWord(word, wordTime, ComputeExpiration(wordTime, HardWordTime))
                );
                _curMessageLength += word.Length;
            }
        }

        public string GetCurrentMessage()
        {
            lock (_syncParagraph)
            {
                DequeueMessages();
                QueueParagraphToFit(RealtimeQueuePadding);
                ProgressWordQueue();

                var message = string.Concat(_messageWordQueue.Select(w => w.Text));
                var showPrefix = _pagePrefix;
                var showSuffix = _wordQueue.Count > 0;

                // If there's no queue and there's new words to display
                _hadTextLast = false;
                if (_wordQueue.Count <= 0 && _curParagraph.Length > 0)
                {
                    var paragraphLength = ParagraphLengthFromIndex();
                    if (paragraphLength > 0)
                    {
                        var availableLength = AvailableMessageLength;
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
                        message += paragraph;
                        showSuffix = paragraphLength > totalTaken;
                        _hadTextLast = totalTaken > 0;
                    }
                }

                if (string.IsNullOrWhiteSpace(message))
                    return "";

                message = message.Trim();
                return (showPrefix ? "-" : "") + message + (showSuffix ? "-" : "");
            }
        }
    }
}
