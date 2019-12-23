using System;

namespace Elsa.Scripting.Liquid
{
    public class LiquidExpression : WorkflowScriptExpression
    {
        public const string ExpressionType = "Liquid";
        
        public LiquidExpression(string script, Type returnType) : base(script, ExpressionType, returnType)
        {
        }
    }
    
    public class LiquidExpression<T> : LiquidExpression, IWorkflowScriptExpression<T>
    {
        public LiquidExpression(string script) : base(script, typeof(T))
        {
        }
    }
}