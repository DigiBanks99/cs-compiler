using Minsk.CodeAnalysis;
using Minsk.CodeAnalysis.Syntax;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Minsk
{
    // 1 + 2 * 3
    //   +
    //  / \
    // 1  *
    //   / \
    //  2   3

    internal static class Program
    {
        private static void Main()
        {
            bool showTree = false;
            Dictionary<VariableSymbol, object> variables = new();
            while (true)
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    return;
                }

                if (line == "#showTree")
                {
                    showTree = !showTree;
                    Console.WriteLine(showTree ? "Showing parse trees." : "Not showing parse trees");
                    continue;
                }
                else if (line == "#cls")
                {
                    Console.Clear();
                    continue;
                }

                SyntaxTree syntaxTree = SyntaxTree.Parse(line);
                Compilation compilation = new(syntaxTree);
                EvaluationResult result = compilation.Evaluate(variables);
                IReadOnlyList<Diagnostic> diagnostics = result.Diagnostics;

                if (showTree)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    syntaxTree.Root.WriteTo(Console.Out);
                    Console.ResetColor();
                }

                if (!diagnostics.Any())
                {
                    Console.WriteLine(result.Value);
                }
                else
                {
                    var text = syntaxTree.Text;

                    foreach (var diagnostic in diagnostics)
                    {
                        var lineIndex = text.GetLineIndex(diagnostic.Span.Start);
                        var lineNumber = lineIndex + 1;
                        var character = diagnostic.Span.Start - text.Lines[lineIndex].Start + 1;

                        Console.WriteLine();

                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.Error.Write($"({lineNumber}, {character}): ");
                        Console.Error.WriteLine(diagnostic);
                        Console.ResetColor();

                        var prefix = line[..diagnostic.Span.Start];
                        var error = line.Substring(diagnostic.Span.Start, diagnostic.Span.Length);
                        var suffix = line[diagnostic.Span.End..];

                        Console.Error.Write($"   {prefix}");

                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.Error.Write(error);
                        Console.ResetColor();

                        Console.Error.Write(suffix);

                        Console.WriteLine();
                    }
                    Console.WriteLine();
                }
            }
        }
    }
}
