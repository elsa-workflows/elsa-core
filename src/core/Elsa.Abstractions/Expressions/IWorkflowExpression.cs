namespace Elsa.Expressions
{
    public interface IWorkflowExpression<T>
    {
        string Syntax { get; }
        string Expression { get; }
    }
}