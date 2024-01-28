namespace ButterSTT.MessageSystem
{
    public readonly struct MessageWord
    {
        public readonly string Text;
        public readonly DateTime DisplayTime;

        public MessageWord(string text, DateTime displayTime)
        {
            Text = text;
            DisplayTime = displayTime;
        }
    }
}
