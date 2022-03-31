using Minsk.CodeAnalysis.Syntax;
using Minsk.CodeAnalysis.Text;

using System.Collections;

namespace Minsk.CodeAnalysis;

internal sealed class DiagnosticBag : IEnumerable<Diagnostic>
{
    private readonly List<Diagnostic> _diagnostics = new();

    public IEnumerator<Diagnostic> GetEnumerator() => _diagnostics.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void AddRange(DiagnosticBag diagnostics)
    {
        _diagnostics.AddRange(diagnostics._diagnostics);
    }

    public void ReportBadCharacter(int position, char character)
    {
        var message = $"Bad character input: '{character}'.";
        TextSpan span = new(position, 1);
        Report(span, message);
    }

    public void ReportInvalidNumber(TextSpan span, string text, Type type)
    {
        var message = $"The number {text} isn't a valid {type}.";
        Report(span, message);
    }

    public void ReportUndefinedBinaryOperator(TextSpan span, string? operatorText, Type leftType, Type rightType)
    {
        var message = $"Binary operator '{operatorText}' is not defined for types {leftType} and {rightType}.";
        Report(span, message);
    }

    public void ReportUndefinedName(TextSpan span, string name)
    {
        var message = $"Variable '{name}' does not exist.";
        Report(span, message);
    }

    public void ReportUndefinedUnaryOperator(TextSpan span, string? operatorText, Type type)
    {
        var message = $"Unary operator '{operatorText}' is not defined for type {type}.";
        Report(span, message);
    }

    public void ReportUnexpectedToken(TextSpan span, SyntaxKind actualKind, SyntaxKind expectedKind)
    {
        var message = $"Unexpected token <{actualKind}>, expected <{expectedKind}>.";
        Report(span, message);
    }

    public void ReportUnknownVariable(TextSpan span, string? name)
    {
        var message = $"Reference of an unknown variable '{name}'.";
        Report(span, message);
    }

    private void Report(TextSpan span, string message)
    {
        var diagnostic = new Diagnostic(span, message);
        _diagnostics.Add(diagnostic);
    }
}
