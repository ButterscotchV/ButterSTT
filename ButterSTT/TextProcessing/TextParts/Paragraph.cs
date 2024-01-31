namespace ButterSTT.TextProcessing.TextParts
{
    public readonly struct Paragraph
    {
        public readonly Sentence[] Sentences => _sentences ?? [];
        public readonly int Length;

        private readonly Sentence[] _sentences;

        public Paragraph(Sentence[] sentences)
        {
            _sentences = sentences;
            Length = sentences.Sum(s => s.Length);
        }
    }
}
