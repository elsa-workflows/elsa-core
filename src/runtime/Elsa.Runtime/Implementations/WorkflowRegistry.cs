using System.Reflection;
using System.Runtime.CompilerServices;
using Elsa.Models;
using Elsa.Persistence.Models;
using Elsa.Runtime.Attributes;
using Elsa.Runtime.Services;

namespace Elsa.Runtime.Implementations;

public class WorkflowRegistry : IWorkflowRegistry
{
    public static Func<IWorkflowProvider, bool> SkipDynamicProviders => x => !x.GetType().GetCustomAttributes<SkipTriggerIndexingAttribute>().Any();
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

    public async Task<Workflow?> FindByNameAsync(string name, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        foreach (var workflowProvider in _workflowProviders)
        {
            var workflow = await workflowProvider.FindByNameAsync(name, versionOptions, cancellationToken);

            if (workflow != null)
                return workflow;
        }

        return default!;
    }

    public IAsyncEnumerable<Workflow> StreamAllAsync(CancellationToken cancellationToken = default) => StreamAllAsync(_workflowProviders, cancellationToken);

    public IAsyncEnumerable<Workflow> StreamAllAsync(Func<IWorkflowProvider, bool> includeProvider, CancellationToken cancellationToken = default)
    {
        var providers = _workflowProviders.Where(includeProvider);
        return StreamAllAsync(providers, cancellationToken);
    }

    public async IAsyncEnumerable<Workflow> StreamAllAsync(IEnumerable<IWorkflowProvider> providers, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var workflowProvider in providers)
        {
            var workflows = workflowProvider.StreamAllAsync(cancellationToken);

            await foreach (var workflow in workflows.WithCancellation(cancellationToken))
                yield return workflow;
        }
    }
}