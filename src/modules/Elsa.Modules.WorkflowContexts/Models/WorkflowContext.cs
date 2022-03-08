using Elsa.Models;
using Elsa.Modules.WorkflowContexts.Contracts;

namespace Elsa.Modules.WorkflowContexts.Models;

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
        var workflowContexts = (IDictionary<WorkflowContext, object?>)context.TransientProperties["WorkflowContexts"]!;
        return (T?)workflowContexts[this];
        
    }
}