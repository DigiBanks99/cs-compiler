using Minsk.CodeAnalysis.Text;

namespace Minsk.CodeAnalysis.Syntax;

public abstract class SyntaxNode
{
    public abstract SyntaxKind Kind { get; }

    public virtual TextSpan Span
    {
        get
        {
            var first = GetChildren().First().Span;
            var last = GetChildren().Last().Span;
            return TextSpan.FromBounds(first.Start, last.End);
        }
    }

    public abstract IEnumerable<SyntaxNode> GetChildren();

    public void WriteTo(TextWriter writer)
    {
        PrettyPrint(writer, this);
    }

    public override string ToString()
    {
        using var writer = new StringWriter();
        WriteTo(writer);
        return writer.ToString();
    }

    private static void PrettyPrint(TextWriter writer, SyntaxNode node, string indent = "", bool isLast = true)
    {
        bool isToConsole = writer == Console.Out;
        var marker = isLast ? "└──" : "├──";


        if (isToConsole)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            writer.Write(indent);
            writer.Write(marker);
            Console.ForegroundColor = node is SyntaxToken ? ConsoleColor.Blue : ConsoleColor.Cyan;
            writer.Write(node.Kind);
        }
        else
        {
            writer.Write("{marker}{node.Kind}");
        }

        if (node is SyntaxToken t && t.Value != null)
        {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            writer.Write($" {t.Value}");
        }
        Console.ResetColor();

        writer.WriteLine();

        indent += isLast ? "   " : "│  ";


        var lastChild = node.GetChildren().LastOrDefault();
        foreach (var child in node.GetChildren())
        {
            PrettyPrint(writer, child, indent, child == lastChild);
        }
    }
}