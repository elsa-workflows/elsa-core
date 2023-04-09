using Elsa.Common.Models;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Middleware.Activity;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Models.Requests;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// An activity invoker that invokes activities detached from the workflow. This is useful for invoking activities from a background worker.
/// </summary>
public class DefaultBackgroundActivityInvoker : IBackgroundActivityInvoker
{
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IWorkflowDispatcher _workflowDispatcher;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowExecutionContextFactory _workflowExecutionContextFactory;
    private readonly IWorkflowStateSerializer _workflowStateSerializer;
    private readonly IVariablePersistenceManager _variablePersistenceManager;
    private readonly IActivityInvoker _activityInvoker;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultBackgroundActivityInvoker"/> class.
    /// </summary>
    public DefaultBackgroundActivityInvoker(
        IWorkflowRuntime workflowRuntime,
        IWorkflowDispatcher workflowDispatcher,
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowExecutionContextFactory workflowExecutionContextFactory,
        IWorkflowStateSerializer workflowStateSerializer,
        IVariablePersistenceManager variablePersistenceManager,
        IActivityInvoker activityInvoker,
        IServiceProvider serviceProvider)
    {
        _workflowRuntime = workflowRuntime;
        _workflowDispatcher = workflowDispatcher;
        _workflowDefinitionService = workflowDefinitionService;
        _workflowExecutionContextFactory = workflowExecutionContextFactory;
        _workflowStateSerializer = workflowStateSerializer;
        _variablePersistenceManager = variablePersistenceManager;
        _activityInvoker = activityInvoker;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken = default)
    {
        var workflowInstanceId = scheduledBackgroundActivity.WorkflowInstanceId;
        var workflowState = await _workflowRuntime.ExportWorkflowStateAsync(workflowInstanceId, cancellationToken);

        if (workflowState == null)
            throw new Exception("Workflow state not found");

        var workflowDefinition = await _workflowDefinitionService.FindAsync(workflowState.DefinitionId, VersionOptions.SpecificVersion(workflowState.DefinitionVersion), cancellationToken);

        if (workflowDefinition == null)
            throw new Exception("Workflow definition not found");

        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        var workflowExecutionContext = await _workflowExecutionContextFactory.CreateAsync(_serviceProvider, workflow, workflowState.Id, workflowState, cancellationToken: cancellationToken);
        var activityId = scheduledBackgroundActivity.ActivityId;
        var activityExecutionContext = workflowExecutionContext.ActivityExecutionContexts.First(x => x.Activity.Id == activityId);

        // Load persistent variables for the activity to use.
        await _variablePersistenceManager.LoadVariablesAsync(workflowExecutionContext);

        // Mark the activity as being invoked from a background worker.
        activityExecutionContext.TransientProperties[BackgroundActivityInvokerMiddleware.IsBackgroundExecution] = true;

        // Invoke the activity.
        await _activityInvoker.InvokeAsync(activityExecutionContext);

        // Capture any activity output produced by the activity.
        var outputDescriptors = activityExecutionContext.ActivityDescriptor.Outputs;
        var outputValues = new Dictionary<string, object>();

        foreach (var outputDescriptor in outputDescriptors)
        {
            var output = (Output?)outputDescriptor.ValueGetter(activityExecutionContext.Activity);
            var outputValue = output != null ? activityExecutionContext.Get(output.MemoryBlockReference()) : default!;

            if (outputValue != null)
                outputValues[outputDescriptor.Name] = outputValue;
        }

        // Resume the workflow, passing along the activity output.
        // TODO: This approach will fail if the output is non-serializable. We need to find a way to pass the output to the workflow without serializing it.
        var bookmarkId = scheduledBackgroundActivity.BookmarkId;
        var inputKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityOutputKey(activityId);

        var dispatchRequest = new DispatchWorkflowInstanceRequest
        {
            InstanceId = workflowInstanceId,
            BookmarkId = bookmarkId,
            Input = new Dictionary<string, object>
            {
                [inputKey] = outputValues
            }
        };

        await _workflowDispatcher.DispatchAsync(dispatchRequest, cancellationToken);
    }
}