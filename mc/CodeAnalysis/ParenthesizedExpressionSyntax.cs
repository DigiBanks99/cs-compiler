using System.Collections.Generic;

namespace Minsk.CodeAnalysis
{
    sealed class ParenthesizedExpressionSyntax : ExpressionSyntax
    {
        public ParenthesizedExpressionSyntax(SyntaxToken openParenthesizeToken, ExpressionSyntax expression, SyntaxToken closeParenthesizeToken)
        {
            OpenParenthesizeToken = openParenthesizeToken;
            Expression = expression;
            CloseParenthesizeToken = closeParenthesizeToken;
        }

        public SyntaxToken OpenParenthesizeToken { get; }
        public ExpressionSyntax Expression { get; }
        public SyntaxToken CloseParenthesizeToken { get; }

        public override SyntaxKind Kind => SyntaxKind.ParenthesizedExpression;

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenParenthesizeToken;
            yield return Expression;
            yield return CloseParenthesizeToken;
        }
    }
}