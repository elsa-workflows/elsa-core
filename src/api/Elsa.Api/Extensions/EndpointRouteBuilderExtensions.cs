using Elsa.Api.Endpoints.ActivityDescriptors;
using Elsa.Api.Endpoints.Events;
using Elsa.Api.Endpoints.WorkflowDefinitions;
using Elsa.Api.Endpoints.WorkflowInstances;
using Microsoft.AspNetCore.Routing;

namespace Elsa.Api.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapElsaApiEndpoints(this IEndpointRouteBuilder endpoints) => endpoints
        .MapWorkflows()
        .MapWorkflowInstances()
        .MapActivityDescriptors()
        .MapEvents();
}