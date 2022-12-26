using System.Reflection;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Implementations;

/// <inheritdoc />
public class WorkflowStateSerializer : IWorkflowStateSerializer
{
    /// <inheritdoc />
    public WorkflowState SerializeState(WorkflowExecutionContext workflowExecutionContext)
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
            Fault = SerializeFault(workflowExecutionContext.Fault)
        };

        SerializeProperties(state, workflowExecutionContext);
        SerializeCompletionCallbacks(state, workflowExecutionContext);
        SerializeActivityExecutionContexts(state, workflowExecutionContext);

        return state;
    }

    /// <inheritdoc />
    public void DeserializeState(WorkflowExecutionContext workflowExecutionContext, WorkflowState state)
    {
        workflowExecutionContext.Id = state.Id;
        workflowExecutionContext.CorrelationId = state.CorrelationId;
        workflowExecutionContext.SubStatus = state.SubStatus;
        workflowExecutionContext.Bookmarks = state.Bookmarks;
        DeserializeProperties(state, workflowExecutionContext);
        DeserializeActivityExecutionContexts(state, workflowExecutionContext);
        DeserializeCompletionCallbacks(state, workflowExecutionContext);
    }

    private void SerializeProperties(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        state.Properties = new PropertyBag(workflowExecutionContext.Properties);
    }

    private void DeserializeProperties(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        workflowExecutionContext.Properties = state.Properties.Properties;
    }

    private static void DeserializeCompletionCallbacks(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        foreach (var completionCallbackEntry in state.CompletionCallbacks)
        {
            var owner = workflowExecutionContext.ActivityExecutionContexts.First(x => x.Id == completionCallbackEntry.OwnerId);
            var child = workflowExecutionContext.FindNodeById(completionCallbackEntry.ChildId).Activity;
            var callbackName = completionCallbackEntry.MethodName;
            var callbackDelegate = !string.IsNullOrEmpty(callbackName) ? owner.Activity.GetActivityCompletionCallback(callbackName) : default;
            workflowExecutionContext.AddCompletionCallback(owner, child, callbackDelegate);
        }
    }

    private static void SerializeCompletionCallbacks(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        // Assert all referenced owner contexts exist.
        foreach (var completionCallback in workflowExecutionContext.CompletionCallbacks)
        {
            var owmnerContext = workflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x == completionCallback.Owner);

            if (owmnerContext == null)
                throw new Exception("Lost an owner context");
        }
        
        var completionCallbacks = workflowExecutionContext.CompletionCallbacks.Select(x => new CompletionCallbackState(x.Owner.Id, x.Child.Id, x.CompletionCallback?.Method.Name));
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
                ScheduledActivityId = activityExecutionContext.Activity.Id,
                OwnerActivityId = activityExecutionContext.ParentActivityExecutionContext?.Activity.Id,
                Properties = activityExecutionContext.Properties,
            };
            return activityExecutionContextState;
        }

        state.ActivityExecutionContexts = workflowExecutionContext.ActivityExecutionContexts.Reverse().Select(CreateActivityExecutionContextState).ToList();
    }

    private static void DeserializeActivityExecutionContexts(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        ActivityExecutionContext CreateActivityExecutionContext(ActivityExecutionContextState activityExecutionContextState)
        {
            var activity = workflowExecutionContext.FindActivityById(activityExecutionContextState.ScheduledActivityId);
            var properties = activityExecutionContextState.Properties;
            var activityExecutionContext = workflowExecutionContext.CreateActivityExecutionContext(activity);
            activityExecutionContext.Id = activityExecutionContextState.Id;
            activityExecutionContext.Properties = properties;

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