namespace ButterSTT.TextProcessing.TextParts
{
    public readonly struct Paragraph
    {
        public readonly Sentence[] Sentences;

        public Paragraph(Sentence[] sentences)
        {
            Sentences = sentences;
        }
    }
}
