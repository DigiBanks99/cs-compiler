using System.Collections.Immutable;

namespace Minsk.CodeAnalysis.Syntax;

public sealed class SyntaxTree
{
    public SyntaxTree(IEnumerable<Diagnostic> diagnostics, ExpressionSyntax root, SyntaxToken endOfFileToken)
    {
        Diagnostics = diagnostics.ToImmutableArray();
        Root = root;
        EndOfFileToken = endOfFileToken;
    }

    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public ExpressionSyntax Root { get; }
    public SyntaxToken EndOfFileToken { get; }

    public static SyntaxTree Parse(string text)
    {
        var parser = new Parser(text);
        return parser.Parse();
    }

    public static IEnumerable<SyntaxToken> ParseTokens(string text)
    {
        var lexer = new Lexer(text);
        while (true)
        {
            SyntaxToken token = lexer.Lex();
            if (token.Kind == SyntaxKind.EndOfFileToken)
            {
                break;
            }

            yield return token;
        }
    }
}