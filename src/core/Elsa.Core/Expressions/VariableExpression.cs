namespace Elsa.Expressions
{
    public class VariableExpression : WorkflowExpression
    {
        public static string ExpressionType => "Variable";

        public VariableExpression(string variableName) : base(ExpressionType)
        {
            VariableName = variableName;
        }

        public string VariableName { get; }
    }

    public class VariableExpression<T> : VariableExpression, IWorkflowExpression<T>
    {
        public VariableExpression(string variableName) : base(variableName)
        {
        }
    }
}