using Minsk.CodeAnalysis.Syntax;

namespace Minsk.CodeAnalysis.Binding;

internal sealed class Binder
{
    private BoundScope _scope;

    public Binder(BoundScope? parent)
    {
        _scope = new BoundScope(parent);
    }

    public DiagnosticBag Diagnostics { get; } = new();

    public static BoundGlobalScope BindGlobalScope(BoundGlobalScope? previous, CompilationUnitSyntax syntax)
    {
        BoundScope? parentScope = CreateParentScope(previous);
        Binder binder = new(parentScope);
        BoundStatement statement = binder.BindStatement(syntax.Statement);
        ImmutableArray<VariableSymbol> variables = binder._scope.GetDeclaredVariables();
        ImmutableArray<Diagnostic> diagnostics = binder.Diagnostics.ToImmutableArray();

        if (previous?.Diagnostics.Any() ?? false)
        {
            diagnostics = diagnostics.InsertRange(0, previous.Diagnostics);
        }

        return new BoundGlobalScope(previous, diagnostics, variables, statement);
    }

    private BoundStatement BindStatement(StatementSyntax syntax)
    {
        return syntax.Kind switch
        {
            SyntaxKind.BlockStatement => BindBlockStatement((BlockStatementSyntax)syntax),
            SyntaxKind.VariableDeclaration => BindVariableDeclarationStatement((VariableDeclarationStatementSyntax)syntax),
            SyntaxKind.IfStatement => BindIfStatement((IfStatementSyntax)syntax),
            SyntaxKind.WhileStatement => BindWhileStatement((WhileStatementSyntax)syntax),
            SyntaxKind.ForStatement => BindForStatement((ForStatementSyntax)syntax),
            SyntaxKind.ExpressionStatement => BindExpressionsStatement((ExpressionStatementSyntax)syntax),
            _ => throw new Exception($"Unexpected syntax {syntax.Kind}"),
        };
    }

    private BoundBlockStatement BindBlockStatement(BlockStatementSyntax syntax)
    {
        var statements = ImmutableArray.CreateBuilder<BoundStatement>();

        _scope = new BoundScope(_scope);

        foreach (StatementSyntax statementSyntax in syntax.Statements)
        {
            BoundStatement statement = BindStatement(statementSyntax);
            statements.Add(statement);
        }

        _scope = _scope.Parent ?? new BoundScope(_scope);

        return new BoundBlockStatement(statements.ToImmutable());
    }

    private BoundVariableDeclarationStatement BindVariableDeclarationStatement(VariableDeclarationStatementSyntax syntax)
    {
        var name = syntax.Identifier.Text;
        if (name == null)
        {
            Diagnostics.ReportUnnamedVariable(syntax.Span);
            throw new Exception($"Unnamed variable: {syntax.Span}");
        }

        var isReadOnly = syntax.Keyword.Kind == SyntaxKind.ConstKeyword;
        BoundExpression initializer = BindExpression(syntax.Initializer);
        VariableSymbol variable = new(name, isReadOnly, initializer.Type);

        if (!_scope.TryDeclare(variable))
        {
            Diagnostics.ReportVariableAlreadyDeclared(syntax.Identifier.Span, name);
        }

        return new BoundVariableDeclarationStatement(variable, initializer);
    }

    private BoundIfStatement BindIfStatement(IfStatementSyntax syntax)
    {
        BoundExpression condition = BindExpression(syntax.Condition, typeof(bool));
        BoundStatement thenStatement = BindStatement(syntax.ThenStatement);
        BoundStatement? elseStatement = syntax.ElseClause != null ? BindStatement(syntax.ElseClause.ElseStatement) : null;

        return new BoundIfStatement(condition, thenStatement, elseStatement);
    }

    private BoundWhileStatement BindWhileStatement(WhileStatementSyntax syntax)
    {
        BoundExpression condition = BindExpression(syntax.Condition, typeof(bool));
        BoundStatement statement = BindStatement(syntax.Body);

        return new BoundWhileStatement(condition, statement);
    }

    private BoundForStatement BindForStatement(ForStatementSyntax syntax)
    {
        BoundVariableDeclarationStatement variableDeclaration = BindVariableDeclarationStatement(syntax.Initializer);
        BoundExpression condition = BindExpression(syntax.Condition, typeof(bool));
        BoundStatement increment = BindStatement(syntax.Increment);
        BoundStatement body = BindStatement(syntax.Body);

        return new BoundForStatement(variableDeclaration, condition, increment, body);
    }

    private BoundExpressionStatement BindExpressionsStatement(ExpressionStatementSyntax syntax)
    {
        BoundExpression expression = BindExpression(syntax.Expression);
        return new BoundExpressionStatement(expression);
    }

    public BoundExpression BindExpression(ExpressionSyntax syntax, Type targetType)
    {
        BoundExpression expression = BindExpression(syntax);
        if (expression.Type != targetType)
        {
            Diagnostics.ReportCannotConvert(syntax.Span, expression.Type, targetType);
        }

        return expression;
    }

