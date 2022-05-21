using Minsk.CodeAnalysis.Syntax;
using Minsk.CodeAnalysis.Text;

using System;
using System.Collections.Generic;

using Xunit;

namespace Minsk.CodeAnalysis;

public class EvaluatorTests
{
    [Theory]
    [InlineData("1;", 1)]
    [InlineData("+1;", 1)]
    [InlineData("-1;", -1)]
    [InlineData("1 + 2;", 3)]
    [InlineData("4 * 2;", 8)]
    [InlineData("1 - 2;", -1)]
    [InlineData("12 - 2;", 10)]
    [InlineData("6 / 3;", 2)]
    [InlineData("(6 / 3);", 2)]
    [InlineData("(10);", 10)]
    [InlineData("true;", true)]
    [InlineData("false;", false)]
    [InlineData("!true;", false)]
    [InlineData("!false;", true)]
    [InlineData("12 == 10;", false)]
    [InlineData("27 == 27;", true)]
    [InlineData("12 != 10;", true)]
    [InlineData("27 != 27;", false)]
    [InlineData("3 < 3;", false)]
    [InlineData("4 < 3;", false)]
    [InlineData("3 < 4;", true)]
    [InlineData("3 <= 3;", true)]
    [InlineData("4 <= 3;", false)]
    [InlineData("3 <= 4;", true)]
    [InlineData("3 > 3;", false)]
    [InlineData("4 > 3;", true)]
    [InlineData("3 > 4;", false)]
    [InlineData("3 >= 3;", true)]
    [InlineData("4 >= 3;", true)]
    [InlineData("3 >= 4;", false)]
    [InlineData("false == false;", true)]
    [InlineData("false != false;", false)]
    [InlineData("true == false;", false)]
    [InlineData("true != false;", true)]
    [InlineData("true && true;", true)]
    [InlineData("false && true;", false)]
    [InlineData("false || true;", true)]
    [InlineData("false || false;", false)]
    [InlineData("var a = 42;", 42)]
    [InlineData("var b = -4;", -4)]
    [InlineData("{ var c = 10; c * 10; }", 100)]
    [InlineData("const d = 200;", 200)]
    [InlineData("{ const x = 3; if (x < 2) { x; } else { 2; } }", 2)]
    [InlineData("{ var x = 3; if (x == 3) { x = 10; x; } }", 10)]
    [InlineData("{ var i = 10; var result = 0; while (i > 0) { result = result + i; i = i - 1; } result; }", 55)]
    [InlineData("{ var result = 0; for (var i = 10; i > 0; i = i -1;) { result = result + i; } result; }", 55)]
    [InlineData("{ var result = 5; for (var i = 10; i > 0; i = i -1;) { var result = 3; result = 20; } result; }", 5)] // ensure new scope
    public void Compilation_Evaluate_ShouldReturnTheCorrectValue(string text, object expectedValue)
    {
        // Arrange
        var syntaxTree = SyntaxTree.Parse(text);
        var compilation = new Compilation(syntaxTree);
        var variables = new Dictionary<VariableSymbol, object?> { };

        // Act
        var result = compilation.Evaluate(variables);

        // Assert
        Assert.Empty(result.Diagnostics);
        Assert.Equal(expectedValue, result.Value);
    }

    [Fact]
    public void Compilation_VariableDeclaration_Reports_Redeclaration()
    {
        // Arrange
        var text = @"
        {
            var a = 10;
            var b = 100;
            {
                var a = false;
            }
            var [b] = 10;
        }
        ";

        var expectedDiagnostic = "Variable 'b' is already declared.";

        // Assert
        AssertHasDiagnostics(text, new string[] { expectedDiagnostic });
    }

    [Fact]
    public void Compilation_Name_Reports_Undefined()
    {
        // Arrange
        var text = @"
        {
            [a] + 10;
        }
        ";

        var expectedDiagnostic = "Variable 'a' does not exist.";
        // Assert
        AssertHasDiagnostics(text, new string[] { expectedDiagnostic });
    }

    [Fact]
    public void Compilation_Name_Reports_NoErrorForInsertedToken()
    {
        // Arrange
        var text = @"[][]";

        // Assert
        AssertHasDiagnostics(text, new string[]
        {
            "Unexpected token <EndOfFileToken>, expected <IdentifierToken>.",
            "Unexpected token <EndOfFileToken>, expected <SemicolonToken>."
        });
    }

    [Fact]
    public void Compilation_Assignment_Reports_Readonly()
    {
        // Arrange
        var text = @"
        {
            const a = 10;
            a [=] 20;
        }";
        var expectedDiagnostic = "Variable 'a' is readonly and cannot be assigned a new value.";

        // Assert
        AssertHasDiagnostics(text, new string[] { expectedDiagnostic });
    }

