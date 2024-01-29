using ButterSTT.TextProcessing;
using Xunit;
using Xunit.Abstractions;

namespace ButterSTT.MessageSystem.Tests
{
    public class MessageQueueTests
    {
        private readonly ITestOutputHelper output;

        public MessageQueueTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact()]
        public void TimeoutTest()
        {
            var queue = new MessageQueue
            {
                WordTime = TimeSpan.Zero,
                MaxWordsDequeued = int.MaxValue
            };

            var firstMessage = "Testing the queue system.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(firstMessage);
            queue.FinishCurrentParagraph();
            var curMessage = queue.GetCurrentMessage();
            output.WriteLine($"First message: \"{curMessage}\"");
            Assert.Equal(firstMessage, curMessage);

            var secondMessage = "Second test message.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(secondMessage);
            queue.FinishCurrentParagraph();
            curMessage = queue.GetCurrentMessage();
            output.WriteLine($"Second message: \"{curMessage}\"");
            Assert.Equal(secondMessage, curMessage);
        }

        [Fact()]
        public void CombineTest()
        {
            var queue = new MessageQueue
            {
                WordTime = TimeSpan.Zero,
                MaxWordsDequeued = int.MaxValue
            };

            var firstMessage = "Testing the queue system.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(firstMessage);
            queue.FinishCurrentParagraph();
            var secondMessage = "Second test message.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(secondMessage);
            queue.FinishCurrentParagraph();

            var curMessage = queue.GetCurrentMessage();
            output.WriteLine($"Combined message: \"{curMessage}\"");
            Assert.Equal($"{firstMessage} {secondMessage}", curMessage);
        }

        [Fact()]
        public void RealtimeTest()
        {
            var queue = new MessageQueue
            {
                WordTime = TimeSpan.Zero,
                MaxWordsDequeued = int.MaxValue
            };

            // Likely incomplete word
            queue.CurParagraph = EnglishTextParser.ParseParagraph("Testing th");
            var curMessage = queue.GetCurrentMessage();
            output.WriteLine($"First message: \"{curMessage}\"");
            Assert.Equal("Testing", curMessage);

            // Complete sentence
            var secondMessage = "Testing the queue system.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(secondMessage);
            curMessage = queue.GetCurrentMessage();
            output.WriteLine($"Second message: \"{curMessage}\"");
            Assert.Equal(secondMessage, curMessage);

            // Complete and partial sentence with one complete word
            var thirdMessage = "Testing the queue system. Second ";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(thirdMessage);
            curMessage = queue.GetCurrentMessage();
            output.WriteLine($"Third message: \"{curMessage}\"");
            Assert.Equal(thirdMessage.Trim(), curMessage);

            // Two complete sentences
            var fourthMessage = "Testing the queue system. Second test message.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(fourthMessage);
            curMessage = queue.GetCurrentMessage();
            output.WriteLine($"Fourth message: \"{curMessage}\"");
            Assert.Equal(fourthMessage, curMessage);
        }

        // Not yet implemented, should fail
        [Fact()]
        public void RealtimeAppendingTest()
        {
            var queue = new MessageQueue { WordTime = TimeSpan.MaxValue, MaxWordsDequeued = 0 };

            // Initial message, fully completed
            var firstMessage = "Testing the queue system.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(firstMessage);
            queue.FinishCurrentParagraph();

            // Add a partial sentence to it
            var secondMessage = "Second test ";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(secondMessage);
            var curMessage = queue.GetCurrentMessage();
            output.WriteLine($"Appended message: \"{curMessage}\"");
            Assert.Equal($"{firstMessage} {secondMessage.Trim()}", curMessage);

            // Replace the partial message
            var thirdMessage = "Second test message.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(thirdMessage);
            curMessage = queue.GetCurrentMessage();
            output.WriteLine($"Appended message: \"{curMessage}\"");
            Assert.Equal($"{firstMessage} {thirdMessage}", curMessage);
        }

        [Fact()]
        public void MaxDequeueTest()
        {
            var queue = new MessageQueue { WordTime = TimeSpan.Zero, MaxWordsDequeued = 2 };

            var firstMessage = "Testing the queue system.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(firstMessage);
            queue.FinishCurrentParagraph();
            var curMessage = queue.GetCurrentMessage();
            output.WriteLine($"First message: \"{curMessage}\"");
            Assert.Equal(firstMessage, curMessage);

            var secondMessage = "Second test message.";
            queue.CurParagraph = EnglishTextParser.ParseParagraph(secondMessage);
            queue.FinishCurrentParagraph();
            curMessage = queue.GetCurrentMessage();
            output.WriteLine($"Second message: \"{curMessage}\"");
            Assert.Equal($"queue system. {secondMessage}", curMessage);
        }
    }
}
