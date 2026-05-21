using Elsa.Abstractions;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Alterations.Endpoints.Alterations.DryRun;

/// <summary>
/// Determines which workflow instances a "Submit" request would target without actually running an alteration.
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
        if (!ValidateInput(filter))
        {
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
            return;
        }

        var workflowInstanceIds = await workflowInstanceFinder.FindAsync(filter, cancellationToken);
        var response = new Response(workflowInstanceIds.ToList());
        await Send.OkAsync(response, cancellationToken);
    }

    private bool ValidateInput(AlterationWorkflowInstanceFilter filter)
    {
        return TimestampFilterValidation.Validate(filter.TimestampFilters, error => AddError(error));
    }
}
