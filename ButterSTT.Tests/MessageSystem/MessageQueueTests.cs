using ButterSTT.TextProcessing;
using Xunit;
using Xunit.Abstractions;

namespace ButterSTT.MessageSystem.Tests
{
    public class MessageQueueTests
    {
        private readonly ITestOutputHelper _output;

        public MessageQueueTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact()]
        public void ScrollTimeoutTest()
        {
            var queue = new MessageQueue
            {
                DequeueSystem = DequeueSystems.Scrolling,
                MaxWordsDequeued = int.MaxValue,
                WordTime = TimeSpan.Zero,
                HardWordTime = TimeSpan.Zero
            };

            var firstMessage = "Testing the queue system.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(firstMessage);
            queue.FinishCurrentParagraph();
            var curMessage = queue.GetCurrentMessage();
            _output.WriteLine($"First message: \"{curMessage}\"");
            Assert.Equal(firstMessage, curMessage);

            var secondMessage = "Second test message.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(secondMessage);
            queue.FinishCurrentParagraph();
            curMessage = queue.GetCurrentMessage();
            _output.WriteLine($"Second message: \"{curMessage}\"");
            Assert.Equal(secondMessage, curMessage);
        }

        [Fact()]
        public void ScrollMaxDequeueTest()
        {
            var queue = new MessageQueue
            {
                DequeueSystem = DequeueSystems.Scrolling,
                MaxWordsDequeued = 2,
                WordTime = TimeSpan.Zero,
                HardWordTime = TimeSpan.MaxValue
            };

            var firstMessage = "Testing the queue system.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(firstMessage);
            queue.FinishCurrentParagraph();
            var curMessage = queue.GetCurrentMessage();
            _output.WriteLine($"First message: \"{curMessage}\"");
            Assert.Equal(firstMessage, curMessage);

            var secondMessage = "Second test message.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(secondMessage);
            queue.FinishCurrentParagraph();
            curMessage = queue.GetCurrentMessage();
            _output.WriteLine($"Second message: \"{curMessage}\"");
            Assert.Equal($"queue system. {secondMessage}", curMessage);
        }

        [Fact()]
        public void ScrollHardWordTest()
        {
            var queue = new MessageQueue
            {
                DequeueSystem = DequeueSystems.Scrolling,
                MaxWordsDequeued = 2,
                WordTime = TimeSpan.Zero,
                HardWordTime = TimeSpan.Zero
            };

            var firstMessage = "Testing the queue system.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(firstMessage);
            queue.FinishCurrentParagraph();
            var curMessage = queue.GetCurrentMessage();
            _output.WriteLine($"First message: \"{curMessage}\"");
            Assert.Equal(firstMessage, curMessage);

            var secondMessage = "Second test message.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(secondMessage);
            queue.FinishCurrentParagraph();
            curMessage = queue.GetCurrentMessage();
            _output.WriteLine($"Second message: \"{curMessage}\"");
            Assert.Equal(secondMessage, curMessage);
        }

        [Fact()]
        public void PageDequeueTest()
        {
            var queue = new MessageQueue
            {
                MessageLength = 47,
                DequeueSystem = DequeueSystems.Pagination,
                WordTime = TimeSpan.Zero,
                HardWordTime = TimeSpan.MaxValue
            };

            var firstMessage = "Testing the queue system.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(firstMessage);
            queue.FinishCurrentParagraph();
            var curMessage = queue.GetCurrentMessage();
            _output.WriteLine($"First message: \"{curMessage}\"");
            Assert.Equal(firstMessage, curMessage);

            var secondMessage = "Second test message.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(secondMessage);
            queue.FinishCurrentParagraph();
            curMessage = queue.GetCurrentMessage();
            _output.WriteLine($"Both messages: \"{curMessage}\"");
            Assert.Equal($"{firstMessage} {secondMessage}", curMessage);

            var thirdMessage = "Testing a third time.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(thirdMessage);
            queue.FinishCurrentParagraph();
            curMessage = queue.GetCurrentMessage();
            _output.WriteLine($"Third messages: \"{curMessage}\"");
            Assert.Equal($"message. {thirdMessage}", curMessage);
        }

        [Theory()]
        [InlineData(DequeueSystems.Scrolling)]
        [InlineData(DequeueSystems.Pagination)]
        public void CombineTest(DequeueSystems dequeueSystem)
        {
            var queue = new MessageQueue
            {
                DequeueSystem = dequeueSystem,
                MaxWordsDequeued = int.MaxValue,
                WordTime = TimeSpan.Zero,
                HardWordTime = TimeSpan.Zero
            };

            var firstMessage = "Testing the queue system.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(firstMessage);
            queue.FinishCurrentParagraph();
            var secondMessage = "Second test message.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(secondMessage);
            queue.FinishCurrentParagraph();

            var curMessage = queue.GetCurrentMessage();
            _output.WriteLine($"Combined message: \"{curMessage}\"");
            Assert.Equal($"{firstMessage} {secondMessage}", curMessage);
        }

