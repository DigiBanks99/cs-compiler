using System.Collections.Immutable;

namespace Minsk.CodeAnalysis.Binding;

internal sealed class BoundScope
{
    private readonly Dictionary<string, VariableSymbol> _variables = new();

    public BoundScope(BoundScope? parent)
    {
        Parent = parent;
    }

    public BoundScope? Parent { get; }

    public bool TryDeclare(VariableSymbol variable)
    {
        if (_variables.ContainsKey(variable.Name))
        {
            return false;
        }

        _variables.Add(variable.Name, variable);
        return true;
    }

    public bool TryLookup(string name, out VariableSymbol? variable)
    {
        return _variables.TryGetValue(name, out variable)
            || Parent != null && Parent.TryLookup(name, out variable);
    }

    public ImmutableArray<VariableSymbol> GetDeclaredVariables()
    {
        return _variables.Values.ToImmutableArray();
    }
}