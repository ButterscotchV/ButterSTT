using LucHeart.CoreOSC;

namespace ButterSTT.MessageSystem
{
    public static class OSCUtils
    {
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
