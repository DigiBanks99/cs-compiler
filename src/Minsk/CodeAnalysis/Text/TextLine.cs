namespace Minsk.CodeAnalysis.Text;

public sealed class TextLine
{
    public TextLine(SourceText text, int start, int length, int lengthIncludingLineBreak)
    {
        Text = text;
        Start = start;
        Length = length;
        End = Start + Length;
        LengthIncludingLineBreak = lengthIncludingLineBreak;

        Span = new TextSpan(Start, Length);
        SpanIncludingLineBreak = new TextSpan(Start, LengthIncludingLineBreak);
    }

    public SourceText Text { get; }
    public int Start { get; }
    public int End { get; }
    public int Length { get; }
    public int LengthIncludingLineBreak { get; }
    public TextSpan Span { get; }
    public TextSpan SpanIncludingLineBreak { get; }

    public override string ToString() => Text.ToString(Span);
}