using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Minsk.CodeAnalysis.Syntax;

namespace Minsk.CodeAnalysis.Binding
{

    internal sealed class Binder
    {
        private readonly List<string> _diagnostics = new();

        public BoundExpression BindExpression(ExpressionSyntax syntax)
        {
            return syntax.Kind switch
            {
                SyntaxKind.LiteralExpression => BindLiteralExpression((LiteralExpressionSyntax)syntax),
                SyntaxKind.BinaryExpression => BindBinaryExpression((BinaryExpressionSyntax)syntax),
                SyntaxKind.UnaryExpression => BindUnaryExpression((UnaryExpressionSyntax)syntax),
                _ => throw new Exception($"Unexpected syntax {syntax.Kind}"),
            };
        }

        public IReadOnlyCollection<string> Diagnostics => _diagnostics.ToImmutableList();

        private BoundExpression BindLiteralExpression(LiteralExpressionSyntax syntax)
        {
            var value = syntax.Value ?? 0;
            return new BoundLiteralExpression(value);
        }

        private BoundExpression BindBinaryExpression(BinaryExpressionSyntax syntax)
        {
            var boundLeft = BindExpression(syntax.Left);
            var boundRight = BindExpression(syntax.Right);
            var boundOperatorKind = BindBinaryOperatorKind(syntax.OperatorToken.Kind, boundLeft.Type, boundRight.Type);

            if (boundOperatorKind == null)
            {
                _diagnostics.Add($"Binary operator '{syntax.OperatorToken.Text}' is not defined for types {boundLeft.Type} and {boundRight.Type}.");
                return boundLeft;
            }

            return new BoundBinaryExpression(boundLeft, boundOperatorKind.Value, boundRight);
        }

        private BoundExpression BindUnaryExpression(UnaryExpressionSyntax syntax)
        {
            var boundOperand = BindExpression(syntax.Operand);
            var boundOperatorKind = BindUnaryOperatorKind(syntax.OperatorToken.Kind, boundOperand.Type);

            if (boundOperatorKind == null)
            {
                _diagnostics.Add($"Unary operator '{syntax.OperatorToken.Text}' is not defined for type {boundOperand.Type}.");
                return boundOperand;
            }

            return new BoundUnaryExpression(boundOperatorKind.Value, boundOperand);
        }

        private BoundBinaryOperatorKind? BindBinaryOperatorKind(SyntaxKind kind, Type leftType, Type rightType)
        {
            if (leftType == typeof(int) && rightType == typeof(int))
                return kind switch
                {
                    SyntaxKind.PlusToken => BoundBinaryOperatorKind.Addition,
                    SyntaxKind.MinusToken => BoundBinaryOperatorKind.Subtraction,
                    SyntaxKind.StarToken => BoundBinaryOperatorKind.Multiplication,
                    SyntaxKind.SlashToken => BoundBinaryOperatorKind.Division,
                    _ => throw new Exception($"Unexpected binary operator {kind}"),
                };

            if (leftType == typeof(bool) && rightType == typeof(bool))
                return kind switch
                {
                    SyntaxKind.AmpersandAmpersandToken => BoundBinaryOperatorKind.LogicalAnd,
                    SyntaxKind.PipePipeToken => BoundBinaryOperatorKind.LogicalOr,
                    _ => throw new Exception($"Unexpected binary operator {kind}"),
                };

            return null;
        }

        private BoundUnaryOperatorKind? BindUnaryOperatorKind(SyntaxKind kind, Type operandType)
        {
            if (operandType == typeof(int))
            {
                return kind switch
                {
                    SyntaxKind.PlusToken => BoundUnaryOperatorKind.Identity,
                    SyntaxKind.MinusToken => BoundUnaryOperatorKind.Negation,
                    _ => throw new Exception($"Unexpected unary operator {kind}"),
                };
            }

            if (operandType == typeof(bool))
            {
                return kind switch
                {
                    SyntaxKind.BangToken => BoundUnaryOperatorKind.LogicalNegation,
                    _ => throw new Exception($"Unexpected unary operator {kind}"),
                };
            }

            return null;
        }
    }
}