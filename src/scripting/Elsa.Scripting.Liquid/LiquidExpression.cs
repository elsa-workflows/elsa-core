using Elsa.Expressions;
using Elsa.Scripting.Liquid.Services;

namespace Elsa.Scripting.Liquid
{
    public class LiquidExpression<T> : WorkflowExpression<T>
    {
        public LiquidExpression(string expression) : base(LiquidExpressionEvaluator.SyntaxName, expression)
        {
        }
    }
}