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
        var expression = ParseExpression(text);

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

    [Theory]
    [MemberData(nameof(GetUnaryOperatorPairData))]
    public void Parser_UnaryExpression_HonoursPrecedences(SyntaxKind unaryKind, SyntaxKind binaryKind)
    {
        var unaryPrecedence = SyntaxFacts.GetUnaryOperatorPrecedence(unaryKind);
        var binaryPrecedence = SyntaxFacts.GetBinaryOperatorPrecedence(binaryKind);
        var unaryText = SyntaxFacts.GetText(unaryKind);
        var binaryText = SyntaxFacts.GetText(binaryKind);

        // input
        var text = $"{unaryText} a {binaryText} b";

        // parse
        var expression = ParseExpression(text);

        using var e = new AssertingEnumerator(expression);

        if (unaryPrecedence >= binaryPrecedence)
        {
            // input was: un a bin b
            // parse to due to precedence:
            //    bin
            //   /  \
            //  un  b
            //  |
            // a

            e.AssertNode(SyntaxKind.BinaryExpression); // binary children
            e.AssertNode(SyntaxKind.UnaryExpression); // unary childrend
            e.AssertToken(unaryKind, unaryText ?? ""); // unary token
            e.AssertNode(SyntaxKind.NameExpression); // a
            e.AssertToken(SyntaxKind.IdentifierToken, "a"); // a
            e.AssertToken(binaryKind, binaryText ?? ""); // binary token
            e.AssertNode(SyntaxKind.NameExpression); // b
            e.AssertToken(SyntaxKind.IdentifierToken, "b"); // b
        }
        else
        {
            // input was: un a bin b
            // parse to due to precedence:
            //    un
            //    |
            //   bin
            //  /  \
            // a   b
            e.AssertNode(SyntaxKind.UnaryExpression); // unary children
            e.AssertToken(unaryKind, unaryText ?? ""); // unary token
            e.AssertNode(SyntaxKind.BinaryExpression); // binary children
            e.AssertNode(SyntaxKind.NameExpression); // a
            e.AssertToken(SyntaxKind.IdentifierToken, "a"); // a
            e.AssertToken(binaryKind, binaryText ?? ""); // op2 token
            e.AssertNode(SyntaxKind.NameExpression); // b
            e.AssertToken(SyntaxKind.IdentifierToken, "b"); // b
        }
    }

    private static ExpressionSyntax ParseExpression(string text)
    {
        SyntaxTree syntaxTree = SyntaxTree.Parse(text);
        CompilationUnitSyntax root = syntaxTree.Root;
        return root.Expression;
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

    public static IEnumerable<object[]> GetUnaryOperatorPairData()
    {
        foreach (var unaryKind in SyntaxFacts.GetUnaryOperatorKinds())
        {
            foreach (var binaryKind in SyntaxFacts.GetBinaryOperatorKinds())
            {
                yield return new object[] { unaryKind, binaryKind };
            }
        }
    }
}