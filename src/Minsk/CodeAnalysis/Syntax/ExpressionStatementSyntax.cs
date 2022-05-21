namespace Minsk.CodeAnalysis.Syntax;

public sealed class ExpressionStatementSyntax : StatementSyntax
{
    public ExpressionStatementSyntax(ExpressionSyntax expression) : this(expression, null)
    {
    }

    public ExpressionStatementSyntax(ExpressionSyntax expression, SyntaxToken? semicolonToken)
    {
        Expression = expression;
        SemicolonToken = semicolonToken;
    }

    public override SyntaxKind Kind => SyntaxKind.ExpressionStatement;

    public ExpressionSyntax Expression { get; private set; }
    public SyntaxToken? SemicolonToken { get; private set; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Expression;
        if (SemicolonToken != null)
        {
            yield return SemicolonToken;
        }
    }
}
