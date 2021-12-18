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
                EvaluationResult result = compilation.Evaluate();
                IReadOnlyList<Diagnostic> diagnostics = result.Diagnostics;

                if (showTree)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    PrettyPrint(syntaxTree.Root);
                    Console.ResetColor();
                }

                if (!diagnostics.Any())
                {
                    Console.WriteLine(result.Value);
                }
                else
                {
                    foreach (var diagnostic in diagnostics)
                    {
                        Console.WriteLine();

                        Console.ForegroundColor = ConsoleColor.DarkRed;
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

        static void PrettyPrint(SyntaxNode node, string indent = "", bool isLast = true)
        {
            var marker = isLast ? "└──" : "├──";

            Console.Write($"{indent}{marker}{node.Kind}");

            if (node is SyntaxToken t && t.Value != null)
            {
                Console.Write($" {t.Value}");
            }

            Console.WriteLine();

            indent += isLast ? "   " : "│  ";

            var lastChild = node.GetChildren().LastOrDefault();
            foreach (var child in node.GetChildren())
            {
                PrettyPrint(child, indent, child == lastChild);
            }
        }
    }
}
