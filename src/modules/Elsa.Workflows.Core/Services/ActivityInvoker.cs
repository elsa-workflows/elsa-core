using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Options;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class ActivityInvoker : IActivityInvoker
{
    private readonly IActivityExecutionPipeline _pipeline;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ActivityInvoker(IActivityExecutionPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    /// <inheritdoc />
    public async Task InvokeAsync(WorkflowExecutionContext workflowExecutionContext, IActivity activity, ActivityInvocationOptions? options = default)
    {
        // Setup an activity execution context.
        var activityExecutionContext = workflowExecutionContext.CreateActivityExecutionContext(activity, options);

        // Add the activity context to the workflow context.
        workflowExecutionContext.AddActivityExecutionContext(activityExecutionContext);

        // Execute the activity execution pipeline.
        await InvokeAsync(activityExecutionContext);
    }

    /// <inheritdoc />
    public async Task InvokeAsync(ActivityExecutionContext activityExecutionContext)
    {
        // Execute the activity execution pipeline.
        await _pipeline.ExecuteAsync(activityExecutionContext);
    }
}