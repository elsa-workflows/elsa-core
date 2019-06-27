namespace Elsa.Core.Expressions
{
    public class PlainTextExpression : WorkflowExpression<string>
    {
        public PlainTextExpression(string expression) : base(PlainTextEvaluator.SyntaxName, expression)
        {
        }
    }
}