using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Services;

public class WorkflowHostFactory : IWorkflowHostFactory
{
    private readonly IServiceProvider _serviceProvider;

    public WorkflowHostFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public Task<IWorkflowHost> CreateAsync(Workflow workflow, WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var workflowHost = (IWorkflowHost)ActivatorUtilities.CreateInstance<WorkflowHost>(_serviceProvider, workflow, workflowState);
        return Task.FromResult(workflowHost);
    }

    public Task<IWorkflowHost> CreateAsync(Workflow workflow, CancellationToken cancellationToken = default)
    {
        var workflowState = new WorkflowState
        {
            DefinitionId = workflow.Identity.DefinitionId,
            DefinitionVersion = workflow.Identity.Version
        };

        return CreateAsync(workflow, workflowState, cancellationToken);
    }
}