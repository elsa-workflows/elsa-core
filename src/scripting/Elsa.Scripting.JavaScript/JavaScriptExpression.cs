namespace Elsa.Expressions
{
    public class JavaScriptExpression<T> : WorkflowExpression<T>
    {
        public JavaScriptExpression(string expression) : base(JavaScriptEvaluator.SyntaxName, expression)
        {
        }
    }
}