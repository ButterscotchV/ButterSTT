using CoreOSC;

namespace ButterSTT
{
    public class OSCHandler
    {
        public readonly UDPSender OSCSender;

        public OSCHandler(string address = "127.0.0.1", int port = 9000)
        {
            OSCSender = new UDPSender(address, port);
        }

        public void SendTyping(bool isTyping)
        {
            OSCSender.Send(new OscMessage("/chatbox/typing", isTyping));
        }

        public void SendMessage(string message)
        {
            OSCSender.Send(new OscMessage("/chatbox/input", message, true, false));
        }
    }
}
