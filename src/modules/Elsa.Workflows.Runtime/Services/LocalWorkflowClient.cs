using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Activities;
using Elsa.Workflows.ActivationValidators;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.Exceptions;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents a client for executing and managing local workflows.
/// </summary>
public class LocalWorkflowClient(
    string workflowInstanceId,
    IWorkflowInstanceManager workflowInstanceManager,
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowRunner workflowRunner,
    IWorkflowCanceler workflowCanceler,
    IWorkflowActivationGate workflowActivationGate,
    WorkflowStateMapper workflowStateMapper,
    ILogger<LocalWorkflowClient> logger) : IWorkflowClient
{
    /// <summary>
    /// Retained for source compatibility. Hosts should prefer the constructor resolved by DI,
    /// which supplies the configured activation gate.
    /// </summary>
    public LocalWorkflowClient(
        string workflowInstanceId,
        IWorkflowInstanceManager workflowInstanceManager,
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowRunner workflowRunner,
        IWorkflowCanceler workflowCanceler,
        WorkflowStateMapper workflowStateMapper,
        ILogger<LocalWorkflowClient> logger)
        : this(workflowInstanceId, workflowInstanceManager, workflowDefinitionService, workflowRunner, workflowCanceler, new CompatibilityActivationGate(), workflowStateMapper, logger)
    {
    }

    /// <inheritdoc />
    public string WorkflowInstanceId => workflowInstanceId;

    /// <inheritdoc />
    public async Task<CreateWorkflowInstanceResponse> CreateInstanceAsync(CreateWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var workflowDefinitionHandle = request.WorkflowDefinitionHandle;
        var workflowGraph = await GetWorkflowGraphAsync(workflowDefinitionHandle, cancellationToken);

        await using var lease = await workflowActivationGate.EvaluateAsync(workflowGraph.Workflow, request.CorrelationId, cancellationToken);
        if (!lease.CanStart)
        {
            LogActivationDenial(workflowGraph.Workflow, request.CorrelationId);
            return new()
            {
                CannotStart = true
            };
        }

        var effectiveCancellationToken = lease.GetEffectiveCancellationToken(cancellationToken);
        effectiveCancellationToken.ThrowIfCancellationRequested();
        var workflowInstance = CreateWorkflowInstance(workflowGraph.Workflow, request);
        effectiveCancellationToken.ThrowIfCancellationRequested();
        await workflowInstanceManager.SaveAsync(workflowInstance, effectiveCancellationToken);
        return new();
    }

    /// <inheritdoc />
    public async Task<RunWorkflowInstanceResponse> RunInstanceAsync(RunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var workflowInstance = await GetWorkflowInstanceAsync(cancellationToken);
        return await RunInstanceAsync(workflowInstance, request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowInstanceResponse> CreateAndRunInstanceAsync(CreateAndRunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var workflowDefinitionHandle = request.WorkflowDefinitionHandle;
        var workflowGraph = await GetWorkflowGraphAsync(workflowDefinitionHandle, cancellationToken);

        await using var lease = await workflowActivationGate.EvaluateAsync(workflowGraph.Workflow, request.CorrelationId, cancellationToken);
        if (!lease.CanStart)
        {
            LogActivationDenial(workflowGraph.Workflow, request.CorrelationId);
            return new()
            {
                CannotStart = true
            };
        }

        var effectiveCancellationToken = lease.GetEffectiveCancellationToken(cancellationToken);
        effectiveCancellationToken.ThrowIfCancellationRequested();
        var workflowInstance = CreateWorkflowInstance(workflowGraph.Workflow, new CreateWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = workflowDefinitionHandle,
            CorrelationId = request.CorrelationId,
            Name = request.Name,
            ParentId = request.ParentId,
            Input = request.Input,
            Properties = request.Properties
        });
        effectiveCancellationToken.ThrowIfCancellationRequested();

        // Do not durably publish a Running/Pending row before execution. If the run is
        // interrupted before WorkflowRunner commits its result, that row would occupy the
        // activation scope despite the failed activation.
        return await RunInstanceAsync(workflowInstance, new()
        {
            Input = request.Input,
            Variables = request.Variables,
            Properties = request.Properties,
            TriggerActivityId = request.TriggerActivityId,
            ActivityHandle = request.ActivityHandle,
            SchedulingActivityExecutionId = request.SchedulingActivityExecutionId,
            SchedulingWorkflowInstanceId = request.SchedulingWorkflowInstanceId,
            SchedulingCallStackDepth = request.SchedulingCallStackDepth,
            IncludeWorkflowOutput = request.IncludeWorkflowOutput
        }, effectiveCancellationToken);
    }

    private void LogActivationDenial(Workflow workflow, string? correlationId)
    {
        var identity = workflow.Identity;
        logger.LogWarning(
            "Workflow activation strategy {ActivationStrategyType} disallowed creating an instance for definition {WorkflowDefinitionId} version {WorkflowDefinitionVersion} ({WorkflowDefinitionVersionId}); correlation ID present: {CorrelationIdPresent}",
            workflow.Options.ActivationStrategyType?.FullName,
            identity.DefinitionId,
            identity.Version,
            identity.Id,
            !string.IsNullOrWhiteSpace(correlationId));
    }

    /// <inheritdoc />
    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        var workflowInstance = await GetWorkflowInstanceAsync(cancellationToken);
        await CancelAsync(workflowInstance, cancellationToken);
    }

    private async Task CancelAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
    {
        if (workflowInstance.Status != WorkflowStatus.Running) return;
        var workflowGraph = await GetWorkflowGraphAsync(workflowInstance, cancellationToken);
        var workflowState = await workflowCanceler.CancelWorkflowAsync(workflowGraph, workflowInstance.WorkflowState, cancellationToken);
        await workflowInstanceManager.SaveAsync(workflowState, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowState> ExportStateAsync(CancellationToken cancellationToken = default)
    {
        var workflowInstance = await GetWorkflowInstanceAsync(cancellationToken);
        return workflowInstance.WorkflowState;
    }

    /// <inheritdoc />
    public async Task ImportStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var workflowInstance = workflowStateMapper.Map(workflowState)!;
        await workflowInstanceManager.SaveAsync(workflowInstance, cancellationToken);
    }

    public Task<bool> InstanceExistsAsync(CancellationToken cancellationToken = default)
    {
        return workflowInstanceManager.ExistsAsync(workflowInstanceId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(CancellationToken cancellationToken = default)
    {
        // Load the workflow instance (single DB call)
        var workflowInstance = await TryGetWorkflowInstanceAsync(cancellationToken);
        if (workflowInstance == null)
            return false;

        await CancelAsync(workflowInstance, cancellationToken);
        
        // Delete the workflow instance
        var filter = new WorkflowInstanceFilter { Id = workflowInstanceId };
        await workflowInstanceManager.DeleteAsync(filter, cancellationToken);
        return true;
    }

    public async Task<RunWorkflowInstanceResponse> RunInstanceAsync(WorkflowInstance workflowInstance, RunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workflowState = workflowInstance.WorkflowState;

        if (workflowInstance.Status != WorkflowStatus.Running)
        {
            logger.LogWarning("Attempt to resume workflow {WorkflowInstanceId} that is not in the Running state. The actual state is {ActualWorkflowStatus}", workflowState.Id, workflowState.Status);
            return new()
            {
                WorkflowInstanceId = WorkflowInstanceId,
                Status = workflowInstance.Status,
                SubStatus = workflowInstance.SubStatus
            };
        }

        var runWorkflowOptions = new RunWorkflowOptions
        {
            Input = request.Input,
            Variables = request.Variables,
            Properties = request.Properties,
            BookmarkId = request.BookmarkId,
            TriggerActivityId = request.TriggerActivityId,
            ActivityHandle = request.ActivityHandle,
            SchedulingActivityExecutionId = request.SchedulingActivityExecutionId,
            SchedulingWorkflowInstanceId = request.SchedulingWorkflowInstanceId,
            SchedulingCallStackDepth = request.SchedulingCallStackDepth
        };

        var workflowGraph = await GetWorkflowGraphAsync(workflowInstance, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var workflowResult = await workflowRunner.RunAsync(workflowGraph, workflowState, runWorkflowOptions, cancellationToken);

        workflowState = workflowResult.WorkflowState;

        return new()
        {
            WorkflowInstanceId = WorkflowInstanceId,
            Status = workflowState.Status,
            SubStatus = workflowState.SubStatus,
            Incidents = workflowState.Incidents,
            Output = request.IncludeWorkflowOutput ? new Dictionary<string, object>(workflowState.Output) : null,
            Bookmarks = workflowState.Bookmarks
        };
    }
    
    public async Task<WorkflowInstance> CreateInstanceInternalAsync(CreateWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var workflowDefinitionHandle = request.WorkflowDefinitionHandle;
        var workflowGraph = await GetWorkflowGraphAsync(workflowDefinitionHandle, cancellationToken);

        return CreateWorkflowInstance(workflowGraph.Workflow, request);
    }

    private WorkflowInstance CreateWorkflowInstance(Workflow workflow, CreateWorkflowInstanceRequest request)
    {
        var options = new WorkflowInstanceOptions
        {
            WorkflowInstanceId = WorkflowInstanceId,
            CorrelationId = request.CorrelationId,
            Name = request.Name,
            ParentWorkflowInstanceId = request.ParentId,
            Input = request.Input,
            Properties = request.Properties
        };

        return workflowInstanceManager.CreateWorkflowInstance(workflow, options);
    }

    private async Task<WorkflowInstance> GetWorkflowInstanceAsync(CancellationToken cancellationToken)
    {
        var workflowInstance = await TryGetWorkflowInstanceAsync(cancellationToken);
        if (workflowInstance == null) throw new WorkflowInstanceNotFoundException("Workflow instance not found.", WorkflowInstanceId);
        return workflowInstance;
    }

    private Task<WorkflowInstance?> TryGetWorkflowInstanceAsync(CancellationToken cancellationToken)
    {
        return workflowInstanceManager.FindByIdAsync(WorkflowInstanceId, cancellationToken);
    }

    private async Task<WorkflowGraph> GetWorkflowGraphAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
    {
        var handle = WorkflowDefinitionHandle.ByDefinitionVersionId(workflowInstance.DefinitionVersionId);
        return await GetWorkflowGraphAsync(handle, cancellationToken);
    }

    private async Task<WorkflowGraph> GetWorkflowGraphAsync(WorkflowDefinitionHandle definitionHandle, CancellationToken cancellationToken)
    {
        var result = await workflowDefinitionService.TryFindWorkflowGraphAsync(definitionHandle, cancellationToken);
        if (!result.WorkflowDefinitionExists) throw new WorkflowDefinitionNotFoundException("Workflow definition not found.", definitionHandle);
        if (!result.WorkflowGraphExists) throw new WorkflowMaterializerNotFoundException(result.WorkflowDefinition!.MaterializerName);
        return result.WorkflowGraph!;
    }

    private sealed class CompatibilityActivationGate : IWorkflowActivationGate
    {
        public Task<WorkflowActivationLease> EvaluateAsync(Workflow workflow, string? correlationId, CancellationToken cancellationToken = default)
        {
            if (workflow.Options.ActivationStrategyType != null && workflow.Options.ActivationStrategyType != typeof(AllowAlwaysStrategy))
                throw new InvalidOperationException("The compatibility LocalWorkflowClient constructor cannot enforce a configured activation strategy. Resolve LocalWorkflowClient from dependency injection so the registered activation gate is used.");

            return Task.FromResult(new WorkflowActivationLease(true, null, cancellationToken));
        }
    }
}
