using Elsa.Abstractions;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Models;
using JetBrains.Annotations;

namespace Elsa.Alterations.Endpoints.Alterations.DryRun;

/// <summary>
/// Executes an alteration plan.
/// </summary>
[PublicAPI]
public class DryRun(IWorkflowInstanceFinder workflowInstanceFinder) : ElsaEndpoint<AlterationWorkflowInstanceFilter, Response>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/alterations/dry-run");
        ConfigurePermissions("run:alterations");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(AlterationWorkflowInstanceFilter filter, CancellationToken cancellationToken)
    {
        var workflowInstanceIds = await workflowInstanceFinder.FindAsync(filter, cancellationToken);

        // Write response.
        var response = new Response(workflowInstanceIds.ToList());
        await SendOkAsync(response, cancellationToken);
    }
}