using Minsk.CodeAnalysis.Text;

namespace Minsk.CodeAnalysis.Syntax;

internal sealed class Parser
{
    private readonly ImmutableArray<SyntaxToken> _tokens;
    readonly SourceText _text;
    private int _position;

    public Parser(SourceText text)
    {
        var tokens = new List<SyntaxToken>();

        var lexer = new Lexer(text);
        SyntaxToken token;
        do
        {
            token = lexer.Lex();

            if (token.Kind != SyntaxKind.BadToken &&
                token.Kind != SyntaxKind.WhitespaceToken)
            {
                tokens.Add(token);
            }
        }
        while (token.Kind != SyntaxKind.EndOfFileToken);

        _text = text;
        _tokens = tokens.ToImmutableArray();
        Diagnostics.AddRange(lexer.Diagnostics);
    }

    public DiagnosticBag Diagnostics { get; } = new();

    private SyntaxToken Current => Peek(0);

    public CompilationUnitSyntax ParseCompilationUnit()
    {
        StatementSyntax statement = ParseStatement();
        SyntaxToken eofToken = MatchToken(SyntaxKind.EndOfFileToken);
        return new CompilationUnitSyntax(statement, eofToken);
    }

    private StatementSyntax ParseStatement()
    {
        return Current.Kind switch
        {
            SyntaxKind.OpenBraceToken => ParseBlockStatement(),
            SyntaxKind.ConstKeyword or SyntaxKind.VarKeyword => ParseVariableDeclarationStatement(),
            SyntaxKind.IfKeyword => ParseIfStatement(),
            _ => ParseExpressionStatement(),
        };
    }

    private BlockStatementSyntax ParseBlockStatement()
    {
        var statements = ImmutableArray.CreateBuilder<StatementSyntax>();

        SyntaxToken openBraceToken = MatchToken(SyntaxKind.OpenBraceToken);

        while (Current.Kind != SyntaxKind.EndOfFileToken
            && Current.Kind != SyntaxKind.CloseBraceToken)
        {
            StatementSyntax statement = ParseStatement();
            statements.Add(statement);
        }

        SyntaxToken closeBraceToken = MatchToken(SyntaxKind.CloseBraceToken);
        return new BlockStatementSyntax(openBraceToken, statements.ToImmutable(), closeBraceToken);
    }

    private VariableDeclarationStatementSyntax ParseVariableDeclarationStatement()
    {
        SyntaxKind expected = Current.Kind == SyntaxKind.ConstKeyword ? SyntaxKind.ConstKeyword : SyntaxKind.VarKeyword;
        SyntaxToken keyword = MatchToken(expected);
        SyntaxToken identifier = MatchToken(SyntaxKind.IdentifierToken);
        SyntaxToken equals = MatchToken(SyntaxKind.EqualsToken);
        ExpressionSyntax initializer = ParseExpression();

        return new VariableDeclarationStatementSyntax(keyword, identifier, equals, initializer);
    }

    private IfStatementSyntax ParseIfStatement()
    {
        SyntaxToken ifKeyword = MatchToken(SyntaxKind.IfKeyword);
        ExpressionSyntax condition = ParseExpression();
        StatementSyntax thenStatement = ParseStatement();
        ElseClauseSyntax? elseStatement = ParseElseClause();

        return new IfStatementSyntax(ifKeyword, condition, thenStatement, elseStatement);
    }

    private ElseClauseSyntax? ParseElseClause()
    {
        if (Current.Kind != SyntaxKind.ElseKeyword)
        {
            return null;
        }

        SyntaxToken elseKeyword = MatchToken(SyntaxKind.ElseKeyword);
        StatementSyntax elseStatement = ParseStatement();
        return new ElseClauseSyntax(elseKeyword, elseStatement);
    }

    private ExpressionStatementSyntax ParseExpressionStatement()
    {
        ExpressionSyntax expression = ParseExpression();
        return new ExpressionStatementSyntax(expression);
    }

