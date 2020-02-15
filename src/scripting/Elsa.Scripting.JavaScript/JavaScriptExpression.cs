using Elsa.Expressions;

namespace Elsa.Scripting.JavaScript
{
    public class JavaScriptExpression : WorkflowExpression
    {
        public const string ExpressionType = "JavaScript";

        public JavaScriptExpression(string expression) : base(ExpressionType)
        {
            Expression = expression;
        }

        public string Expression { get; }
    }

    public class JavaScriptExpression<T> : JavaScriptExpression, IWorkflowExpression<T>
    {
        public JavaScriptExpression(string expression) : base(expression)
        {
        }
    }
}