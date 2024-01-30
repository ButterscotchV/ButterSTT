using System.Diagnostics;
using System.Net;
using ButterSTT.Config;
using LucHeart.CoreOSC;

namespace ButterSTT.MessageSystem
{
    public class OSCMessageHandler : IDisposable
    {
        public TimeSpan RateLimit = STTConfig.Default.OSCChatboxRateLimit;

        public readonly MessageQueue MessageQueue;
        private OscSender _oscSender;

        private CancellationTokenSource? _messageLoopCancelSource;
        private Task? _messageLoop;

        private string _lastMessage = "";

        public OSCMessageHandler(MessageQueue? messageQueue = null, IPEndPoint? ipEndPoint = null)
        {
            MessageQueue = messageQueue ?? new();
            _oscSender = new(ipEndPoint ?? STTConfig.Default.OSCEndpoint);
        }

        public void SetOSCEndpoint(IPEndPoint? ipEndPoint = null)
        {
            _oscSender.Dispose();
            _oscSender = new(ipEndPoint ?? STTConfig.Default.OSCEndpoint);
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

                await SendMessage(cancelToken);

                var timeout = RateLimit - timer.Elapsed;
                if (timeout.TotalMilliseconds > 0)
                    await Task.Delay(timeout, cancelToken);
            }
        }

        private async Task SendMessage(CancellationToken cancelToken = default)
        {
            try
            {
                var message = MessageQueue.GetCurrentMessage();
                if (string.IsNullOrWhiteSpace(message) || message == _lastMessage)
                    return;

                var chatbox = OSCUtils.MakeChatboxInput(message);
                if (MessageQueue.IsFinished)
                {
                    // Just send the message, no more typing
                    await _oscSender.SendAsync(chatbox);
                }
                else
                {
                    // Still typing the message... Show as typing!
                    await _oscSender.SendAsync(
                        new OscBundle(0, chatbox, OSCUtils.MakeChatboxTyping(true))
                    );
                }

                _lastMessage = message;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void Dispose()
        {
            _oscSender.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
