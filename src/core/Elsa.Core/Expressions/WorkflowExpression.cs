using Elsa.Expressions;

namespace Elsa.Core.Expressions
{
    public class WorkflowExpression : IWorkflowExpression
    {
        public WorkflowExpression(string syntax, string expression)
        {
            Syntax = syntax;
            Expression = expression;
        }

        public string Syntax { get; }
        public string Expression { get; }

        public override string ToString() => Expression;
    }

    public class WorkflowExpression<T> : WorkflowExpression, IWorkflowExpression<T>
    {   
        public WorkflowExpression(string syntax, string expression) : base(syntax, expression)
        {
        }
    }
}