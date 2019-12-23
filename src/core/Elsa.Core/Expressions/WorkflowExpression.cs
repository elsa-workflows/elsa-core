using System;
using Elsa.Scripting;

namespace Elsa.Expressions
{
    public class WorkflowExpression : IWorkflowExpression
    {
        public WorkflowExpression(string type, Type returnType)
        {
            Type = type;
            ReturnType = returnType;
        }

        public string Type { get; }
        public Type ReturnType { get; }
    }

    public class WorkflowExpression<T> : WorkflowExpression, IWorkflowExpression<T>
    {   
        public WorkflowExpression(string type) : base(type, typeof(T))
        {
        }
    }
}