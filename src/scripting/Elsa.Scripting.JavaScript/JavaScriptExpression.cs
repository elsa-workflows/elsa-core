using Elsa.Expressions;

namespace Elsa.Scripting.JavaScript
{
    public class JavaScriptExpression<T> : WorkflowExpression<T>
    {
        public JavaScriptExpression(string expression) : base(JavaScriptExpressionEvaluator.SyntaxName, expression)
        {
        }
    }
}