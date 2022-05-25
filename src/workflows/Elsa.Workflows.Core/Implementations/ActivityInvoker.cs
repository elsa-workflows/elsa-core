using Elsa.Expressions.Models;
using Elsa.Models;
using Elsa.Services;

namespace Elsa.Implementations;

public class ActivityInvoker : IActivityInvoker
{
    private readonly IActivityExecutionPipeline _pipeline;
    private readonly IServiceProvider _serviceProvider;

    public ActivityInvoker(IActivityExecutionPipeline pipeline, IServiceProvider serviceProvider)
    {
        _pipeline = pipeline;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(
        WorkflowExecutionContext workflowExecutionContext,
        IActivity activity,
        ActivityExecutionContext? owner,
        IEnumerable<RegisterLocationReference>? locationReferences = default)
    {
        var cancellationToken = workflowExecutionContext.CancellationToken;

        // Get a handle to the parent execution context.
        var parentActivityExecutionContext = owner;

        // Setup an activity execution context.
        var register = workflowExecutionContext.Register;
        var workflow = workflowExecutionContext.Workflow;
        var transientProperties = workflowExecutionContext.TransientProperties;
        var input = workflowExecutionContext.Input;
        var applicationProperties = ExpressionExecutionContextExtensions.CreateApplicationPropertiesFrom(workflow, transientProperties, input);
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, register, parentActivityExecutionContext?.ExpressionExecutionContext, applicationProperties, cancellationToken);
        var activityExecutionContext = new ActivityExecutionContext(workflowExecutionContext, parentActivityExecutionContext, expressionExecutionContext, activity, cancellationToken);

        // Declare locations.
        if (locationReferences != null)
            register.Declare(locationReferences);

        // Push the activity context into the workflow context.
        workflowExecutionContext.ActivityExecutionContexts.Add(activityExecutionContext);

        // Execute the activity execution pipeline.
        await InvokeAsync(activityExecutionContext);
    }

    public async Task InvokeAsync(ActivityExecutionContext activityExecutionContext)
    {
        // Execute the activity execution pipeline.
        await _pipeline.ExecuteAsync(activityExecutionContext);
    }
}