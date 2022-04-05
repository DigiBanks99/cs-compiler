namespace Minsk.CodeAnalysis.Syntax;

public sealed class VariableDeclarationStatementSyntax : StatementSyntax
{
    public VariableDeclarationStatementSyntax(SyntaxToken keyword, SyntaxToken identifier, SyntaxToken equalsToken, ExpressionSyntax initializer)
    {
        Keyword = keyword;
        Identifier = identifier;
        EqualsToken = equalsToken;
        Initializer = initializer;
    }

    public SyntaxToken Keyword { get; }
    public SyntaxToken Identifier { get; }
    public SyntaxToken EqualsToken { get; }
    public ExpressionSyntax Initializer { get; }

    public override SyntaxKind Kind => SyntaxKind.VariableDeclaration;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Keyword;
        yield return Identifier;
        yield return EqualsToken;
        yield return Initializer;
    }
}