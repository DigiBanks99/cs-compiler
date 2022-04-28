using Minsk.CodeAnalysis.Text;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;

namespace Minsk.CodeAnalysis;

internal sealed class AnnotatedText
{
    public AnnotatedText(string text, ImmutableArray<TextSpan> spans)
    {
        Text = text;
        Spans = spans;
    }

    public string Text { get; }
    public ImmutableArray<TextSpan> Spans { get; }

    public static AnnotatedText Parse(string text)
    {
        text = Unindent(text);

        StringBuilder textBuilder = new();
        ImmutableArray<TextSpan>.Builder spanBuilder = ImmutableArray.CreateBuilder<TextSpan>();
        Stack<int> startStack = new();
        var position = 0;
        foreach (char c in text)
        {
            if (c == '[')
            {
                startStack.Push(position);
            }
            else if (c == ']')
            {
                if (startStack.Count == 0)
                {
                    throw new ArgumentException("Found annotation marker ']' with no supporting '['.", nameof(text));
                }

                var start = startStack.Pop();
                var end = position;
                var span = TextSpan.FromBounds(start, end);
                spanBuilder.Add(span);
            }
            else
            {
                position++;
                textBuilder.Append(c);
            }
        }

        return startStack.Count > 0
            ? throw new ArgumentException("Found annotation marker '[' with no supporting ']'.", nameof(text))
            : new AnnotatedText(textBuilder.ToString(), spanBuilder.ToImmutable());
    }

    private static string Unindent(string text)
    {
        List<string> lines = ReadLines(text);

        int minIndentation = int.MaxValue;
        for (var index = 0; index < lines.Count; index++)
        {
            string line = lines[index];
            if (line.Length == 0)
            {
                lines[index] = string.Empty;
                continue;
            }

            int indentation = line.Length - line.TrimStart().Length;
            minIndentation = Math.Min(minIndentation, indentation);
        }

        for (var index = 0; index < lines.Count; index++)
        {
            if (lines[index].Length == 0)
            {
                continue;
            }

            lines[index] = lines[index][minIndentation..];
        }

        DropEmptyBoundaryLines(lines);

        return string.Join(Environment.NewLine, lines);
    }

    private static void DropEmptyBoundaryLines(List<string> lines)
    {
        DropEmptyBoundary(lines, 0);
        DropEmptyBoundary(lines, lines.Count - 1);
    }

    private static void DropEmptyBoundary(List<string> lines, int index)
    {
        while (lines.Count > 0
            && index < lines.Count
            && lines[index].Length == 0)
        {
            lines.RemoveAt(index);
        }
    }

    private static List<string> ReadLines(string text)
    {
        List<string> lines = new();
        using StringReader sr = new(text);
        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            lines.Add(line);
        }

        return lines;
    }
}