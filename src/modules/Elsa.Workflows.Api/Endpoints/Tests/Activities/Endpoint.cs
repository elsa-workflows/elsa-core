using Elsa.Abstractions;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Api.Endpoints.Tests.Activities;

/// <summary>
/// This abstract class provides the necessary infrastructure to handle the execution of workflows, including setup of routes, permissions,
/// and processing of HTTP requests to execute workflows.
/// </summary>
internal class Endpoint(
    IWorkflowDefinitionService workflowDefinitionService,
    IActivityTestRunner activityTestRunner,
    IActivityExecutionMapper activityExecutionMapper,
    IIdentityGenerator identityGenerator,
    IServiceProvider serviceProvider)
    : ElsaEndpoint<Request>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/tests/activities");
        ConfigurePermissions("exec:tests");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(request.WorkflowDefinitionHandle, cancellationToken);

        if (workflowGraph == null)
        {
            AddError("Workflow definition not found.");
            await SendErrorsAsync(cancellation: cancellationToken);
            return;
        }
        
        var activity = workflowGraph.FindActivity(request.ActivityHandle);

        if (activity == null)
        {
            AddError("Activity not found.");
            await SendErrorsAsync(cancellation: cancellationToken);
            return;
        }

        var activityExecutionContext = await activityTestRunner.RunAsync(workflowGraph, activity, cancellationToken);
        var record = activityExecutionMapper.Map(activityExecutionContext);
        var outcomes = record.Payload != null && record.Payload.TryGetValue("Outcomes", out var outcomesObj) ? outcomesObj as ICollection<string> : null;
        var response = new Response
        {
            Outputs = record.Outputs,
            Outcomes = outcomes,
            Exception = record.Exception,
            Status = record.Status
        };
        
        await SendOkAsync(response, cancellationToken);
    }
}

public class Request
{
    public WorkflowDefinitionHandle WorkflowDefinitionHandle { get; set; } = null!;
    public ActivityHandle ActivityHandle { get; set; } = null!;
}

public class Response
{
    public IDictionary<string, object?>? Outputs { get; set; }
    public ICollection<string>? Outcomes { get; set; }
    public ExceptionState? Exception { get; set; }
    public ActivityStatus Status { get; set; }
}