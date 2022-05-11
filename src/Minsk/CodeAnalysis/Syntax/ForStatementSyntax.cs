namespace Minsk.CodeAnalysis.Syntax;

public sealed class ForStatementSyntax : StatementSyntax
{
    public ForStatementSyntax(SyntaxToken keyword, VariableDeclarationStatementSyntax initializer, ExpressionSyntax condition, StatementSyntax increment, StatementSyntax body)
    {
        Keyword = keyword;
        Initializer = initializer;
        Condition = condition;
        Increment = increment;
        Body = body;
    }

    public override SyntaxKind Kind => SyntaxKind.ForStatement;

    public SyntaxToken Keyword { get; }
    public VariableDeclarationStatementSyntax Initializer { get; }
    public ExpressionSyntax Condition { get; }
    public StatementSyntax Increment { get; }
    public StatementSyntax Body { get; }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Keyword;
        yield return Initializer;
        yield return Condition;
        yield return Increment;
        yield return Body;
    }
}