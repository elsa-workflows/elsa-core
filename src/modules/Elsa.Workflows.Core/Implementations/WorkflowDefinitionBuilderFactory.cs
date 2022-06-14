using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Implementations;

public class WorkflowDefinitionBuilderFactory : IWorkflowDefinitionBuilderFactory
{
    private readonly Func<IWorkflowDefinitionBuilder> _factory;
    public WorkflowDefinitionBuilderFactory(Func<IWorkflowDefinitionBuilder> factory) => _factory = factory;
    public IWorkflowDefinitionBuilder CreateBuilder() => _factory();
}