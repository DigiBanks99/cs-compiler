using Minsk.CodeAnalysis.Text;

namespace Minsk.CodeAnalysis.Syntax;

internal sealed class Parser
{
    private readonly ImmutableArray<SyntaxToken> _tokens;
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
            SyntaxKind.WhileKeyword => ParseWhileStatement(),
            SyntaxKind.ForKeyword => ParseForStatement(),
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
            SyntaxToken startToken = Current;

            StatementSyntax statement = ParseStatement();
            statements.Add(statement);

            // To prevent infinite loop, we need to make sure that the current token is different from
            // the start token. If the token is the same, we'll already have tried to parse the token,
            // so lets not report errors as we've already tried to parse a statement and reported one.
            if (Current == startToken)
            {
                NextToken();
            }
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
        SyntaxToken semicolon = MatchToken(SyntaxKind.SemicolonToken);

        return new VariableDeclarationStatementSyntax(keyword, identifier, equals, initializer, semicolon);
    }

    private IfStatementSyntax ParseIfStatement()
    {
        SyntaxToken ifKeyword = MatchToken(SyntaxKind.IfKeyword);
        ExpressionSyntax condition = ParseExpression();
        StatementSyntax thenStatement = ParseStatement();
        ElseClauseSyntax? elseStatement = ParseElseClause();

        return new IfStatementSyntax(ifKeyword, condition, thenStatement, elseStatement);
    }

    private WhileStatementSyntax ParseWhileStatement()
    {
        SyntaxToken whileKeyword = MatchToken(SyntaxKind.WhileKeyword);
        ExpressionSyntax condition = ParseExpression();
        StatementSyntax body = ParseStatement();

        return new WhileStatementSyntax(whileKeyword, condition, body);
    }

    private ForStatementSyntax ParseForStatement()
    {
        SyntaxToken forKeyword = MatchToken(SyntaxKind.ForKeyword);
        MatchToken(SyntaxKind.OpenParenthesisToken);
        VariableDeclarationStatementSyntax initializer = ParseVariableDeclarationStatement();
        ExpressionSyntax condition = ParseExpression();
        MatchToken(SyntaxKind.SemicolonToken);
        StatementSyntax increment = ParseStatement();
        MatchToken(SyntaxKind.CloseParenthesisToken);
        StatementSyntax body = ParseStatement();

        return new ForStatementSyntax(forKeyword, initializer, condition, increment, body);
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
        SyntaxToken semicolonToken = MatchToken(SyntaxKind.SemicolonToken);
        return new ExpressionStatementSyntax(expression, semicolonToken);
    }

    private ExpressionSyntax ParseExpression()
    {
        return ParseAssignmentExpression();
    }

    private ExpressionSyntax ParseAssignmentExpression()
    {
        // a + b + 5;
        //
        //     +
        //    / \
        //   +  5
        //  /\
        // a b
        //
        // a = b = 5;
        //
        //     =
        //    / \
        //   a  =
        //     /\
        //    b 5

        if (Peek(0).Kind == SyntaxKind.IdentifierToken
         && Peek(1).Kind == SyntaxKind.EqualsToken)
        {
            SyntaxToken identifierToken = NextToken();
            SyntaxToken operatorToken = NextToken();
            ExpressionSyntax right = ParseAssignmentExpression();
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
            SyntaxToken operatorToken = NextToken();
            ExpressionSyntax operand = ParseBinaryExpression(unaryOperatorPrecedence);
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

            SyntaxToken operatorToken = NextToken();
            ExpressionSyntax right = ParseBinaryExpression(precedence);
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
        SyntaxToken left = MatchToken(SyntaxKind.OpenParenthesisToken);
        ExpressionSyntax expression = ParseExpression();
        SyntaxToken right = MatchToken(SyntaxKind.CloseParenthesisToken);
        return new ParenthesizedExpressionSyntax(left, expression, right);
    }

    private ExpressionSyntax ParseBooleanLiteral()
    {
        bool isTrue = Current.Kind == SyntaxKind.TrueKeyword;
        SyntaxToken keywordToken = isTrue ? MatchToken(SyntaxKind.TrueKeyword) : MatchToken(SyntaxKind.FalseKeyword);
        return new LiteralExpressionSyntax(keywordToken, isTrue);
    }

    private ExpressionSyntax ParseNumberLiteral()
    {
        SyntaxToken numberToken = MatchToken(SyntaxKind.NumberToken);
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
