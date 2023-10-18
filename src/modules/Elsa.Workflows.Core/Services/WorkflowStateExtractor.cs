using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class WorkflowStateExtractor : IWorkflowStateExtractor
{
    /// <inheritdoc />
    public WorkflowState Extract(WorkflowExecutionContext workflowExecutionContext)
    {
        var state = new WorkflowState
        {
            Id = workflowExecutionContext.Id,
            DefinitionId = workflowExecutionContext.Workflow.Identity.DefinitionId,
            DefinitionVersionId = workflowExecutionContext.Workflow.Identity.Id,
            DefinitionVersion = workflowExecutionContext.Workflow.Identity.Version,
            CorrelationId = workflowExecutionContext.CorrelationId,
            Status = workflowExecutionContext.Status,
            SubStatus = workflowExecutionContext.SubStatus,
            Bookmarks = workflowExecutionContext.Bookmarks,
            ExecutionLogSequence = workflowExecutionContext.ExecutionLogSequence,
            Input = GetPersistableInput(workflowExecutionContext),
            Output = workflowExecutionContext.Output,
            Incidents = workflowExecutionContext.Incidents,
            CreatedAt = workflowExecutionContext.CreatedAt
        };

        ExtractProperties(state, workflowExecutionContext);
        ExtractActiveActivityExecutionContexts(state, workflowExecutionContext);
        ExtractCompletionCallbacks(state, workflowExecutionContext);
        ExtractScheduledActivities(state, workflowExecutionContext);

        return state;
    }

    private IDictionary<string, object> GetPersistableInput(WorkflowExecutionContext workflowExecutionContext)
    {
        // TODO: This is a temporary solution. We need to find a better way to handle this.
        var persistableInput = workflowExecutionContext.Workflow.Inputs.Where(x => x.StorageDriverType == typeof(WorkflowStorageDriver)).ToList();
        var input = workflowExecutionContext.Input;
        var filteredInput = new Dictionary<string, object>();

        foreach (var inputDefinition in persistableInput)
        {
            if (input.TryGetValue(inputDefinition.Name, out var value))
                filteredInput.Add(inputDefinition.Name, value);
        }

        return filteredInput;
    }

    /// <inheritdoc />
    public WorkflowExecutionContext Apply(WorkflowExecutionContext workflowExecutionContext, WorkflowState state)
    {
        // Do not map input. We don't want to overwrite the input that was passed to the workflow.
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
        ApplyScheduledActivities(state, workflowExecutionContext);
        return workflowExecutionContext;
    }

    private void ExtractProperties(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        state.Properties = workflowExecutionContext.Properties;
    }

    private void ApplyProperties(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        workflowExecutionContext.Properties = state.Properties;
    }

    private static void ApplyActivityExecutionContexts(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        var activityExecutionContexts = state.ActivityExecutionContexts
            .Select(CreateActivityExecutionContext)
            .Where(x => x != null)
            .Select(x => x!)
            .ToList();

        var lookup = activityExecutionContexts.ToDictionary(x => x.Id);

        // Reconstruct hierarchy.
        foreach (var contextState in state.ActivityExecutionContexts.Where(x => !string.IsNullOrWhiteSpace(x.ParentContextId)))
        {
            var parentContext = lookup[contextState.ParentContextId!];
            var contextId = contextState.Id;

            if (lookup.TryGetValue(contextId, out var context))
            {
                context.ExpressionExecutionContext.ParentContext = parentContext.ExpressionExecutionContext;
                context.ParentActivityExecutionContext = parentContext;
            }
        }

        // Assign root expression execution context.
        var rootActivityExecutionContexts = activityExecutionContexts.Where(x => x.ExpressionExecutionContext.ParentContext == null);

        foreach (var rootActivityExecutionContext in rootActivityExecutionContexts)
            rootActivityExecutionContext.ExpressionExecutionContext.ParentContext = workflowExecutionContext.ExpressionExecutionContext;

        workflowExecutionContext.ActivityExecutionContexts = activityExecutionContexts;
        return;

        ActivityExecutionContext? CreateActivityExecutionContext(ActivityExecutionContextState activityExecutionContextState)
        {
            var activity = workflowExecutionContext.FindActivityByNodeId(activityExecutionContextState.ScheduledActivityNodeId);

            // Activity can be null in case the workflow instance was migrated to a newer version that no longer contains this activity.
            if (activity == null)
                return null;

            var properties = activityExecutionContextState.Properties;
            var activityExecutionContext = workflowExecutionContext.CreateActivityExecutionContext(activity);
            activityExecutionContext.Id = activityExecutionContextState.Id;
            activityExecutionContext.Properties = properties;
            activityExecutionContext.ActivityState = activityExecutionContextState.ActivityState ?? new Dictionary<string, object>();
            activityExecutionContext.Status = activityExecutionContextState.Status;
            activityExecutionContext.StartedAt = activityExecutionContextState.StartedAt;
            activityExecutionContext.CompletedAt = activityExecutionContextState.CompletedAt;
            activityExecutionContext.Tag = activityExecutionContextState.Tag;
            activityExecutionContext.DynamicVariables = activityExecutionContextState.DynamicVariables;

            return activityExecutionContext;
        }
    }

    private static void ApplyCompletionCallbacks(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        foreach (var completionCallbackEntry in state.CompletionCallbacks)
        {
            var ownerActivityExecutionContext = workflowExecutionContext.ActivityExecutionContexts.First(x => x.Id == completionCallbackEntry.OwnerInstanceId);
            var childNode = workflowExecutionContext.FindNodeById(completionCallbackEntry.ChildNodeId);

            if (childNode == null)
                continue;

            var callbackName = completionCallbackEntry.MethodName;
            var callbackDelegate = !string.IsNullOrEmpty(callbackName) ? ownerActivityExecutionContext.Activity.GetActivityCompletionCallback(callbackName) : default;
            var tag = completionCallbackEntry.Tag;
            workflowExecutionContext.AddCompletionCallback(ownerActivityExecutionContext, childNode, callbackDelegate, tag);
        }
    }

    private void ApplyScheduledActivities(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        foreach (var activityWorkItemState in state.ScheduledActivities)
        {
            var activity = workflowExecutionContext.FindActivityById(activityWorkItemState.ActivityId);

            if (activity == null)
                continue;

            var ownerContext = workflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.Id == activityWorkItemState.OwnerContextId);
            var existingActivityExecutionContext = workflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.Id == activityWorkItemState.ExistingActivityExecutionContextId);
            var variables = activityWorkItemState.Variables;
            var input = activityWorkItemState.Input;
            var tag = activityWorkItemState.Tag;
            var workItem = new ActivityWorkItem(activity, ownerContext, tag, variables, existingActivityExecutionContext, input);
            workflowExecutionContext.Scheduler.Schedule(workItem);
        }
    }

    private static void ExtractCompletionCallbacks(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        // Assert all referenced owner contexts exist.
        var activeContexts = GetActiveActivityExecutionContexts(workflowExecutionContext.ActivityExecutionContexts).ToList();
        foreach (var completionCallback in workflowExecutionContext.CompletionCallbacks)
        {
            var ownerContext = activeContexts.FirstOrDefault(x => x == completionCallback.Owner);

            if (ownerContext == null)
                throw new Exception("Lost an owner context");
        }

        var completionCallbacks = workflowExecutionContext
            .CompletionCallbacks
            .Select(x => new CompletionCallbackState(x.Owner.Id, x.Child.NodeId, x.CompletionCallback?.Method.Name, x.Tag));

        state.CompletionCallbacks = completionCallbacks.ToList();
    }

    private static void ExtractActiveActivityExecutionContexts(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        ActivityExecutionContextState CreateActivityExecutionContextState(ActivityExecutionContext activityExecutionContext)
        {
            var parentId = activityExecutionContext.ParentActivityExecutionContext?.Id;

            if (parentId != null)
            {
                var parentContext = activityExecutionContext.WorkflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.Id == parentId);

                if (parentContext == null)
                    throw new Exception("We lost a context. This could indicate a bug in a parent activity that completed before (some of) its child activities.");
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
                CompletedAt = activityExecutionContext.CompletedAt,
                Tag = activityExecutionContext.Tag,
                DynamicVariables = activityExecutionContext.DynamicVariables
            };
            return activityExecutionContextState;
        }

        // Only persist non-completed contexts.
        state.ActivityExecutionContexts = GetActiveActivityExecutionContexts(workflowExecutionContext.ActivityExecutionContexts).Reverse().Select(CreateActivityExecutionContextState).ToList();
    }

    private void ExtractScheduledActivities(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        var scheduledActivities = workflowExecutionContext
            .Scheduler.List()
            .Select(x => new ActivityWorkItemState
            {
                ActivityId = x.Activity.Id,
                OwnerContextId = x.Owner?.Id,
                Tag = x.Tag,
                Variables = x.Variables?.ToList(),
                ExistingActivityExecutionContextId = x.ExistingActivityExecutionContext?.Id,
                Input = x.Input
            });

        state.ScheduledActivities = scheduledActivities.ToList();
    }

    private static IEnumerable<ActivityExecutionContext> GetActiveActivityExecutionContexts(IEnumerable<ActivityExecutionContext> activityExecutionContexts)
    {
        var contexts = activityExecutionContexts.ToList();

        // // If there are any faulted contexts, keep everything so that the user can fix the issue and potentially reschedule existing instances.
        // if (contexts.Any(x => x.Status == ActivityStatus.Faulted))
            return contexts;

        // return contexts
        //     .Where(x => !x.IsCompleted)
        //     .ToList();
    }
}