    [Fact]
    public void Compilation_Assignment_Reports_ConversionError()
    {
        // Arrange
        var text = @"
        {
            var a = false;
            a = [20];
        }";
        var expectedDiagnostic = "Cannot convert 'System.Int32' to 'System.Boolean'.";

        // Assert
        AssertHasDiagnostics(text, new string[] { expectedDiagnostic });
    }

    [Fact]
    public void Compilation_Unary_Reports_UndefinedOperator()
    {
        // Arrange
        var text = @"[+]true;";
        var expectedDiagnostic = "Unary operator '+' is not defined for type 'System.Boolean'.";

        // Assert
        AssertHasDiagnostics(text, new string[] { expectedDiagnostic });
    }

    [Fact]
    public void Compilation_Binary_Reports_UndefinedOperator()
    {
        // Arrange
        var text = @"true [+] 10;";
        var expectedDiagnostic = "Binary operator '+' is not defined for types 'System.Boolean' and 'System.Int32'.";

        // Assert
        AssertHasDiagnostics(text, new string[] { expectedDiagnostic });
    }

    [Fact]
    public void Compilation_IfStatement_Reports_CannotConvert()
    {
        // Arrange
        var text = @"
            {
                var x = 0;
                if [(x)]
                {
                    x = 10;
                }
            }
        ";
        var expectedDiagnostic = "Cannot convert 'System.Int32' to 'System.Boolean'.";

        // Assert
        AssertHasDiagnostics(text, new string[] { expectedDiagnostic });
    }

    [Fact]
    public void Compilation_WhileStatement_Reports_CannotConvert()
    {
        // Arrange
        var text = @"
            {
                var x = 0;
                while [(x)]
                {
                    x = 10;
                }
            }
        ";
        var expectedDiagnostic = "Cannot convert 'System.Int32' to 'System.Boolean'.";

        // Assert
        AssertHasDiagnostics(text, new string[] { expectedDiagnostic });
    }

    [Fact]
    public void Compilation_ForStatement_Throws_UnnamedVariableException()
    {
        // Arrange
        var text = @"
            {
                var i = 0;
                for [true]; i < 10; i = i + 1;
                {
                    10;
                }
            }
        ";
        var expectedExceptionMessage = "Unnamed variable: 37...42";

        // Assert
        AssertException(text, expectedExceptionMessage);
    }

    [Fact]
    public void Compilation_ForStatement_Reports_CannotConvert()
    {
        // Arrange
        var text = @"
            {
                for (var i = 0; [(i)]; i = i + 1;)
                {
                    10;
                }
            }
        ";
        var expectedDiagnostic = "Cannot convert 'System.Int32' to 'System.Boolean'.";

        // Assert
        AssertHasDiagnostics(text, new string[] { expectedDiagnostic });
    }

    [Fact]
    public void Compilation_BlockStatement_Handles_InfiniteLoop()
    {
        var text = @"
        {
            [[)]][]
        ";

        // Assert
        AssertHasDiagnostics(text, new string[]
        {
            "Unexpected token <CloseParenthesisToken>, expected <IdentifierToken>.",
            "Unexpected token <CloseParenthesisToken>, expected <SemicolonToken>.",
            "Unexpected token <EndOfFileToken>, expected <CloseBraceToken>."
        });
    }

    private static void AssertHasDiagnostics(string text, string[] expectedDiagnostics)
    {
        var annotatedText = AnnotatedText.Parse(text);
        var syntaxTree = SyntaxTree.Parse(annotatedText.Text);
        Compilation compilation = new(syntaxTree);

        // Act
        EvaluationResult result = compilation.Evaluate(new Dictionary<VariableSymbol, object?>());

        // Assert
        Assert.Equal(expectedDiagnostics.Length, result.Diagnostics.Length);
        for (int i = 0; i < expectedDiagnostics.Length; i++)
        {
            Assert.Equal(expectedDiagnostics[i], result.Diagnostics[i].Message);
            Assert.Equal(annotatedText.Spans[i], result.Diagnostics[i].Span);
        }
    }

    private static void AssertException(string text, string exceptionMessage)
    {
        var annotatedText = AnnotatedText.Parse(text);
        var syntaxTree = SyntaxTree.Parse(annotatedText.Text);
        Compilation compilation = new(syntaxTree);

        // Act
        Exception ex = Assert.Throws<Exception>(() => compilation.Evaluate(new Dictionary<VariableSymbol, object?>()));

        // Assert
        Assert.Equal(exceptionMessage, ex.Message);
    }
}