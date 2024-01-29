namespace ButterSTT.MessageSystem
{
    public readonly struct MessageWord
    {
        public readonly string Text;
        public readonly DateTime ExpiryTime;
        public readonly DateTime HardExpiryTime;

        public MessageWord(string text, DateTime expiryTime, DateTime hardExpiryTime)
        {
            Text = text;
            ExpiryTime = expiryTime;
            HardExpiryTime = hardExpiryTime;
        }
    }
}
