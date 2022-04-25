using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Services;
using Elsa.Persistence.Requests;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;

namespace Elsa.Api.Endpoints.WorkflowInstances;

public static partial class WorkflowInstances
{
    public static async Task<IResult> GetAsync(IRequestSender requestSender, WorkflowSerializerOptionsProvider serializerOptionsProvider, string id, CancellationToken cancellationToken)
    {
        var serializerOptions = serializerOptionsProvider.CreateApiOptions();
        var request = new FindWorkflowInstance(id);
        var workflowInstance = await requestSender.RequestAsync(request, cancellationToken);
        return workflowInstance != null ? Results.Json(workflowInstance, serializerOptions, statusCode: StatusCodes.Status200OK) : Results.NotFound();
    }
}