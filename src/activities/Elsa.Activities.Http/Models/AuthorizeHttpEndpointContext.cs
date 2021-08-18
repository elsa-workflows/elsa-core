using System.Threading;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Models
{
    public record AuthorizeHttpEndpointContext(HttpContext HttpContext, IActivityBlueprintWrapper<HttpEndpoint> HttpEndpointActivity, IWorkflowBlueprint WorkflowBlueprint, string WorkflowInstanceId, CancellationToken CancellationToken);
}