using Elsa.Expressions;

namespace Elsa.Scripting
{
    public interface IWorkflowScriptExpression : IWorkflowExpression
    {
        string Script { get; }
    }
    
    public interface IWorkflowScriptExpression<T> : IWorkflowScriptExpression, IWorkflowExpression<T>
    {
    }
}