using System.Threading;
using System.Threading.Tasks;
using Elsa.Management.Services;
using Elsa.Persistence.Models;
using Elsa.Runtime.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Api.Endpoints.Workflows;

public static partial class Workflows
{
    public static async Task<IResult> DeleteAsync(string definitionId, IWorkflowRegistry workflowRegistry,
        IWorkflowPublisher workflowPublisher, CancellationToken cancellationToken)
    {
        var workflow = await workflowRegistry.FindByIdAsync(definitionId, VersionOptions.LatestOrPublished, cancellationToken);
        if (workflow == null)
        {
            return Results.NotFound();
        }

        await workflowPublisher.DeleteAsync(workflow, cancellationToken);
        return Results.NoContent();
    }
}