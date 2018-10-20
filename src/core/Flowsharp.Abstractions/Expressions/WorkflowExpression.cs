namespace Flowsharp.Expressions
{
    public class WorkflowExpression<T>
    {
        public WorkflowExpression(string syntax, string expression)
        {
            Syntax = syntax;
            Expression = expression;
        }

        public string Syntax { get; set; }
        public string Expression { get; }

        public override string ToString() => Expression;
    }
}