using System.Collections.Immutable;

namespace Minsk.CodeAnalysis.Syntax;

internal sealed class Lexer
{
    private readonly string _text;
    private int _position;
    private readonly List<string> _diagnostics = new();

    public Lexer(string text)
    {
        _text = text;
    }

    public IEnumerable<string> Diagnostics
    {
        get
        {
            return _diagnostics.ToImmutableArray();
        }
    }

    private char Current => Peek(0);
    private char Lookahead => Peek(1);

    public SyntaxToken Lex()
    {
        if (_position >= _text.Length)
        {
            return new SyntaxToken(SyntaxKind.EndOfFileToken, _position, "\0", null);
        }

        if (char.IsDigit(Current))
        {
            var start = _position;

            while (char.IsDigit(Current))
                Next();

            var length = _position - start;
            var text = _text.Substring(start, length);
            if (!int.TryParse(text, out var value))
            {
                _diagnostics.Add($"ERROR: The number {_text} isn't a valid Int32.");
            }
            return new SyntaxToken(SyntaxKind.NumberToken, start, text, value);
        }

        if (char.IsWhiteSpace(Current))
        {
            var start = _position;

            while (char.IsWhiteSpace(Current))
                Next();

            var length = _position - start;
            var text = _text.Substring(start, length);
            return new SyntaxToken(SyntaxKind.WhitespaceToken, start, text, null);
        }

        // true & false
        if (char.IsLetter(Current))
        {
            var start = _position;

            while (char.IsLetter(Current))
                Next();

            var length = _position - start;
            var text = _text.Substring(start, length);
            var kind = SyntaxFacts.GetKeywordKind(text);
            return new SyntaxToken(kind, start, text, null);
        }

        switch (Current)
        {
            case '+':
                return new SyntaxToken(SyntaxKind.PlusToken, _position++, "+", null);
            case '-':
                return new SyntaxToken(SyntaxKind.MinusToken, _position++, "-", null);
            case '*':
                return new SyntaxToken(SyntaxKind.StarToken, _position++, "*", null);
            case '/':
                return new SyntaxToken(SyntaxKind.SlashToken, _position++, "/", null);
            case '(':
                return new SyntaxToken(SyntaxKind.OpenParenthesisToken, _position++, "(", null);
            case ')':
                return new SyntaxToken(SyntaxKind.CloseParenthesisToken, _position++, ")", null);
            case '!':
                if (Lookahead == '=')
                {
                    return new SyntaxToken(SyntaxKind.BangEqualsToken, _position += 2, "!=", null);
                }
                return new SyntaxToken(SyntaxKind.BangToken, _position++, "!", null);
            case '&':
                if (Lookahead == '&')
                {
                    return new SyntaxToken(SyntaxKind.AmpersandAmpersandToken, _position += 2, "&&", null);
                }
                break;
            case '|':
                if (Lookahead == '|')
                {
                    return new SyntaxToken(SyntaxKind.PipePipeToken, _position += 2, "||", null);
                }
                break;
            case '=':
                if (Lookahead == '=')
                {
                    return new SyntaxToken(SyntaxKind.EqualsEqualsToken, _position += 2, "==", null);
                }
                break;
        }

        _diagnostics.Add($"ERROR: bad character input: '{Current}'");
        return new SyntaxToken(SyntaxKind.BadToken, _position++, _text.Substring(_position - 1, 1), null);
    }

    private void Next()
    {
        _position++;
    }

    private char Peek(int offset)
    {
        var index = _position + offset;
        if (index >= _text.Length)
        {
            return '\0';
        }
        return _text[index];
    }
}
