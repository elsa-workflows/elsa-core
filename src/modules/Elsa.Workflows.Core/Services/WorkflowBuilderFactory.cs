using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Services;

public class WorkflowBuilderFactory : IWorkflowBuilderFactory
{
    private readonly Func<IWorkflowBuilder> _factory;
    public WorkflowBuilderFactory(Func<IWorkflowBuilder> factory) => _factory = factory;
    public IWorkflowBuilder CreateBuilder() => _factory();
}