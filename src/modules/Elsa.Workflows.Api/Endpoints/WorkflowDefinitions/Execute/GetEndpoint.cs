using Elsa.Abstractions;
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
    : ElsaEndpoint<GetRequest>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Routes("/workflow-definitions/{definitionId}/execute");
        ConfigurePermissions("exec:workflow-definitions");
        Verbs(FastEndpoints.Http.GET);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(GetRequest request, CancellationToken cancellationToken)
    {
        await WorkflowExecutionHelper.ExecuteWorkflowAsync(
            request,
            workflowDefinitionService,
            workflowRuntime,
            workflowStarter,
            apiSerializer,
            HttpContext,
            cancellationToken);
    }
}