        [Theory()]
        [InlineData(DequeueSystems.Scrolling)]
        [InlineData(DequeueSystems.Pagination)]
        public void RealtimeTest(DequeueSystems dequeueSystem)
        {
            var queue = new MessageQueue
            {
                DequeueSystem = dequeueSystem,
                MaxWordsDequeued = int.MaxValue,
                WordTime = TimeSpan.Zero,
                HardWordTime = TimeSpan.Zero
            };

            // Likely incomplete word
            queue.CurParagraph = EnglishTextParser.ParseParagraph("Testing th");
            var curMessage = queue.GetCurrentMessage();
            _output.WriteLine($"First message: \"{curMessage}\"");
            Assert.Equal("Testing", curMessage);

            // Complete sentence
            var secondMessage = "Testing the queue system.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(secondMessage);
            curMessage = queue.GetCurrentMessage();
            _output.WriteLine($"Second message: \"{curMessage}\"");
            Assert.Equal(secondMessage, curMessage);

            // Complete and partial sentence with one complete word
            var thirdMessage = "Testing the queue system. Second ";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(thirdMessage);
            curMessage = queue.GetCurrentMessage();
            _output.WriteLine($"Third message: \"{curMessage}\"");
            Assert.Equal(thirdMessage.Trim(), curMessage);

            // Two complete sentences
            var fourthMessage = "Testing the queue system. Second test message.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(fourthMessage);
            curMessage = queue.GetCurrentMessage();
            _output.WriteLine($"Fourth message: \"{curMessage}\"");
            Assert.Equal(fourthMessage, curMessage);
        }

        [Theory()]
        [InlineData(DequeueSystems.Scrolling)]
        [InlineData(DequeueSystems.Pagination)]
        public void RealtimeAppendingTest(DequeueSystems dequeueSystem)
        {
            var queue = new MessageQueue
            {
                DequeueSystem = dequeueSystem,
                MaxWordsDequeued = 0,
                WordTime = TimeSpan.MaxValue,
                HardWordTime = TimeSpan.MaxValue
            };

            // Initial message, fully completed
            var firstMessage = "Testing the queue system.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(firstMessage);
            queue.FinishCurrentParagraph();

            // Add a partial sentence to it
            var secondMessage = "Second test ";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(secondMessage);
            var curMessage = queue.GetCurrentMessage();
            _output.WriteLine($"Appended message: \"{curMessage}\"");
            Assert.Equal($"{firstMessage} {secondMessage.Trim()}", curMessage);

            // Replace the partial message
            var thirdMessage = "Second test message.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(thirdMessage);
            curMessage = queue.GetCurrentMessage();
            _output.WriteLine($"Appended message: \"{curMessage}\"");
            Assert.Equal($"{firstMessage} {thirdMessage}", curMessage);
        }

        [Theory()]
        [InlineData(DequeueSystems.Scrolling)]
        [InlineData(DequeueSystems.Pagination)]
        public void RealtimeAppendingTooLongTest(DequeueSystems dequeueSystem)
        {
            var queue = new MessageQueue
            {
                MessageLength = 33,
                DequeueSystem = dequeueSystem,
                MaxWordsDequeued = int.MaxValue,
                WordTime = TimeSpan.Zero,
                HardWordTime = TimeSpan.Zero
            };

            // Initial message, fully completed
            var firstMessage = "Testing the queue system.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(firstMessage);
            queue.FinishCurrentParagraph();

            // Add a partial sentence to it
            var secondMessage = "Second test ";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(secondMessage);
            var curMessage = queue.GetCurrentMessage();
            _output.WriteLine($"Appended message: \"{curMessage}\"");
            Assert.Equal($"{firstMessage} Second-", curMessage);
        }

        [Theory()]
        [InlineData(DequeueSystems.Scrolling)]
        [InlineData(DequeueSystems.Pagination)]
        public void RealtimeTooLongTest(DequeueSystems dequeueSystem)
        {
            var queue = new MessageQueue
            {
                MessageLength = 18,
                DequeueSystem = dequeueSystem,
                MaxWordsDequeued = int.MaxValue,
                WordTime = TimeSpan.Zero,
                HardWordTime = TimeSpan.Zero
            };

            queue.CurParagraph = EnglishTextParser.ParseParagraph("Testing th");
            var curMessage = queue.GetCurrentMessage();
            _output.WriteLine($"First message: \"{curMessage}\"");
            Assert.Equal("Testing", curMessage);

            queue.CurParagraph = EnglishTextParser.ParseParagraph("Testing the queue system.");
            curMessage = queue.GetCurrentMessage();
            _output.WriteLine($"Second message: \"{curMessage}\"");
            Assert.Equal("Testing the queue-", curMessage);
        }
    }
}
