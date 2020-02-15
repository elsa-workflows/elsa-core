namespace Elsa.Expressions
{
    public class LiteralExpression : WorkflowExpression
    {
        public static string ExpressionType => "Literal";
        public string Expression { get; set; }
        
        public LiteralExpression(string expression) : base(ExpressionType)
        {
            Expression = expression;
        }

        public override string ToString() => Expression;
    }
    
    public class LiteralExpression<T> : LiteralExpression, IWorkflowExpression<T>
    {
        public LiteralExpression() : this("")
        {
        }
        
        public LiteralExpression(string expression) : base(expression)
        {
        }
    }
}