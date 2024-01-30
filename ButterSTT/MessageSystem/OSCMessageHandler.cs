using System.Diagnostics;
using ButterSTT.Config;
using CoreOSC;

namespace ButterSTT.MessageSystem
{
    public class OSCMessageHandler
    {
        public TimeSpan RateLimit = STTConfig.Default.OSCChatboxRateLimit;

        public readonly MessageQueue MessageQueue;
        private OSCHandler _oscHandler;

        private CancellationTokenSource? _messageLoopCancelSource;
        private Task? _messageLoop;

        private string _lastMessage = "";

        public OSCMessageHandler(
            MessageQueue? messageQueue = null,
            string? oscAddress = null,
            int? oscPort = null
        )
        {
            MessageQueue = messageQueue ?? new();
            _oscHandler = MakeOSCHandler(oscAddress, oscPort);
        }

        private static OSCHandler MakeOSCHandler(string? oscAddress = null, int? oscPort = null)
        {
            return new(
                oscAddress ?? STTConfig.Default.OSCAddress,
                oscPort ?? STTConfig.Default.OSCPort
            );
        }

        public void SetOSCEndpoint(string? oscAddress = null, int? oscPort = null)
        {
            _oscHandler = MakeOSCHandler(oscAddress, oscPort);
        }

        public bool IsLoopRunning => _messageLoop != null && !_messageLoop.IsCompleted;

        public void StartMessageLoop()
        {
            if (IsLoopRunning)
                return;

            var cancelSource = new CancellationTokenSource();
            _messageLoop = MessageLoop(cancelSource.Token);
            _messageLoopCancelSource = cancelSource;
        }

        public async Task StopMessageLoop(CancellationToken cancelToken = default)
        {
            if (!IsLoopRunning)
                return;

            var messageLoop = _messageLoop;
            if (messageLoop == null)
                return;

            _messageLoopCancelSource?.Cancel();
            await messageLoop.WaitAsync(cancelToken);
            _messageLoop = null;
            _messageLoopCancelSource = null;
        }

        private async Task MessageLoop(CancellationToken cancelToken)
        {
            var timer = new Stopwatch();
            while (!cancelToken.IsCancellationRequested)
            {
                timer.Restart();

                SendMessage();

                var timeout = RateLimit - timer.Elapsed;
                if (timeout.TotalMilliseconds > 0)
                    await Task.Delay(timeout, cancelToken);
            }
        }

        private void SendMessage()
        {
            try
            {
                var message = MessageQueue.GetCurrentMessage();
                if (string.IsNullOrWhiteSpace(message) || message == _lastMessage)
                    return;

                var chatbox = OSCHandler.MakeChatboxInput(message);
                if (MessageQueue.IsFinished)
                {
                    // Just send the message, no more typing
                    _oscHandler.OSCSender.Send(chatbox);
                }
                else
                {
                    // Still typing the message... Show as typing!
                    _oscHandler.OSCSender.Send(
                        new OscBundle(0, chatbox, OSCHandler.MakeChatboxTyping(true))
                    );
                }

                _lastMessage = message;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