    public BoundExpression BindExpression(ExpressionSyntax syntax)
    {
        return syntax.Kind switch
        {
            SyntaxKind.ParenthesizedExpression => BindParenthesizedExpression((ParenthesizedExpressionSyntax)syntax),
            SyntaxKind.LiteralExpression => BindLiteralExpression((LiteralExpressionSyntax)syntax),
            SyntaxKind.BinaryExpression => BindBinaryExpression((BinaryExpressionSyntax)syntax),
            SyntaxKind.UnaryExpression => BindUnaryExpression((UnaryExpressionSyntax)syntax),
            SyntaxKind.NameExpression => BindNameExpression((NameExpressionSyntax)syntax),
            SyntaxKind.AssignmentExpression => BindAssignmentExpression((AssignmentExpressionSyntax)syntax),
            _ => throw new Exception($"Unexpected syntax {syntax.Kind}"),
        };
    }

    private static BoundScope? CreateParentScope(BoundGlobalScope? previous)
    {
        Stack<BoundGlobalScope> stack = new();
        while (previous != null)
        {
            stack.Push(previous);
            previous = previous.Previous;
        }

        BoundScope? parentScope = null;
        while (stack.Count > 0)
        {
            BoundGlobalScope globalScope = stack.Pop();
            BoundScope scope = new(parentScope);
            for (int i = 0; i < globalScope.Variables.Length; i++)
            {
                scope.TryDeclare(globalScope.Variables[i]);
            }

            parentScope = scope;
        }

        return parentScope;
    }

    private BoundExpression BindAssignmentExpression(AssignmentExpressionSyntax syntax)
    {
        var name = syntax.IdentifierToken.Text;
        if (name == null)
        {
            Diagnostics.ReportUnknownVariable(syntax.IdentifierToken.Span, name);
            throw new Exception($"Unknown variable name '{name}'");
        }
        var boundExpression = BindExpression(syntax.Expression);

        if (!_scope.TryLookup(name, out VariableSymbol? variable))
        {
            Diagnostics.ReportUndefinedName(syntax.IdentifierToken.Span, name);
            return boundExpression;
        }

        if (variable == null)
        {
            Diagnostics.ReportUndefinedName(syntax.IdentifierToken.Span, name);
            return boundExpression;
        }

        if (variable.IsReadOnly)
        {
            Diagnostics.ReportAssignmentToReadonlyVariable(syntax.EqualsToken.Span, name);
        }

        if (boundExpression.Type != variable.Type)
        {
            Diagnostics.ReportCannotConvert(syntax.Expression.Span, boundExpression.Type, variable.Type);
            return boundExpression;
        }

        return new BoundAssignmentExpression(variable, boundExpression);
    }

    private BoundExpression BindBinaryExpression(BinaryExpressionSyntax syntax)
    {
        var boundLeft = BindExpression(syntax.Left);
        var boundRight = BindExpression(syntax.Right);
        var boundOperator = BoundBinaryOperator.Bind(syntax.OperatorToken.Kind, boundLeft.Type, boundRight.Type);

        if (boundOperator == null)
        {
            Diagnostics.ReportUndefinedBinaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text, boundLeft.Type, boundRight.Type);
            return boundLeft;
        }

        return new BoundBinaryExpression(boundLeft, boundOperator, boundRight);
    }

    private static BoundExpression BindLiteralExpression(LiteralExpressionSyntax syntax)
    {
        var value = syntax.Value ?? 0;
        return new BoundLiteralExpression(value);
    }

    private BoundExpression BindNameExpression(NameExpressionSyntax syntax)
    {
        var name = syntax.IdentifierToken.Text;
        if (string.IsNullOrEmpty(name))
        {
            // This means the token was inserted by the parser and shouldn't be bound as the error was reported.
            return new BoundLiteralExpression(0);
        }

        if (_scope.TryLookup(name, out VariableSymbol? variable))
        {
            return new BoundVariableExpression(variable!);
        }

        Diagnostics.ReportUndefinedName(syntax.IdentifierToken.Span, name);
        return new BoundLiteralExpression(0);
    }

    private BoundExpression BindParenthesizedExpression(ParenthesizedExpressionSyntax syntax)
    {
        return BindExpression(syntax.Expression);
    }

    private BoundExpression BindUnaryExpression(UnaryExpressionSyntax syntax)
    {
        var boundOperand = BindExpression(syntax.Operand);
        var boundOperator = BoundUnaryOperator.Bind(syntax.OperatorToken.Kind, boundOperand.Type);

        if (boundOperator == null)
        {
            Diagnostics.ReportUndefinedUnaryOperator(syntax.OperatorToken.Span, syntax.OperatorToken.Text, boundOperand.Type);
            return boundOperand;
        }

        return new BoundUnaryExpression(boundOperator, boundOperand);
    }
}