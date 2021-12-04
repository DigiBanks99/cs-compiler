namespace Minsk.CodeAnalysis
{
    public enum SyntaxKind
    {
        // Tokens
        BadToken,
        EndOfFileToken,
        WhitespaceToken,
        LiteralToken,
        PlusToken,
        MinusToken,
        StarToken,
        SlashToken,
        OpenParenthesisToken,
        CloseParenthesisToken,

        // Expressions
        LiteralExpression,
        BinaryExpression,
        ParenthesizedExpression
    }
}