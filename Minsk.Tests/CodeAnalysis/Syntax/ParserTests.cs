using System.Collections.Generic;

using Xunit;

namespace Minsk.CodeAnalysis.Syntax;

public class ParserTests
{
    [Theory]
    [MemberData(nameof(GetBinaryOperatorPairData))]
    public void Parser_BinaryExpression_HonoursPrecedences(SyntaxKind op1, SyntaxKind op2)
    {
        var op1Precedence = SyntaxFacts.GetBinaryOperatorPrecedence(op1);
        var op2Precedence = SyntaxFacts.GetBinaryOperatorPrecedence(op2);
        var op1Text = SyntaxFacts.GetText(op1);
        var op2Text = SyntaxFacts.GetText(op2);

        // input
        var text = $"a {op1Text} b {op2Text} c";

        // parse
        var expression = SyntaxTree.Parse(text).Root;

        using var e = new AssertingEnumerator(expression);

        if (op1Precedence >= op2Precedence)
        {
            // input was: a op1 b op2 c
            // parse to due to precedence:
            //      op2
            //     /  \
            //   op1  c
            //  /  \
            // a   b

            e.AssertNode(SyntaxKind.BinaryExpression); // op2 children
            e.AssertNode(SyntaxKind.BinaryExpression); // op1 childrend
            e.AssertNode(SyntaxKind.NameExpression); // a
            e.AssertToken(SyntaxKind.IdentifierToken, "a"); // a
            e.AssertToken(op1, op1Text ?? ""); // op1 token
            e.AssertNode(SyntaxKind.NameExpression); // b
            e.AssertToken(SyntaxKind.IdentifierToken, "b"); // b
            e.AssertToken(op2, op2Text ?? ""); // op2 token
            e.AssertNode(SyntaxKind.NameExpression); // c
            e.AssertToken(SyntaxKind.IdentifierToken, "c"); // c
        }
        else
        {
            // input was: a op1 b op2 c
            // parse to due to precedence:
            //   op1
            //  /  \
            // a   op2
            //    /  \
            //   b   c
            e.AssertNode(SyntaxKind.BinaryExpression); // op1 children
            e.AssertNode(SyntaxKind.NameExpression); // a
            e.AssertToken(SyntaxKind.IdentifierToken, "a"); // a
            e.AssertToken(op1, op1Text ?? ""); // op1 token
            e.AssertNode(SyntaxKind.BinaryExpression); // op2 children
            e.AssertNode(SyntaxKind.NameExpression); // b
            e.AssertToken(SyntaxKind.IdentifierToken, "b"); // b
            e.AssertToken(op2, op2Text ?? ""); // op2 token
            e.AssertNode(SyntaxKind.NameExpression); // c
            e.AssertToken(SyntaxKind.IdentifierToken, "c"); // c
        }
    }

    public static IEnumerable<object[]> GetBinaryOperatorPairData()
    {
        foreach (var op1 in SyntaxFacts.GetBinaryOperatorKinds())
        {
            foreach (var op2 in SyntaxFacts.GetBinaryOperatorKinds())
            {
                yield return new object[] { op1, op2 };
            }
        }
    }
}