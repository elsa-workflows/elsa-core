using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class WorkflowExecutionContextMapper : IWorkflowExecutionContextMapper
{
    /// <inheritdoc />
    public WorkflowState Extract(WorkflowExecutionContext workflowExecutionContext)
    {
        var state = new WorkflowState
        {
            Id = workflowExecutionContext.Id,
            DefinitionId = workflowExecutionContext.Workflow.Identity.DefinitionId,
            DefinitionVersionId = workflowExecutionContext.Workflow.Id,
            DefinitionVersion = workflowExecutionContext.Workflow.Identity.Version,
            CorrelationId = workflowExecutionContext.CorrelationId,
            Status = workflowExecutionContext.Status,
            SubStatus = workflowExecutionContext.SubStatus,
            Bookmarks = workflowExecutionContext.Bookmarks,
            ExecutionLogSequence = workflowExecutionContext.ExecutionLogSequence,
            Output = workflowExecutionContext.Output,
            Fault = MapFault(workflowExecutionContext.Fault),
            CreatedAt = workflowExecutionContext.CreatedAt
        };

        ExportProperties(state, workflowExecutionContext);
        ExtractCompletionCallbacks(state, workflowExecutionContext);
        ExtractActiveActivityExecutionContexts(state, workflowExecutionContext);

        return state;
    }

    /// <inheritdoc />
    public WorkflowExecutionContext Apply(WorkflowExecutionContext workflowExecutionContext, WorkflowState state)
    {
        workflowExecutionContext.Id = state.Id;
        workflowExecutionContext.CorrelationId = state.CorrelationId;
        workflowExecutionContext.SubStatus = state.SubStatus;
        workflowExecutionContext.Bookmarks = state.Bookmarks;
        workflowExecutionContext.Output = state.Output;
        workflowExecutionContext.ExecutionLogSequence = state.ExecutionLogSequence;
        workflowExecutionContext.CreatedAt = state.CreatedAt;
        ApplyProperties(state, workflowExecutionContext);
        ApplyActivityExecutionContexts(state, workflowExecutionContext);
        ApplyCompletionCallbacks(state, workflowExecutionContext);
        return workflowExecutionContext;
    }

    private void ExportProperties(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        state.Properties = workflowExecutionContext.Properties;
    }

    private void ApplyProperties(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        workflowExecutionContext.Properties = state.Properties;
    }

    private static void ApplyCompletionCallbacks(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        foreach (var completionCallbackEntry in state.CompletionCallbacks)
        {
            var ownerActivityExecutionContext = workflowExecutionContext.ActiveActivityExecutionContexts.First(x => x.Id == completionCallbackEntry.OwnerInstanceId);
            var childNode = workflowExecutionContext.ActiveActivityExecutionContexts.First(x => x.NodeId == completionCallbackEntry.ChildNodeId).ActivityNode;
            var callbackName = completionCallbackEntry.MethodName;
            var callbackDelegate = !string.IsNullOrEmpty(callbackName) ? ownerActivityExecutionContext.Activity.GetActivityCompletionCallback(callbackName) : default;
            workflowExecutionContext.AddCompletionCallback(ownerActivityExecutionContext, childNode, callbackDelegate);
        }
    }

    private static void ExtractCompletionCallbacks(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        // Assert all referenced owner contexts exist.
        foreach (var completionCallback in workflowExecutionContext.CompletionCallbacks)
        {
            var ownerContext = workflowExecutionContext.ActiveActivityExecutionContexts.FirstOrDefault(x => x == completionCallback.Owner);

            if (ownerContext == null)
                throw new Exception("Lost an owner context");
        }
        
        var completionCallbacks = workflowExecutionContext.CompletionCallbacks.Select(x => new CompletionCallbackState(x.Owner.Id, x.Child.NodeId, x.CompletionCallback?.Method.Name));
        state.CompletionCallbacks = completionCallbacks.ToList();
    }

    private static void ExtractActiveActivityExecutionContexts(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        ActivityExecutionContextState CreateActivityExecutionContextState(ActivityExecutionContext activityExecutionContext)
        {
            var parentId = activityExecutionContext.ParentActivityExecutionContext?.Id;

            if (parentId != null)
            {
                var parentContext = activityExecutionContext.WorkflowExecutionContext.ActiveActivityExecutionContexts.FirstOrDefault(x => x.Id == parentId);

                if (parentContext == null)
                    throw new Exception("We lost a context");
            }
            
            var activityExecutionContextState = new ActivityExecutionContextState
            {
                Id = activityExecutionContext.Id,
                ParentContextId = activityExecutionContext.ParentActivityExecutionContext?.Id,
                ScheduledActivityNodeId = activityExecutionContext.NodeId,
                OwnerActivityNodeId = activityExecutionContext.ParentActivityExecutionContext?.NodeId,
                Properties = activityExecutionContext.Properties,
                ActivityState = activityExecutionContext.ActivityState,
                Status = activityExecutionContext.Status,
                StartedAt = activityExecutionContext.StartedAt,
                CompletedAt = activityExecutionContext.CompletedAt
            };
            return activityExecutionContextState;
        }

        // Notice that we only persist the active activity execution contexts. The completed ones are not persisted.
        state.ActivityExecutionContexts = workflowExecutionContext.ActiveActivityExecutionContexts.Reverse().Select(CreateActivityExecutionContextState).ToList();
    }

    private static void ApplyActivityExecutionContexts(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        ActivityExecutionContext CreateActivityExecutionContext(ActivityExecutionContextState activityExecutionContextState)
        {
            var activity = workflowExecutionContext.FindActivityByNodeId(activityExecutionContextState.ScheduledActivityNodeId);
            var properties = activityExecutionContextState.Properties;
            var activityExecutionContext = workflowExecutionContext.CreateActivityExecutionContext(activity);
            activityExecutionContext.Id = activityExecutionContextState.Id;
            activityExecutionContext.Properties = properties;
            activityExecutionContext.ActivityState = activityExecutionContextState.ActivityState ?? new Dictionary<string, object>();
            activityExecutionContext.Status = activityExecutionContextState.Status;
            activityExecutionContext.StartedAt = activityExecutionContextState.StartedAt;
            activityExecutionContext.CompletedAt = activityExecutionContextState.CompletedAt;

            return activityExecutionContext;
        }

        var activityExecutionContexts = state.ActivityExecutionContexts.Select(CreateActivityExecutionContext).ToList();
        var lookup = activityExecutionContexts.ToDictionary(x => x.Id);

        // Reconstruct hierarchy.
        foreach (var contextState in state.ActivityExecutionContexts.Where(x => !string.IsNullOrWhiteSpace(x.ParentContextId)))
        {
            var parentContext = lookup[contextState.ParentContextId!];
            var contextId = contextState.Id;
            var context = lookup[contextId];
            context.ExpressionExecutionContext.ParentContext = parentContext.ExpressionExecutionContext;
            context.ParentActivityExecutionContext = parentContext;
        }
        
        // Assign root expression execution context.
        var rootActivityExecutionContexts = activityExecutionContexts.Where(x => x.ExpressionExecutionContext.ParentContext == null);

        foreach (var rootActivityExecutionContext in rootActivityExecutionContexts) 
            rootActivityExecutionContext.ExpressionExecutionContext.ParentContext = workflowExecutionContext.ExpressionExecutionContext;

        workflowExecutionContext.ActivityExecutionContexts = activityExecutionContexts;
    }
    
    private static WorkflowFaultState? MapFault(WorkflowFault? fault)
    {
        if (fault == null)
            return null;

        var exceptionState = ExceptionState.FromException(fault.Exception);
        return new WorkflowFaultState(exceptionState, fault.Message, fault.FaultedActivityId);
    }
}