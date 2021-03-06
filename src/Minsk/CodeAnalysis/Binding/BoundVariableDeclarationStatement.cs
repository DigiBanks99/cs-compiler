namespace Minsk.CodeAnalysis.Binding;

internal sealed class BoundVariableDeclarationStatement : BoundStatement
{
    public BoundVariableDeclarationStatement(VariableSymbol variable, BoundExpression initializer)
    {
        Variable = variable;
        Initializer = initializer;
    }

    public override BoundNodeKind Kind => BoundNodeKind.VariableDeclaration;
    public VariableSymbol Variable { get; }
    public BoundExpression Initializer { get; }
}