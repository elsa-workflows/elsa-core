using System;

namespace Elsa.Expressions
{
    public class WorkflowExpression : IWorkflowExpression
    {
        public WorkflowExpression(string type, Type returnType)
        {
            Type = type;
            ReturnType = returnType;
        }

        public string Type { get; set; }
        public Type ReturnType { get; set; }
    }

    public class WorkflowExpression<T> : WorkflowExpression, IWorkflowExpression<T>
    {
        public WorkflowExpression(string type) : base(type, typeof(T))
        {
        }
    }
}