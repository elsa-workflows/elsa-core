namespace Flowsharp.Expressions
{
    public class WorkflowExpression<T>
    {
        public WorkflowExpression()
        {
        }
        
        public WorkflowExpression(string syntax, string expression)
        {
            Syntax = syntax;
            Expression = expression;
        }

        public string Syntax { get; set; }
        public string Expression { get; set; }

        public override string ToString() => Expression;
    }
}