using Elsa.Expressions.Models;
using Elsa.WorkflowContexts.Services;

namespace Elsa.WorkflowContexts.Models;

public class WorkflowContext
{
    public WorkflowContext(Type providerType)
    {
        ProviderType = providerType;
    }
    
    public Type ProviderType { get; }
}

public class WorkflowContext<T, TProvider> : WorkflowContext where TProvider:IWorkflowContextProvider
{
    public WorkflowContext() : base(typeof(TProvider))
    {
    }
    
    public T? Get(ExpressionExecutionContext context)
    {
        var workflowContexts = (IDictionary<WorkflowContext, object?>)context.GetTransientProperties()["WorkflowContexts"]!;
        return workflowContexts.TryGetValue(this, out var workflowContext) ? (T?)workflowContext : default;
    }
}