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
        var marker = isLast ? "└──" : "├──";

        writer.Write($"{indent}{marker}{node.Kind}");

        if (node is SyntaxToken t && t.Value != null)
        {
            writer.Write($" {t.Value}");
        }

        writer.WriteLine();

        indent += isLast ? "   " : "│  ";

        var lastChild = node.GetChildren().LastOrDefault();
        foreach (var child in node.GetChildren())
        {
            PrettyPrint(writer, child, indent, child == lastChild);
        }
    }
}