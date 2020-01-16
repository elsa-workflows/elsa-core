using System;
using Elsa.Expressions;

namespace Elsa.Scripting.Liquid
{
    public class LiquidExpression : WorkflowExpression
    {
        public const string ExpressionType = "Liquid";

        public LiquidExpression(string script, Type returnType) : base(ExpressionType, returnType)
        {
            Script = script;
        }

        public string Script { get; }
    }

    public class LiquidExpression<T> : LiquidExpression, IWorkflowExpression<T>
    {
        public LiquidExpression(string script) : base(script, typeof(T))
        {
        }
    }
}