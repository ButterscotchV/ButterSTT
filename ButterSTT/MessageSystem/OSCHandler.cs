using CoreOSC;

namespace ButterSTT.MessageSystem
{
    public class OSCHandler
    {
        public readonly UDPSender OSCSender;

        public OSCHandler(string address = "127.0.0.1", int port = 9000)
        {
            OSCSender = new UDPSender(address, port);
        }

        public static OscMessage MakeChatboxTyping(bool isTyping)
        {
            return new OscMessage("/chatbox/typing", isTyping);
        }

        public static OscMessage MakeChatboxInput(
            string message,
            bool skipKeyboard = true,
            bool playNotification = false
        )
        {
            return new OscMessage("/chatbox/input", message, skipKeyboard, playNotification);
        }
    }
}
