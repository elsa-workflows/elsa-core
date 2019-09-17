namespace Elsa.Expressions
{
    public class LiteralExpression<T> : WorkflowExpression<T>
    {
        public LiteralExpression(string expression) : base(LiteralEvaluator.SyntaxName, expression)
        {
        }
    }

    public class LiteralExpression : LiteralExpression<string>
    {
        public LiteralExpression(string expression) : base(expression)
        {
        }
    }
}