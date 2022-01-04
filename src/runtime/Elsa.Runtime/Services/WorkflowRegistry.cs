using System.Runtime.CompilerServices;
using Elsa.Models;
using Elsa.Persistence.Models;
using Elsa.Runtime.Contracts;

namespace Elsa.Runtime.Services;

public class WorkflowRegistry : IWorkflowRegistry
{
    private readonly IEnumerable<IWorkflowProvider> _workflowProviders;

    public WorkflowRegistry(IEnumerable<IWorkflowProvider> workflowProviders) => _workflowProviders = workflowProviders;

    public async Task<Workflow?> FindByIdAsync(string id, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        foreach (var workflowProvider in _workflowProviders)
        {
            var workflow = await workflowProvider.FindByDefinitionIdAsync(id, versionOptions, cancellationToken);

            if (workflow != null)
                return workflow;
        }

        return default!;
    }

    public async IAsyncEnumerable<Workflow> StreamAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var workflowProvider in _workflowProviders)
        {
            var workflows = workflowProvider.StreamAllAsync(cancellationToken);

            await foreach (var workflow in workflows.WithCancellation(cancellationToken))
                yield return workflow;
        }
    }
}