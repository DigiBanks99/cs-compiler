using Minsk.CodeAnalysis.Text;

using System.Collections.Immutable;

namespace Minsk.CodeAnalysis.Syntax;

public sealed class SyntaxTree
{
    private SyntaxTree(SourceText text)
    {
        Parser parser = new(text);
        Root = parser.ParseCompilationUnit();
        Diagnostics = parser.Diagnostics.ToImmutableArray();

        Text = text;
    }

    public SourceText Text { get; }
    public ImmutableArray<Diagnostic> Diagnostics { get; }
    public CompilationUnitSyntax Root { get; }

    public static SyntaxTree Parse(string text)
    {
        var sourceText = SourceText.From(text);
        return Parse(sourceText);
    }

    public static SyntaxTree Parse(SourceText text)
    {
        return new SyntaxTree(text);
    }

    public static IEnumerable<SyntaxToken> ParseTokens(string text)
    {
        var sourceText = SourceText.From(text);
        return ParseTokens(sourceText);
    }

    public static IEnumerable<SyntaxToken> ParseTokens(SourceText text)
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