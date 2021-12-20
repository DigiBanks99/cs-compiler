using Minsk.CodeAnalysis.Binding;
using Minsk.CodeAnalysis.Syntax;

using System.Collections.Immutable;

namespace Minsk.CodeAnalysis;

public class Compilation
{
    public Compilation(SyntaxTree syntax)
    {
        Syntax = syntax;
    }

    public SyntaxTree Syntax { get; }

    public EvaluationResult Evaluate(Dictionary<string, object> variables)
    {
        var binder = new Binder(variables);
        var boundExpression = binder.BindExpression(Syntax.Root);
        var diagnostics = Syntax.Diagnostics.Concat(binder.Diagnostics).ToImmutableArray();
        if (diagnostics.Any())
        {
            return new EvaluationResult(diagnostics, null);
        }

        var evaluator = new Evaluator(boundExpression, variables);
        var value = evaluator.Evaluate();

        return new EvaluationResult(Array.Empty<Diagnostic>(), value);
    }
}
