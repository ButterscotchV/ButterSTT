namespace ButterSTT.TextProcessing.TextParts
{
    public readonly struct Sentence
    {
        public readonly Word[] Words;
        public readonly int Length;

        public Sentence(Word[] words)
        {
            Words = words;
            Length = words.Sum(w => w.Text.Length);
        }
    }
}
