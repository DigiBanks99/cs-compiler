using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace Minsk.CodeAnalysis.Syntax;

public class LexerTests
{
    [Theory]
    [MemberData(nameof(GetTokensData))]
    public void Lexer_Lexes_Token(SyntaxKind kind, string text)
    {
        var tokens = SyntaxTree.ParseTokens(text);

        var token = Assert.Single(tokens);
        Assert.Equal(kind, token.Kind);
        Assert.Equal(text, token.Text);
    }


    [Theory]
    [MemberData(nameof(GetTokenPairsData))]
    public void Lexer_Lexes_TokenPairs(SyntaxKind t1Kind, string t1Text,
                                       SyntaxKind t2Kind, string t2Text)
    {
        var text = t1Text + t2Text;
        var tokens = SyntaxTree.ParseTokens(text).ToArray();

        Assert.Equal(2, tokens.Length);

        Assert.Equal(t1Kind, tokens[0].Kind);
        Assert.Equal(t1Text, tokens[0].Text);

        Assert.Equal(t2Kind, tokens[1].Kind);
        Assert.Equal(t2Text, tokens[1].Text);
    }


    [Theory]
    [MemberData(nameof(GetTokenPairsWithSeparatorData))]
    public void Lexer_Lexes_TokenPairsWithSeparators(SyntaxKind t1Kind, string t1Text,
                                                     SyntaxKind seperatorKind, string seperatorText,
                                                     SyntaxKind t2Kind, string t2Text)
    {
        var text = t1Text + seperatorText + t2Text;
        var tokens = SyntaxTree.ParseTokens(text).ToArray();

        Assert.Equal(3, tokens.Length);

        Assert.Equal(t1Kind, tokens[0].Kind);
        Assert.Equal(t1Text, tokens[0].Text);

        Assert.Equal(seperatorKind, tokens[1].Kind);
        Assert.Equal(seperatorText, tokens[1].Text);

        Assert.Equal(t2Kind, tokens[2].Kind);
        Assert.Equal(t2Text, tokens[2].Text);
    }

    public static IEnumerable<object[]> GetTokensData()
    {
        foreach (var token in GetTokens().Concat(GetSeparators()))
        {
            yield return new object[] { token.kind, token.text };
        }
    }

    public static IEnumerable<object[]> GetTokenPairsData()
    {
        foreach (var token in GetTokenPairs())
        {
            yield return new object[] { token.t1Kind, token.t1Text, token.t2Kind, token.t2Text };
        }
    }

    public static IEnumerable<object[]> GetTokenPairsWithSeparatorData()
    {
        foreach (var token in GetTokenPairsWithSeparator())
        {
            yield return new object[] { token.t1Kind, token.t1Text, token.separatorKind, token.separatorText, token.t2Kind, token.t2Text };
        }
    }

    private static IEnumerable<(SyntaxKind kind, string text)> GetTokens()
    {
        return new[]
        {
            (SyntaxKind.PlusToken, "+"),
            (SyntaxKind.MinusToken, "-"),
            (SyntaxKind.StarToken, "*"),
            (SyntaxKind.SlashToken, "/"),

            (SyntaxKind.BangToken, "!"),
            (SyntaxKind.EqualsToken, "="),

            (SyntaxKind.AmpersandAmpersandToken, "&&"),
            (SyntaxKind.PipePipeToken, "||"),

            (SyntaxKind.EqualsEqualsToken, "=="),
            (SyntaxKind.BangEqualsToken, "!="),

            (SyntaxKind.OpenParenthesisToken, "("),
            (SyntaxKind.CloseParenthesisToken, ")"),

            (SyntaxKind.IdentifierToken, "a"),
            (SyntaxKind.IdentifierToken, "abc"),

            (SyntaxKind.FalseKeyword, "false"),
            (SyntaxKind.TrueKeyword, "true"),

            (SyntaxKind.NumberToken, "1"),
            (SyntaxKind.NumberToken, "123")
        };
    }

    private static IEnumerable<(SyntaxKind kind, string text)> GetSeparators()
    {
        return new[]
        {
            (SyntaxKind.WhitespaceToken, " "),
            (SyntaxKind.WhitespaceToken, "   "),
            (SyntaxKind.WhitespaceToken, "\r"),
            (SyntaxKind.WhitespaceToken, "\n"),
            (SyntaxKind.WhitespaceToken, "\r\n"),
            (SyntaxKind.WhitespaceToken, "\t")
        };
    }

    private static IEnumerable<(SyntaxKind t1Kind, string t1Text, SyntaxKind t2Kind, string t2Text)> GetTokenPairs()
    {
        foreach (var t1 in GetTokens())
        {
            foreach (var t2 in GetTokens())
            {
                if (!RequiresSeperator(t1.kind, t2.kind))
                {
                    yield return (t1.kind, t1.text, t2.kind, t2.text);
                }
            }
        }
    }

    private static IEnumerable<(SyntaxKind t1Kind, string t1Text, SyntaxKind separatorKind, string separatorText, SyntaxKind t2Kind, string t2Text)> GetTokenPairsWithSeparator()
    {
        foreach (var t1 in GetTokens())
        {
            foreach (var t2 in GetTokens())
            {
                if (RequiresSeperator(t1.kind, t2.kind))
                {
                    foreach (var s in GetSeparators())
                    {
                        yield return (t1.kind, t1.text, s.kind, s.text, t2.kind, t2.text);
                    }
                }
            }
        }
    }

    private static bool RequiresSeperator(SyntaxKind t1Kind, SyntaxKind t2Kind)
    {
        var t1IsKeyword = t1Kind.ToString().EndsWith("Keyword");
        var t2IsKeyword = t2Kind.ToString().EndsWith("Keyword");

        if (SyntaxKind.IdentifierToken == t1Kind && SyntaxKind.IdentifierToken == t2Kind)
        {
            return true;
        }

        if (t1IsKeyword && t2IsKeyword)
        {
            return true;
        }

        if (t1IsKeyword && SyntaxKind.IdentifierToken == t2Kind)
        {
            return true;
        }

        if (t2IsKeyword && SyntaxKind.IdentifierToken == t1Kind)
        {
            return true;
        }

        if (SyntaxKind.NumberToken == t1Kind && SyntaxKind.NumberToken == t2Kind)
        {
            return true;
        }

        if (SyntaxKind.BangToken == t1Kind && SyntaxKind.EqualsToken == t2Kind)
        {
            return true;
        }

        // != = is okay
        if (SyntaxKind.BangToken == t1Kind && SyntaxKind.EqualsEqualsToken == t2Kind)
        {
            return true;
        }

        if (SyntaxKind.EqualsToken == t1Kind && SyntaxKind.EqualsToken == t2Kind)
        {
            return true;
        }

        // == = is okay
        if (SyntaxKind.EqualsToken == t1Kind && SyntaxKind.EqualsEqualsToken == t2Kind)
        {
            return true;
        }

        return false;
    }
}