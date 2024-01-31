namespace ButterSTT.TextProcessing.TextParts
{
    public readonly struct Sentence
    {
        public readonly Word[] Words => _words ?? [];
        public readonly int Length;

        private readonly Word[] _words;

        public Sentence(Word[] words)
        {
            _words = words;
            Length = words.Sum(w => w.Text.Length);
        }
    }
}
