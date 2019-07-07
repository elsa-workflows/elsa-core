namespace Elsa.Core.Expressions
{
    public class PlainTextExpression<T> : WorkflowExpression<T>
    {
        public PlainTextExpression(string expression) : base(PlainTextEvaluator.SyntaxName, expression)
        {
        }
    }

    public class PlainTextExpression : PlainTextExpression<string>
    {
        public PlainTextExpression(string expression) : base(expression)
        {
        }
    }
}