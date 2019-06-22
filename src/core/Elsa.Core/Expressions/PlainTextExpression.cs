namespace Elsa.Core.Expressions
{
    public class PlainTextExpression : WorkflowExpression<string>
    {
        public PlainTextExpression(string text) : base(PlainTextEvaluator.SyntaxName, text)
        {
        }
    }
}