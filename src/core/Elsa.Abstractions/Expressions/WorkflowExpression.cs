namespace Elsa.Expressions
{
    public class WorkflowExpression
    {
        protected WorkflowExpression(string syntax, string expression)
        {
            Syntax = syntax;
            Expression = expression;
        }

        public string Syntax { get; }
        public string Expression { get; }

        public override string ToString() => Expression;
    }
    
    public class WorkflowExpression<T> : WorkflowExpression
    {   
        public WorkflowExpression(string syntax, string expression) : base(syntax, expression)
        {
        }
    }
}