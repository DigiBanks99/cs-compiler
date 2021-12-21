using System.Collections.Immutable;

namespace Minsk.CodeAnalysis;

public sealed class EvaluationResult
{
    public EvaluationResult(IEnumerable<Diagnostic> diagnostics, object? value)
    {
        Diagnostics = diagnostics.ToImmutableArray();
        Value = value;
    }

    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public object? Value { get; }
}
