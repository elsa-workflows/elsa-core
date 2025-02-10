using Elsa.Workflows.Options;
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
    public async Task InvokeAsync(WorkflowExecutionContext workflowExecutionContext, IActivity activity, ActivityInvocationOptions? options = null)
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
        await InvokeAsync(activityExecutionContext);
    }

    /// <inheritdoc />
    public async Task InvokeAsync(ActivityExecutionContext activityExecutionContext)
    {
        var loggerState = loggerStateGenerator.GenerateLoggerState(activityExecutionContext);
        using var loggingScope = logger.BeginScope(loggerState);

        // Execute the activity execution pipeline.
        await pipeline.ExecuteAsync(activityExecutionContext);
    }
}