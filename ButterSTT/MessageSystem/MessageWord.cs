namespace ButterSTT.MessageSystem
{
    public readonly struct MessageWord
    {
        public readonly string Text;
        public readonly DateTime ExpiryTime;

        public MessageWord(string text, DateTime expiryTime)
        {
            Text = text;
            ExpiryTime = expiryTime;
        }
    }
}
