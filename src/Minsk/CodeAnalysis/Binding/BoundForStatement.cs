namespace Minsk.CodeAnalysis.Binding;

internal sealed class BoundForStatement : BoundStatement
{
    public BoundForStatement(BoundVariableDeclarationStatement variableDeclaration, BoundExpression condition, BoundStatement increment, BoundStatement body)
    {
        VariableDeclaration = variableDeclaration;
        Condition = condition;
        Increment = increment;
        Body = body;
    }

    public override BoundNodeKind Kind => BoundNodeKind.ForStatement;

    public BoundVariableDeclarationStatement VariableDeclaration { get; private set; }
    public BoundExpression Condition { get; private set; }
    public BoundStatement Increment { get; }
    public BoundStatement Body { get; }
}