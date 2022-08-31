using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Implementations;

public class WorkflowBuilderFactory : IWorkflowBuilderFactory
{
    private readonly Func<IWorkflowBuilder> _factory;
    public WorkflowBuilderFactory(Func<IWorkflowBuilder> factory) => _factory = factory;
    public IWorkflowBuilder CreateBuilder() => _factory();
}