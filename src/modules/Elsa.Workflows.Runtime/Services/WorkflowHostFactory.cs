using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class WorkflowHostFactory : IWorkflowHostFactory
{
    private readonly IIdentityGenerator _identityGenerator;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowHostFactory"/> class.
    /// </summary>
    public WorkflowHostFactory(IIdentityGenerator identityGenerator, IServiceProvider serviceProvider)
    {
        _identityGenerator = identityGenerator;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public Task<IWorkflowHost> CreateAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        return CreateAsync(workflow, default(string), cancellationToken);
    }

    /// <inheritdoc />
    public Task<IWorkflowHost> CreateAsync(Workflow workflow, string? instanceId, CancellationToken cancellationToken = default)
    {
        var workflowState = new WorkflowState
        {
            Id = instanceId ?? _identityGenerator.GenerateId(),
            DefinitionId = workflow.Identity.DefinitionId,
            DefinitionVersion = workflow.Identity.Version
        };

        return CreateAsync(workflow, workflowState, cancellationToken);
    }
    
    /// <inheritdoc />
    public Task<IWorkflowHost> CreateAsync(Workflow workflow, WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var workflowHost = (IWorkflowHost)ActivatorUtilities.CreateInstance<WorkflowHost>(_serviceProvider, workflow, workflowState);
        return Task.FromResult(workflowHost);
    }
}