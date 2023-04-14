using System.Text.Json;
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
            DefinitionVersion = workflowExecutionContext.Workflow.Identity.Version,
            CorrelationId = workflowExecutionContext.CorrelationId,
            Status = workflowExecutionContext.Status,
            SubStatus = workflowExecutionContext.SubStatus,
            Bookmarks = workflowExecutionContext.Bookmarks,
            Output = workflowExecutionContext.Output,
            Fault = SerializeFault(workflowExecutionContext.Fault)
        };

        SerializeProperties(state, workflowExecutionContext);
        SerializeCompletionCallbacks(state, workflowExecutionContext);
        SerializeActivityExecutionContexts(state, workflowExecutionContext);

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
        DeserializeProperties(state, workflowExecutionContext);
        DeserializeActivityExecutionContexts(state, workflowExecutionContext);
        DeserializeCompletionCallbacks(state, workflowExecutionContext);
        return workflowExecutionContext;
    }

    private void SerializeProperties(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        state.Properties = new PropertyBag(workflowExecutionContext.Properties);
    }

    private void DeserializeProperties(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        workflowExecutionContext.Properties = state.Properties.Dictionary;
    }

    private static void DeserializeCompletionCallbacks(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        foreach (var completionCallbackEntry in state.CompletionCallbacks)
        {
            var ownerActivityExecutionContext = workflowExecutionContext.ActivityExecutionContexts.First(x => x.Id == completionCallbackEntry.OwnerInstanceId);
            var childNode = workflowExecutionContext.ActivityExecutionContexts.First(x => x.NodeId == completionCallbackEntry.ChildNodeId).ActivityNode;
            var callbackName = completionCallbackEntry.MethodName;
            var callbackDelegate = !string.IsNullOrEmpty(callbackName) ? ownerActivityExecutionContext.Activity.GetActivityCompletionCallback(callbackName) : default;
            workflowExecutionContext.AddCompletionCallback(ownerActivityExecutionContext, childNode, callbackDelegate);
        }
    }

    private static void SerializeCompletionCallbacks(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        // Assert all referenced owner contexts exist.
        foreach (var completionCallback in workflowExecutionContext.CompletionCallbacks)
        {
            var ownerContext = workflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x == completionCallback.Owner);

            if (ownerContext == null)
                throw new Exception("Lost an owner context");
        }
        
        var completionCallbacks = workflowExecutionContext.CompletionCallbacks.Select(x => new CompletionCallbackState(x.Owner.Id, x.Child.NodeId, x.CompletionCallback?.Method.Name));
        state.CompletionCallbacks = completionCallbacks.ToList();
    }

    private static void SerializeActivityExecutionContexts(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        ActivityExecutionContextState CreateActivityExecutionContextState(ActivityExecutionContext activityExecutionContext)
        {
            var parentId = activityExecutionContext.ParentActivityExecutionContext?.Id;

            if (parentId != null)
            {
                var parentContext = activityExecutionContext.WorkflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.Id == parentId);

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
                ActivityState = activityExecutionContext.ActivityState
            };
            return activityExecutionContextState;
        }

        state.ActivityExecutionContexts = workflowExecutionContext.ActivityExecutionContexts.Reverse().Select(CreateActivityExecutionContextState).ToList();
    }

    private static void DeserializeActivityExecutionContexts(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        ActivityExecutionContext CreateActivityExecutionContext(ActivityExecutionContextState activityExecutionContextState)
        {
            var activity = workflowExecutionContext.FindActivityByNodeId(activityExecutionContextState.ScheduledActivityNodeId);
            var properties = activityExecutionContextState.Properties;
            var activityExecutionContext = workflowExecutionContext.CreateActivityExecutionContext(activity);
            activityExecutionContext.Id = activityExecutionContextState.Id;
            activityExecutionContext.Properties = properties;
            activityExecutionContext.ActivityState = activityExecutionContextState.ActivityState ?? new Dictionary<string, object>();

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
    
    private static WorkflowFaultState? SerializeFault(WorkflowFault? fault)
    {
        if (fault == null)
            return null;

        var exceptionState = ExceptionState.FromException(fault.Exception);
        return new WorkflowFaultState(exceptionState, fault.Message, fault.FaultedActivityId);
    }
}