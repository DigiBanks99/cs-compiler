using Minsk.CodeAnalysis;
using Minsk.CodeAnalysis.Syntax;
using Minsk.CodeAnalysis.Text;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// 1 + 2 * 3
//   +
//  / \
// 1  *
//   / \
//  2   3

bool showTree = false;
Dictionary<VariableSymbol, object?> variables = new();
StringBuilder textBuilder = new();
Compilation? previousCompilation = null;

while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    if (textBuilder.Length == 0)
    {
        Console.Write("› ");
    }
    else
    {
        Console.Write("· ");
    }
    Console.ResetColor();

    var input = Console.ReadLine();
    var isBlank = string.IsNullOrEmpty(input);

    if (textBuilder.Length == 0)
    {
        if (isBlank)
        {
            break;
        }
        else if (input == "#showTree")
        {
            showTree = !showTree;
            Console.WriteLine(showTree ? "Showing parse trees." : "Not showing parse trees");
            continue;
        }
        else if (input == "#cls")
        {
            Console.Clear();
            continue;
        }
        else if (input == "#reset")
        {
            previousCompilation = null;
            continue;
        }
    }

    textBuilder.AppendLine(input);
    var text = textBuilder.ToString();

    SyntaxTree syntaxTree = SyntaxTree.Parse(text);
    if (!isBlank && syntaxTree.Diagnostics.Any())
    {
        continue;
    }

    Compilation compilation = previousCompilation == null ? new(syntaxTree) : previousCompilation.ContinueWith(syntaxTree);
    EvaluationResult result = compilation.Evaluate(variables);

    if (showTree)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        syntaxTree.Root.WriteTo(Console.Out);
        Console.ResetColor();
    }

    if (!result.Diagnostics.Any())
    {
        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        Console.WriteLine(result.Value);
        Console.ResetColor();

        previousCompilation = compilation;
    }
    else
    {
        foreach (var diagnostic in result.Diagnostics)
        {
            var lineIndex = syntaxTree.Text.GetLineIndex(diagnostic.Span.Start);
            var line = syntaxTree.Text.Lines[lineIndex];
            var lineNumber = lineIndex + 1;
            var character = diagnostic.Span.Start - line.Start + 1;

            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Error.Write($"({lineNumber}, {character}): ");
            Console.Error.WriteLine(diagnostic);
            Console.ResetColor();

            var prefixSpan = TextSpan.FromBounds(line.Start, diagnostic.Span.Start);
            var suffixSpan = TextSpan.FromBounds(diagnostic.Span.End, line.End);

            var prefix = syntaxTree.Text.ToString(prefixSpan);
            var error = syntaxTree.Text.ToString(diagnostic.Span);
            var suffix = syntaxTree.Text.ToString(suffixSpan);

            Console.Error.Write($"    {prefix}");

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Error.Write(error);
            Console.ResetColor();

            Console.Error.Write(suffix);

            Console.WriteLine();
        }
        Console.WriteLine();
    }

    textBuilder.Clear();
}