    private ExpressionSyntax ParseExpression()
    {
        return ParseAssingmentExpression();
    }

    private ExpressionSyntax ParseAssingmentExpression()
    {
        // a + b + 5
        //
        //     +
        //    / \
        //   +  5
        //  /\
        // a b
        //
        // a = b = 5
        //
        //     =
        //    / \
        //   a  =
        //     /\
        //    b 5

        if (Peek(0).Kind == SyntaxKind.IdentifierToken
         && Peek(1).Kind == SyntaxKind.EqualsToken)
        {
            var identifierToken = NextToken();
            var operatorToken = NextToken();
            var right = ParseAssingmentExpression();
            return new AssignmentExpressionSyntax(identifierToken, operatorToken, right);
        }

        return ParseBinaryExpression();
    }

    private ExpressionSyntax ParseBinaryExpression(int parentPrecedence = 0)
    {
        ExpressionSyntax left;
        var unaryOperatorPrecedence = Current.Kind.GetUnaryOperatorPrecedence();
        if (unaryOperatorPrecedence != 0 && unaryOperatorPrecedence >= parentPrecedence)
        {
            var operatorToken = NextToken();
            var operand = ParseBinaryExpression(unaryOperatorPrecedence);
            left = new UnaryExpressionSyntax(operatorToken, operand);
        }
        else
        {
            left = ParsePrimaryExpression();
        }

        while (true)
        {
            var precedence = Current.Kind.GetBinaryOperatorPrecedence();
            if (precedence == 0 || precedence <= parentPrecedence)
            {
                break;
            }

            var operatorToken = NextToken();
            var right = ParseBinaryExpression(precedence);
            left = new BinaryExpressionSyntax(left, operatorToken, right);
        }

        return left;
    }

    private ExpressionSyntax ParsePrimaryExpression()
    {
        return Current.Kind switch
        {
            SyntaxKind.OpenParenthesisToken => ParseParenthesizedExpression(),
            SyntaxKind.FalseKeyword or SyntaxKind.TrueKeyword => ParseBooleanLiteral(),
            SyntaxKind.NumberToken => ParseNumberLiteral(),
            _ => ParseNameExpression(),
        };
    }

    private ExpressionSyntax ParseParenthesizedExpression()
    {
        var left = MatchToken(SyntaxKind.OpenParenthesisToken);
        var expression = ParseExpression();
        var right = MatchToken(SyntaxKind.CloseParenthesisToken);
        return new ParenthesizedExpressionSyntax(left, expression, right);
    }

    private ExpressionSyntax ParseBooleanLiteral()
    {
        var isTrue = Current.Kind == SyntaxKind.TrueKeyword;
        var keywordToken = isTrue ? MatchToken(SyntaxKind.TrueKeyword) : MatchToken(SyntaxKind.FalseKeyword);
        return new LiteralExpressionSyntax(keywordToken, isTrue);
    }

    private ExpressionSyntax ParseNumberLiteral()
    {
        var numberToken = MatchToken(SyntaxKind.NumberToken);
        return new LiteralExpressionSyntax(numberToken);
    }

    private ExpressionSyntax ParseNameExpression()
    {
        var identifierToken = MatchToken(SyntaxKind.IdentifierToken);
        return new NameExpressionSyntax(identifierToken);
    }

    private SyntaxToken MatchToken(SyntaxKind kind)
    {
        if (Current.Kind == kind)
        {
            return NextToken();
        }

        Diagnostics.ReportUnexpectedToken(Current.Span, Current.Kind, kind);
        return new SyntaxToken(kind, Current.Position, null, null);
    }

    private SyntaxToken NextToken()
    {
        var current = Current;
        _position++;
        return current;
    }

    private SyntaxToken Peek(int offset)
    {
        var index = _position + offset;
        return index >= _tokens.Length
             ? _tokens[^1]
             : _tokens[index];
    }
}
