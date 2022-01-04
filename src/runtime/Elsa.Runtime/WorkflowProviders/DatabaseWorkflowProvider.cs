using System.Runtime.CompilerServices;
using Elsa.Mediator.Contracts;
using Elsa.Models;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;
using Elsa.Runtime.Contracts;

namespace Elsa.Runtime.WorkflowProviders;

/// <summary>
/// Provides workflows to the system that are stored in the database.
/// </summary>
public class DatabaseWorkflowProvider : IWorkflowProvider
{
    private readonly IRequestSender _requestSender;

    public DatabaseWorkflowProvider(IRequestSender requestSender) => _requestSender = requestSender;

    public async ValueTask<Workflow?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default) =>
        await _requestSender.RequestAsync(new FindWorkflow(definitionId, versionOptions), cancellationToken);

    public async IAsyncEnumerable<Workflow> StreamAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Instead of loading ALL workflows in the system into memory at once, it's better to load them in batches of a small number and yield them to the caller.
        var skip = 0;
        const int take = 250;

        while (true)
        {
            var workflows = (await _requestSender.RequestAsync(new ListWorkflows(VersionOptions.Published, skip, take), cancellationToken)).ToList();

            if (!workflows.Any())
                yield break;

            foreach (var workflow in workflows)
                yield return workflow;

            skip += workflows.Count;
        }
    }
}