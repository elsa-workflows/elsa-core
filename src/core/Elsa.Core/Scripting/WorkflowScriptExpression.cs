using System;
using Elsa.Expressions;

namespace Elsa.Scripting
{
    public class WorkflowScriptExpression : WorkflowExpression, IWorkflowScriptExpression
    {
        public WorkflowScriptExpression(string script, string type, Type returnType) : base(type, returnType)
        {
            Script = script;
        }
        
        public string Script { get; }

        public override string ToString() => Script;
    }

    public class WorkflowScriptExpression<T> : WorkflowScriptExpression, IWorkflowScriptExpression<T>
    {   
        public WorkflowScriptExpression(string type, string script) : base(type, script, typeof(T))
        {
        }
    }
}