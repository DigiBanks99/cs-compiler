using Minsk.CodeAnalysis.Binding;
using Minsk.CodeAnalysis.Syntax;

using System.Collections.Immutable;

namespace Minsk.CodeAnalysis;

public class Compilation
{
    private BoundGlobalScope? _globalScope;

    public Compilation(SyntaxTree syntax) : this(null, syntax) { }

    public Compilation(Compilation? previous, SyntaxTree syntax)
    {
        Previous = previous;
        Syntax = syntax;
    }

    public Compilation? Previous { get; }
    public SyntaxTree Syntax { get; }

    internal BoundGlobalScope GlobalScope
    {
        get
        {
            if (_globalScope == null)
            {
                var globalScope = Binder.BindGlobalScope(Previous?.GlobalScope, Syntax.Root);
                Interlocked.CompareExchange(ref _globalScope, globalScope, null);
            }

            return _globalScope;
        }
    }

    public Compilation ContinueWith(SyntaxTree syntaxTree)
    {
        return new Compilation(this, syntaxTree);
    }

    public EvaluationResult Evaluate(Dictionary<VariableSymbol, object?> variables)
    {
        var diagnostics = Syntax.Diagnostics.Concat(GlobalScope.Diagnostics).ToImmutableArray();
        if (diagnostics.Any())
        {
            return new EvaluationResult(diagnostics, null);
        }

        var evaluator = new Evaluator(GlobalScope.Expression, variables);
        var value = evaluator.Evaluate();

        return new EvaluationResult(ImmutableArray<Diagnostic>.Empty, value);
    }
}
