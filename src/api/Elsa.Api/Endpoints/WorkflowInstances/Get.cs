using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Services;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;

namespace Elsa.Api.Endpoints.WorkflowInstances;

public static partial class WorkflowInstances
{
    public static async Task<IResult> GetAsync(IWorkflowInstanceStore workflowInstanceStore, WorkflowSerializerOptionsProvider serializerOptionsProvider, string id, CancellationToken cancellationToken)
    {
        var serializerOptions = serializerOptionsProvider.CreateApiOptions();
        var workflowInstance = await workflowInstanceStore.FindByIdAsync(id, cancellationToken);
        return workflowInstance != null ? Results.Json(workflowInstance, serializerOptions, statusCode: StatusCodes.Status200OK) : Results.NotFound();
    }
}