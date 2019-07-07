namespace Elsa.Expressions
{
    public interface IWorkflowExpression
    {
        string Syntax { get; }
        string Expression { get; }
    }
    
    public interface IWorkflowExpression<T> : IWorkflowExpression
    {
    }
}