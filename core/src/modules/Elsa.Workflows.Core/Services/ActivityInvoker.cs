using Elsa.Workflows.Options;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.Telemetry;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows;

/// <inheritdoc />
public class ActivityInvoker(
    IActivityExecutionPipeline pipeline,
    ILoggerStateGenerator<ActivityExecutionContext> loggerStateGenerator,
    ILogger<ActivityInvoker> logger)
    : IActivityInvoker
{

    /// <inheritdoc />
    public async Task<ActivityExecutionContext> InvokeAsync(WorkflowExecutionContext workflowExecutionContext, IActivity activity, ActivityInvocationOptions? options = null)
    {
        await DemandUnownedAsync(workflowExecutionContext);
        return await InvokeCoreAsync(workflowExecutionContext, activity, options, false);
    }

    internal async Task<ActivityExecutionContext> InvokeAuthorizedAsync(WorkflowExecutionContext context, IActivity activity, ActivityInvocationOptions options)
    {
        if (!WorkflowExecutionPhase.Contains(context) || pipeline.GetType() != typeof(ActivityExecutionPipeline))
        {
            throw new InvalidOperationException("Activity execution requires the internal authorized scheduler lane.");
        }
        return await InvokeCoreAsync(context, activity, options, true);
    }

    private async Task<ActivityExecutionContext> InvokeCoreAsync(WorkflowExecutionContext workflowExecutionContext, IActivity activity,
        ActivityInvocationOptions? options, bool authorized)
    {
        // Setup an activity execution context, potentially reusing an existing one if requested.
        var existingActivityExecutionContext = options?.ExistingActivityExecutionContext;

        // Perform a lookup to make sure the activity execution context is part of the workflow execution context.
        var activityExecutionContext = existingActivityExecutionContext != null
            ? workflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.Id == existingActivityExecutionContext.Id)
            : null;

        if (activityExecutionContext == null)
        {
            // Create a new activity execution context.
            activityExecutionContext = await workflowExecutionContext.CreateActivityExecutionContextAsync(activity, options);
            activityExecutionContext.Taint();

            // Add the activity context to the workflow context.
            workflowExecutionContext.AddActivityExecutionContext(activityExecutionContext);
        }

        // Execute the activity execution pipeline.
        await InvokeCoreAsync(activityExecutionContext, authorized);
        
        return activityExecutionContext;
    }

    /// <inheritdoc />
    public async Task InvokeAsync(ActivityExecutionContext activityExecutionContext)
    {
        await DemandUnownedAsync(activityExecutionContext.WorkflowExecutionContext);
        await InvokeCoreAsync(activityExecutionContext, false);
    }

    private async ValueTask DemandUnownedAsync(WorkflowExecutionContext context)
    {
        if (pipeline is ActivityExecutionPipeline builtIn)
        {
            await builtIn.DemandPublicInvocationAsync(context);
        }
        else if (context.GetService<IWorkflowExecutionGuard>() is { } guard)
        {
            await guard.AuthorizeAsync(context, WorkflowExecutionEntryPoint.DirectPipeline);
        }
    }

    private async Task InvokeCoreAsync(ActivityExecutionContext activityExecutionContext, bool authorized)
    {
        var telemetryScope = WorkflowInstrumentation.StartActivity(activityExecutionContext);
        Exception? exception = null;

        try
        {
            var loggerState = loggerStateGenerator.GenerateLoggerState(activityExecutionContext);
            using var loggingScope = logger.BeginScope(loggerState);

            // Execute the activity execution pipeline.
            if (authorized)
            {
                await ((ActivityExecutionPipeline)pipeline).ExecuteAuthorizedAsync(activityExecutionContext);
            }
            else
            {
                await pipeline.ExecuteAsync(activityExecutionContext);
            }
        }
        catch (Exception e)
        {
            exception = e;
            throw;
        }
        finally
        {
            WorkflowInstrumentation.StopActivity(telemetryScope, activityExecutionContext, exception);
        }
    }
}
