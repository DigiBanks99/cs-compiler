using System.Collections.Immutable;

namespace Minsk.CodeAnalysis;

public sealed class EvaluationResult
{
    public EvaluationResult(IEnumerable<string> diagnostics, object? value)
    {
        Diagnostics = diagnostics.ToImmutableArray();
        Value = value;
    }

    public IReadOnlyList<string> Diagnostics { get; }
    public object? Value { get; }
}
