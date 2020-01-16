using System;
using Elsa.Expressions;

namespace Elsa.Scripting.JavaScript
{
    public class JavaScriptExpression : WorkflowExpression
    {
        public const string ExpressionType = "JavaScript";

        public JavaScriptExpression(string script, Type returnType) : base(ExpressionType, returnType)
        {
            Script = script;
        }

        public string Script { get; }
    }

    public class JavaScriptExpression<T> : JavaScriptExpression, IWorkflowExpression<T>
    {
        public JavaScriptExpression(string script) : base(script, typeof(T))
        {
        }
    }
}