using Minsk.CodeAnalysis.Binding;

namespace Minsk.CodeAnalysis;

internal sealed class Evaluator
{
    private readonly BoundStatement _root;
    private readonly Dictionary<VariableSymbol, object?> _variables;

    private object? _lastValue;

    public Evaluator(BoundStatement root, Dictionary<VariableSymbol, object?> variables)
    {
        _root = root;
        _variables = variables;
    }

    public object? Evaluate()
    {
        EvaluateStatement(_root);
        return _lastValue;
    }

    private void EvaluateStatement(BoundStatement node)
    {
        switch (node.Kind)
        {
            case BoundNodeKind.BlockStatement: EvaluateBlockStatement((BoundBlockStatement)node); break;
            case BoundNodeKind.VariableDeclaration: EvaluateVariableDeclarationStatement((BoundVariableDeclarationStatement)node); break;
            case BoundNodeKind.IfStatement: EvaluateIfStatement((BoundIfStatement)node); break;
            case BoundNodeKind.WhileStatement: EvaluateWhileStatement((BoundWhileStatement)node); break;
            case BoundNodeKind.ForStatement: EvaluateForStatement((BoundForStatement)node); break;
            case BoundNodeKind.ExpressionStatement: EvaluateExpressionStatement((BoundExpressionStatement)node); break;
            default: throw new Exception($"Unexpected node {node.Kind}");
        }
    }

    private void EvaluateBlockStatement(BoundBlockStatement node)
    {
        foreach (BoundStatement statement in node.Statements)
        {
            EvaluateStatement(statement);
        }
    }

    private void EvaluateVariableDeclarationStatement(BoundVariableDeclarationStatement node)
    {
        var value = EvaluateExpression(node.Initializer);
        _variables[node.Variable] = value;
        _lastValue = value;
    }

    private void EvaluateIfStatement(BoundIfStatement node)
    {
        bool condition = (bool?)EvaluateExpression(node.Condition) ?? false;
        if (condition)
        {
            EvaluateStatement(node.ThenStatement);
        }
        else if (node.ElseStatement != null)
        {
            EvaluateStatement(node.ElseStatement);
        }
    }

    private void EvaluateWhileStatement(BoundWhileStatement node)
    {
        while ((bool?)EvaluateExpression(node.Condition) ?? false)
        {
            EvaluateStatement(node.Body);
        }
    }

    private void EvaluateForStatement(BoundForStatement node)
    {
        var value = EvaluateExpression(node.VariableDeclaration.Initializer);
        _variables[node.VariableDeclaration.Variable] = value;
        _lastValue = value;

        while ((bool?)EvaluateExpression(node.Condition) ?? false)
        {
            EvaluateStatement(node.Body);
            EvaluateStatement(node.Increment);
        }
    }

    private void EvaluateExpressionStatement(BoundExpressionStatement node)
    {
        _lastValue = EvaluateExpression(node.Expression);
    }

    private object? EvaluateExpression(BoundExpression node)
    {
        return node.Kind switch
        {
            BoundNodeKind.LiteralExpression => EvaluateLitteralExpression((BoundLiteralExpression)node),
            BoundNodeKind.VariableExpression => EvaluateVariableExpression((BoundVariableExpression)node),
            BoundNodeKind.AssignmentExpression => EvaluateAssignmentExpression((BoundAssignmentExpression)node),
            BoundNodeKind.UnaryExpression => EvaluateUnaryExpression((BoundUnaryExpression)node),
            BoundNodeKind.BinaryExpression => EvaluateBinaryExpression((BoundBinaryExpression)node),
            _ => throw new Exception($"Unexpected node {node.Kind}"),
        };
    }

    private static object EvaluateLitteralExpression(BoundLiteralExpression n)
    {
        return n.Value;
    }

    private object? EvaluateVariableExpression(BoundVariableExpression v)
    {
        return _variables[v.Variable];
    }

    private object? EvaluateAssignmentExpression(BoundAssignmentExpression a)
    {
        var value = EvaluateExpression(a.Expression);
        _variables[a.Variable] = value;
        return value;
    }

    private object? EvaluateUnaryExpression(BoundUnaryExpression u)
    {
        var operand = EvaluateExpression(u.Operand);

        return operand == null
        ? null
        : u.Op.Kind switch
        {
            BoundUnaryOperatorKind.Identity => (int)operand,
            BoundUnaryOperatorKind.Negation => -(int)operand,
            BoundUnaryOperatorKind.LogicalNegation => !(bool)operand,
            _ => throw new Exception($"Unexpected unary operator {u.Op}")
        };
    }

    private object? EvaluateBinaryExpression(BoundBinaryExpression b)
    {
        var left = EvaluateExpression(b.Left);
        var right = EvaluateExpression(b.Right);
        return left == null || right == null
        ? null
        : b.Op.Kind switch
        {
            BoundBinaryOperatorKind.Addition => (int)left + (int)right,
            BoundBinaryOperatorKind.Subtraction => (int)left - (int)right,
            BoundBinaryOperatorKind.Multiplication => (int)left * (int)right,
            BoundBinaryOperatorKind.Division => (int)left / (int)right,
            BoundBinaryOperatorKind.LogicalAnd => (bool)left && (bool)right,
            BoundBinaryOperatorKind.LogicalOr => (bool)left || (bool)right,
            BoundBinaryOperatorKind.Equals => Equals(left, right),
            BoundBinaryOperatorKind.NotEquals => !Equals(left, right),
            BoundBinaryOperatorKind.Less => (int)left < (int)right,
            BoundBinaryOperatorKind.LessOrEquals => (int)left <= (int)right,
            BoundBinaryOperatorKind.Greater => (int)left > (int)right,
            BoundBinaryOperatorKind.GreaterOrEquals => (int)left >= (int)right,
            _ => throw new Exception($"Unexpected binary operator {b.Op}")
        };
    }
}