using System.Collections.Immutable;

namespace Minsk.CodeAnalysis.Text;

public sealed class SourceText
{
    private readonly string _text;

    private SourceText(string text)
    {
        Lines = ParseLines(this, text);
        _text = text;
    }

    public ImmutableArray<TextLine> Lines { get; }

    public char this[int index] => _text[index];

    public int Length => _text.Length;

    public int GetLineIndex(int position)
    {
        int lower = 0;
        int upper = _text.Length - 1;

        while (lower <= upper)
        {
            var index = lower + (upper - lower) / 2;
            var start = Lines[index].Start;

            if (position == start)
            {
                return index;
            }
            else if (start > position)
            {
                upper = index + 1;
            }
            else
            {
                lower = index + 1;
            }
        }

        return lower - 1;
    }

    public override string ToString() => _text;

    public string ToString(int start, int length) => _text.Substring(start, length);

    public string ToString(TextSpan span) => _text.Substring(span.Start, span.Length);

    public static SourceText From(string text)
    {
        return new SourceText(text);
    }

    private static ImmutableArray<TextLine> ParseLines(SourceText sourceText, string text)
    {
        var result = ImmutableArray.CreateBuilder<TextLine>();

        int position = 0;
        int lineStart = 0;
        while (position < text.Length)
        {
            int lineBreakWidth = GetLineBreakWidth(text, position);
            if (lineBreakWidth == 0)
            {
                position++;
            }
            else
            {
                AddLine(result, sourceText, position, lineStart, lineBreakWidth);
                position += lineBreakWidth;
                lineStart = position;
            }
        }

        if (position > lineStart)
        {
            AddLine(result, sourceText, position, lineStart, 0);
        }

        return result.ToImmutable();
    }

    private static void AddLine(ImmutableArray<TextLine>.Builder result,
                                SourceText sourceText,
                                int position,
                                int lineStart,
                                int lineBreakWidth)
    {
        var lineLength = position - lineStart;
        var lineLengthIncludingLineBreak = lineLength + lineBreakWidth;
        var line = new TextLine(sourceText, lineStart, lineLength, lineLengthIncludingLineBreak);
        result.Add(line);
    }

    private static int GetLineBreakWidth(string text, int position)
    {
        var current = text[position];
        var next = position + 1 >= text.Length ? '\0' : text[position + 1];

        return current switch
        {
            '\r' when next == '\n' => 2,
            '\r' or '\n' => 1,
            _ => 0,
        };
    }
}
