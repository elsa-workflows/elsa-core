using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;

/// <summary>
/// An API endpoint that executes a given workflow definition through POST method.
/// </summary>
[PublicAPI]
internal class PostEndpoint(
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowRuntime workflowRuntime,
    IWorkflowStarter workflowStarter,
    IApiSerializer apiSerializer)
    : EndpointBase<PostRequest>(workflowDefinitionService, workflowRuntime, workflowStarter, apiSerializer)
{
    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Verbs(FastEndpoints.Http.POST);
    }
}