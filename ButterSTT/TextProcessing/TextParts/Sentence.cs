namespace ButterSTT.TextProcessing.TextParts
{
    public readonly struct Sentence
    {
        public readonly Word[] Words;

        public Sentence(Word[] words)
        {
            Words = words;
        }
    }
}
