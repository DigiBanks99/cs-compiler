using System.Collections.Immutable;

namespace Minsk.CodeAnalysis.Syntax;

public sealed class BlockStatementSyntax : StatementSyntax
{
    public BlockStatementSyntax(SyntaxToken openBraceToken, ImmutableArray<StatementSyntax> statements, SyntaxToken closeBraceToken)
    {
        OpenBraceToken = openBraceToken;
        Statements = statements;
        CloseBraceToken = closeBraceToken;
    }
    public override SyntaxKind Kind => SyntaxKind.BlockStatement;
    public SyntaxToken OpenBraceToken { get; private set; }
    public ImmutableArray<StatementSyntax> Statements { get; private set; }
    public SyntaxToken CloseBraceToken { get; private set; }


    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return OpenBraceToken;
        foreach (StatementSyntax statement in Statements)
        {
            yield return statement;
        }
        yield return CloseBraceToken;
    }
}