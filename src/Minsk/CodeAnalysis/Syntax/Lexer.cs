using Minsk.CodeAnalysis.Text;

namespace Minsk.CodeAnalysis.Syntax;

internal sealed class Lexer
{
    private readonly SourceText _text;

    private int _position;

    private int _start;
    private SyntaxKind _kind;
    private object? _value;

    public Lexer(SourceText text)
    {
        _text = text;
    }

    public DiagnosticBag Diagnostics { get; } = new();

    private char Current => Peek(0);
    private char Lookahead => Peek(1);

    public SyntaxToken Lex()
    {
        _start = _position;
        _kind = SyntaxKind.BadToken;
        _value = null;

        switch (Current)
        {
            case '\0':
                _kind = SyntaxKind.EndOfFileToken;
                break;
            case '+':
                _kind = SyntaxKind.PlusToken;
                _position++;
                break;
            case '-':
                _kind = SyntaxKind.MinusToken;
                _position++;
                break;
            case '*':
                _kind = SyntaxKind.StarToken;
                _position++;
                break;
            case '/':
                _kind = SyntaxKind.SlashToken;
                _position++;
                break;
            case '(':
                _kind = SyntaxKind.OpenParenthesisToken;
                _position++;
                break;
            case ')':
                _kind = SyntaxKind.CloseParenthesisToken;
                _position++;
                break;
            case '{':
                _kind = SyntaxKind.OpenBraceToken;
                _position++;
                break;
            case '}':
                _kind = SyntaxKind.CloseBraceToken;
                _position++;
                break;
            case '&':
                if (Lookahead == '&')
                {
                    _kind = SyntaxKind.AmpersandAmpersandToken;
                    _position += 2;
                    break;
                }
                break;
            case '|':
                if (Lookahead == '|')
                {
                    _kind = SyntaxKind.PipePipeToken;
                    _position += 2;
                    break;
                }
                break;
            case '=':
                _position++;
                if (Current != '=')
                {
                    _kind = SyntaxKind.EqualsToken;
                }
                else
                {
                    _position++;
                    _kind = SyntaxKind.EqualsEqualsToken;
                }
                break;
            case '!':
                _position++;
                if (Current != '=')
                {
                    _kind = SyntaxKind.BangToken;
                }
                else
                {
                    _position++;
                    _kind = SyntaxKind.BangEqualsToken;
                }
                break;
            case '<': 
                _position++;
                if (Current != '=')
                {
                    _kind = SyntaxKind.LessToken;
                }
                else
                {
                    _position++;
                    _kind = SyntaxKind.LessOrEqualsToken;
                }break;
            case '>': 
                _position++;
                if (Current != '=')
                {
                    _kind = SyntaxKind.GreaterToken;
                }
                else
                {
                    _position++;
                    _kind = SyntaxKind.GreaterOrEqualsToken;
                }break;
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
                ReadNumberToken();
                break;
            case ' ':
            case '\t':
            case '\n':
            case '\r':
                ReadWhiteSpaceToken();
                break;
            default:
                if (char.IsLetter(Current))
                {
                    ReadIdentifierOrKeyword();
                }
                else if (char.IsWhiteSpace(Current))
                {
                    ReadWhiteSpaceToken();
                }
                else
                {
                    Diagnostics.ReportBadCharacter(_position, Current);
                    _position++;
                }
                break;
        }

        int length = _position - _start;
        string? text = SyntaxFacts.GetText(_kind);
        if (text == null)
        {
            text = _text.ToString(_start, length);
        }
        return new SyntaxToken(_kind, _start, text, _value);
    }

    private char Peek(int offset)
    {
        var index = _position + offset;
        return index >= _text.Length
             ? '\0'
             : _text[index];
    }

    private void ReadIdentifierOrKeyword()
    {
        while (char.IsLetter(Current))
        {
            _position++;
        }

        var length = _position - _start;
        var text = _text.ToString(_start, length);
        _kind = SyntaxFacts.GetKeywordKind(text);
    }

    private void ReadNumberToken()
    {
        while (char.IsDigit(Current))
        {
            _position++;
        }

        var length = _position - _start;
        var text = _text.ToString(_start, length);
        if (!int.TryParse(text, out var value))
        {
            Diagnostics.ReportInvalidNumber(new TextSpan(_start, length), text, typeof(int));
        }

        _value = value;
        _kind = SyntaxKind.NumberToken;
    }

    private void ReadWhiteSpaceToken()
    {
        while (char.IsWhiteSpace(Current))
        {
            _position++;
        }

        _kind = SyntaxKind.WhitespaceToken;
    }

}
