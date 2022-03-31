using Minsk.CodeAnalysis.Syntax;

using System.Collections.Generic;

using Xunit;

namespace Minsk.CodeAnalysis;

public class EvaluatorTests
{
    [Theory]
    [InlineData("1", 1)]
    [InlineData("+1", 1)]
    [InlineData("-1", -1)]
    [InlineData("1 + 2", 3)]
    [InlineData("4 * 2", 8)]
    [InlineData("1 - 2", -1)]
    [InlineData("12 - 2", 10)]
    [InlineData("6 / 3", 2)]
    [InlineData("(6 / 3)", 2)]
    [InlineData("(10)", 10)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("!true", false)]
    [InlineData("!false", true)]
    [InlineData("12 == 10", false)]
    [InlineData("27 == 27", true)]
    [InlineData("12 != 10", true)]
    [InlineData("27 != 27", false)]
    [InlineData("false == false", true)]
    [InlineData("false != false", false)]
    [InlineData("true == false", false)]
    [InlineData("true != false", true)]
    [InlineData("true && true", true)]
    [InlineData("false && true", false)]
    [InlineData("false || true", true)]
    [InlineData("false || false", false)]
    [InlineData("a", 42)]
    [InlineData("b", -4)]
    [InlineData("(c = 10) * c", 100)]
    public void Compilation_Evaluate_ShouldReturnTheCorrectValue(string text, object expectedValue)
    {
        // Arrange
        var syntaxTree = SyntaxTree.Parse(text);
        var compilation = new Compilation(syntaxTree);
        var variables = new Dictionary<VariableSymbol, object?>
        {
            { new VariableSymbol("a", typeof(int)), 42 },
            { new VariableSymbol("b", typeof(int)), -4 }
        };

        // Act
        var result = compilation.Evaluate(variables);

        // Assert
        Assert.Empty(result.Diagnostics);
        Assert.Equal(expectedValue, result.Value);
    }
}