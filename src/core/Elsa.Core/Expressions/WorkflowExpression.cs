using System;

namespace Elsa.Expressions
{
    public class WorkflowExpression : IWorkflowExpression
    {
        public WorkflowExpression(string syntax, string expression, Type type)
        {
            Syntax = syntax;
            Expression = expression;
            Type = type;
        }

        public string Syntax { get; }
        public string Expression { get; }
        public Type Type { get; }

        public override string ToString() => Expression;
    }

    public class WorkflowExpression<T> : WorkflowExpression, IWorkflowExpression<T>
    {   
        public WorkflowExpression(string syntax, string expression) : base(syntax, expression, typeof(T))
        {
        }
    }
}