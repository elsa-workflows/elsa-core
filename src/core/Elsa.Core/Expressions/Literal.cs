namespace Elsa.Expressions
{
    public class PlainTextExpression<T> : WorkflowExpression<T>
    {
        public PlainTextExpression(string expression) : base(PlainTextEvaluator.SyntaxName, expression)
        {
        }
    }

    public class Literal : PlainTextExpression<string>
    {
        public Literal(string expression) : base(expression)
        {
        }
    }
}