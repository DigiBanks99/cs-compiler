namespace Minsk.CodeAnalysis.Syntax;

public sealed class CompilationUnitSyntax : SyntaxNode
{
    public CompilationUnitSyntax(ExpressionSyntax expression, SyntaxToken eofToken)
    {
        Expression = expression;
        EofToken = eofToken;
    }

    public ExpressionSyntax Expression { get; }
    public SyntaxToken EofToken { get; }

    public override SyntaxKind Kind => SyntaxKind.CompilationUnit;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Expression;
        yield return EofToken;
    }
}