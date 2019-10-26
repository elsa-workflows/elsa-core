using Elsa.Expressions;

namespace Elsa.Scripting.Liquid
{
    public class LiquidExpression<T> : WorkflowExpression<T>
    {
        public LiquidExpression(string expression) : base(LiquidExpressionEvaluator.SyntaxName, expression)
        {
        }
    }
}