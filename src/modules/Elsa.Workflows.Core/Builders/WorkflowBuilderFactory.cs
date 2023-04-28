using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Builders;

/// <inheritdoc />
public class WorkflowBuilderFactory : IWorkflowBuilderFactory
{
    private readonly Func<IWorkflowBuilder> _factory;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowBuilderFactory"/> class.
    /// </summary>
    public WorkflowBuilderFactory(Func<IWorkflowBuilder> factory) => _factory = factory;

    /// <inheritdoc />
    public IWorkflowBuilder CreateBuilder() => _factory();
}