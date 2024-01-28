namespace ButterSTT.TextProcessing.TextParts
{
    public readonly struct Paragraph
    {
        public readonly Sentence[] Sentences;
        public readonly int Length;

        public Paragraph(Sentence[] sentences)
        {
            Sentences = sentences;
            Length = sentences.Sum(s => s.Length);
        }
    }
}
