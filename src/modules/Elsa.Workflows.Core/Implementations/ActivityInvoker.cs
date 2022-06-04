using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Implementations;

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
        IEnumerable<MemoryDatumReference>? locationReferences = default)
    {
        var cancellationToken = workflowExecutionContext.CancellationToken;

        // Get a handle to the parent execution context.
        var parentActivityExecutionContext = owner;
        var parentExpressionExecutionContext = parentActivityExecutionContext?.ExpressionExecutionContext;

        // Setup an activity execution context.
        var workflowMemory = workflowExecutionContext.MemoryRegister;
        var workflow = workflowExecutionContext.Workflow;
        var transientProperties = workflowExecutionContext.TransientProperties;
        var input = workflowExecutionContext.Input;
        var applicationProperties = ExpressionExecutionContextExtensions.CreateApplicationPropertiesFrom(workflow, transientProperties, input);
        var activityMemory = new MemoryRegister(workflowMemory);
        var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, activityMemory, parentExpressionExecutionContext, applicationProperties, cancellationToken);
        var activityExecutionContext = new ActivityExecutionContext(workflowExecutionContext, parentActivityExecutionContext, expressionExecutionContext, activity, cancellationToken);

        // Declare locations.
        if (locationReferences != null)
            activityMemory.Declare(locationReferences);

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