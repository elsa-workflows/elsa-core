using System.Runtime.CompilerServices;
using Elsa.Mediator.Services;
using Elsa.Models;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;
using Elsa.Runtime.Attributes;
using Elsa.Runtime.Services;

namespace Elsa.Runtime.WorkflowProviders;

/// <summary>
/// Provides workflows to the system that are stored in the database.
/// </summary>
[SkipTriggerIndexing]
public class DatabaseWorkflowProvider : IWorkflowProvider
{
    private readonly IRequestSender _requestSender;

    public DatabaseWorkflowProvider(IRequestSender requestSender) => _requestSender = requestSender;

    public async ValueTask<Workflow?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default) =>
        await _requestSender.RequestAsync(new FindWorkflowByDefinitionId(definitionId, versionOptions), cancellationToken);

    public async ValueTask<Workflow?> FindByNameAsync(string name, VersionOptions versionOptions, CancellationToken cancellationToken = default) =>
        await _requestSender.RequestAsync(new FindWorkflowByName(name, versionOptions), cancellationToken);

    public async IAsyncEnumerable<Workflow> StreamAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Instead of loading ALL workflows in the system into memory at once, it's better to load them in batches of a small number and yield them to the caller.
        var skip = 0;
        const int take = 250;

        while (true)
        {
            var workflows = (await _requestSender.RequestAsync(new ListWorkflowDefinitions(VersionOptions.Published, skip, take), cancellationToken)).ToList();

            if (!workflows.Any())
                yield break;

            foreach (var workflow in workflows)
                yield return workflow;

            skip += workflows.Count;
        }
    }
}