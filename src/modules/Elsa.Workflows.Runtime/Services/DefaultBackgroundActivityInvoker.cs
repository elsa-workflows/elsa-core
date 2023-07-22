using Elsa.Common.Models;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Middleware.Activities;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Requests;
using Microsoft.Extensions.Logging;

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
    private readonly IVariablePersistenceManager _variablePersistenceManager;
    private readonly IActivityInvoker _activityInvoker;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultBackgroundActivityInvoker"/> class.
    /// </summary>
    public DefaultBackgroundActivityInvoker(
        IWorkflowRuntime workflowRuntime,
        IWorkflowDispatcher workflowDispatcher,
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowExecutionContextFactory workflowExecutionContextFactory,
        IVariablePersistenceManager variablePersistenceManager,
        IActivityInvoker activityInvoker,
        IServiceProvider serviceProvider,
        ILogger<DefaultBackgroundActivityInvoker> logger)
    {
        _workflowRuntime = workflowRuntime;
        _workflowDispatcher = workflowDispatcher;
        _workflowDefinitionService = workflowDefinitionService;
        _workflowExecutionContextFactory = workflowExecutionContextFactory;
        _variablePersistenceManager = variablePersistenceManager;
        _activityInvoker = activityInvoker;
        _serviceProvider = serviceProvider;
        _logger = logger;
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
        var activityNodeId = scheduledBackgroundActivity.ActivityNodeId;
        var activityExecutionContext = workflowExecutionContext.ActiveActivityExecutionContexts.First(x => x.NodeId == activityNodeId);

        // Load persistent variables for the activity to use.
        await _variablePersistenceManager.LoadVariablesAsync(workflowExecutionContext);

        // Mark the activity as being invoked from a background worker.
        activityExecutionContext.TransientProperties[BackgroundActivityInvokerMiddleware.IsBackgroundExecution] = true;

        // Invoke the activity.
        await _activityInvoker.InvokeAsync(activityExecutionContext);

        // Capture any activity output produced by the activity (but only if the associated memory block is stored in the workflow itself).
        var outputDescriptors = activityExecutionContext.ActivityDescriptor.Outputs;
        var outputValues = new Dictionary<string, object>();

        foreach (var outputDescriptor in outputDescriptors)
        {
            var output = (Output?)outputDescriptor.ValueGetter(activityExecutionContext.Activity);
            
            if(output == null)
                continue;

            var memoryBlockReference = output.MemoryBlockReference();
            
            if(!activityExecutionContext.ExpressionExecutionContext.TryGetBlock(memoryBlockReference, out var memoryBlock))
                continue;
            
            var variableMetadata = memoryBlock.Metadata as VariableBlockMetadata;
            var driver = variableMetadata?.StorageDriverType;
            
            // We only capture output written to the workflow itself. Other drivers like blob storage, etc. will be ignored since the foreground context will be loading those.
            if(driver != typeof(WorkflowStorageDriver))
                continue;
            
            var outputValue = activityExecutionContext.Get(memoryBlockReference);

            if (outputValue != null)
                outputValues[outputDescriptor.Name] = outputValue;
        }

        // Persist any variables that were written to by the activity.
        await _variablePersistenceManager.SaveVariablesAsync(workflowExecutionContext);
        
        // Resume the workflow, passing along the activity output.
        // TODO: This approach will fail if the output is non-serializable. We need to find a way to pass the output to the workflow without serializing it.
        var bookmarkId = scheduledBackgroundActivity.BookmarkId;
        var inputKey = BackgroundActivityInvokerMiddleware.GetBackgroundActivityOutputKey(activityNodeId);

        var dispatchRequest = new DispatchWorkflowInstanceRequest
        {
            InstanceId = workflowInstanceId,
            BookmarkId = bookmarkId,
            Input = new Dictionary<string, object>
            {
                [inputKey] = outputValues
            }
        };

        if(cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Background execution for activity {ActivityNodeId} was canceled", activityNodeId);
            return;
        }
        
        await _workflowDispatcher.DispatchAsync(dispatchRequest, cancellationToken);
    }
}