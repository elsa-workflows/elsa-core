using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;

/// <summary>
/// An API endpoint that executes a given workflow definition through GET method.
/// </summary>
[PublicAPI]
internal class GetEndpoint(
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowRuntime workflowRuntime,
    IWorkflowStarter workflowStarter,
    IApiSerializer apiSerializer) 
    : EndpointBase<GetRequest>(workflowDefinitionService, workflowRuntime, workflowStarter, apiSerializer)
{
    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Verbs(FastEndpoints.Http.GET);
    }
}