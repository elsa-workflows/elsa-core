using Elsa.Extensions;
using Elsa.Workflows.Models;
using Elsa.Workflows.Services;
using Elsa.Workflows.State;

namespace Elsa.Workflows;

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
            ParentWorkflowInstanceId = workflowExecutionContext.ParentWorkflowInstanceId,
            Status = workflowExecutionContext.Status,
            SubStatus = workflowExecutionContext.SubStatus,
            Bookmarks = workflowExecutionContext.Bookmarks,
            ExecutionLogSequence = workflowExecutionContext.ExecutionLogSequence,
            Input = GetPersistableInput(workflowExecutionContext),
            Output = workflowExecutionContext.Output,
            Incidents = workflowExecutionContext.Incidents,
            IsSystem = workflowExecutionContext.Workflow.IsSystem,
            CreatedAt = workflowExecutionContext.CreatedAt,
            UpdatedAt = workflowExecutionContext.UpdatedAt,
            FinishedAt = workflowExecutionContext.FinishedAt
        };

        ExtractProperties(state, workflowExecutionContext);
        ExtractActiveActivityExecutionContexts(state, workflowExecutionContext);
        ExtractCompletionCallbacks(state, workflowExecutionContext);
        ExtractScheduledActivities(state, workflowExecutionContext);

        return state;
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionContext> ApplyAsync(WorkflowExecutionContext workflowExecutionContext, WorkflowState state)
    {
        workflowExecutionContext.Id = state.Id;
        workflowExecutionContext.CorrelationId = state.CorrelationId;
        workflowExecutionContext.ParentWorkflowInstanceId = state.ParentWorkflowInstanceId;
        workflowExecutionContext.SubStatus = state.SubStatus;
        workflowExecutionContext.Bookmarks = state.Bookmarks;
        workflowExecutionContext.Output = state.Output;
        workflowExecutionContext.ExecutionLogSequence = state.ExecutionLogSequence;
        workflowExecutionContext.CreatedAt = state.CreatedAt;
        workflowExecutionContext.UpdatedAt = state.UpdatedAt;
        workflowExecutionContext.FinishedAt = state.FinishedAt;
        ApplyInput(state, workflowExecutionContext);
        ApplyProperties(state, workflowExecutionContext);
        await ApplyActivityExecutionContextsAsync(state, workflowExecutionContext);
        ApplyCompletionCallbacks(state, workflowExecutionContext);
        ApplyScheduledActivities(state, workflowExecutionContext);
        return workflowExecutionContext;
    }
    
    private void ApplyInput(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        // Only add input from state if the input doesn't already exist on the workflow execution context.
        foreach (var inputItem in state.Input)
            if (!workflowExecutionContext.Input.ContainsKey(inputItem.Key)) workflowExecutionContext.Input.Add(inputItem.Key, inputItem.Value);
    }

    private IDictionary<string, object> GetPersistableInput(WorkflowExecutionContext workflowExecutionContext)
    {
        // TODO: This is a temporary solution. We need to find a better way to handle this.
        var persistableInput = workflowExecutionContext.Workflow.Inputs.Where(x => x.StorageDriverType == typeof(WorkflowStorageDriver) || x.StorageDriverType == typeof(WorkflowInstanceStorageDriver)).ToList();
        var input = workflowExecutionContext.Input;
        var filteredInput = new Dictionary<string, object>();

        foreach (var inputDefinition in persistableInput)
        {
            if (input.TryGetValue(inputDefinition.Name, out var value))
                filteredInput.Add(inputDefinition.Name, value);
        }

        return filteredInput;
    }

    private void ExtractProperties(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        state.Properties = workflowExecutionContext.Properties;
    }

    private void ApplyProperties(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        // Merge properties.
        foreach (var property in state.Properties)
            workflowExecutionContext.Properties[property.Key] = property.Value;
    }

    private static async Task ApplyActivityExecutionContextsAsync(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        var activityExecutionContexts = (await Task.WhenAll(
                state.ActivityExecutionContexts.Select(async item => await CreateActivityExecutionContextAsync(item))))
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

        async Task<ActivityExecutionContext?> CreateActivityExecutionContextAsync(ActivityExecutionContextState activityExecutionContextState)
        {
            var activity = workflowExecutionContext.FindActivityByNodeId(activityExecutionContextState.ScheduledActivityNodeId);

            // Activity can be null in case the workflow instance was migrated to a newer version that no longer contains this activity.
            if (activity == null)
                return null;

            var properties = activityExecutionContextState.Properties;
            var activityExecutionContext = await workflowExecutionContext.CreateActivityExecutionContextAsync(activity);
            activityExecutionContext.Id = activityExecutionContextState.Id;
            activityExecutionContext.Properties.Merge(properties);
            
            if(activityExecutionContextState.ActivityState != null)
                activityExecutionContext.ActivityState.Merge(activityExecutionContextState.ActivityState);
            
            activityExecutionContext.TransitionTo(activityExecutionContextState.Status);
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
            var activity = workflowExecutionContext.FindActivityByNodeId(activityWorkItemState.ActivityNodeId);

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
        // Assert that all referenced owner contexts exist.
        var activeContexts = workflowExecutionContext.GetActiveActivityExecutionContexts().ToList();
        foreach (var completionCallback in workflowExecutionContext.CompletionCallbacks)
        {
            var ownerContext = activeContexts.FirstOrDefault(x => x == completionCallback.Owner);

            if (ownerContext == null)
                throw new("Lost an owner context");
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
                    throw new("We lost a context. This could indicate a bug in a parent activity that completed before (some of) its child activities.");
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
        state.ActivityExecutionContexts = workflowExecutionContext.GetActiveActivityExecutionContexts().Reverse().Select(CreateActivityExecutionContextState).ToList();
    }

    private void ExtractScheduledActivities(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        var scheduledActivities = workflowExecutionContext
            .Scheduler.List()
            .Select(x => new ActivityWorkItemState
            {
                ActivityNodeId = x.Activity.NodeId,
                OwnerContextId = x.Owner?.Id,
                Tag = x.Tag,
                Variables = x.Variables?.ToList(),
                ExistingActivityExecutionContextId = x.ExistingActivityExecutionContext?.Id,
                Input = x.Input
            });

        state.ScheduledActivities = scheduledActivities.ToList();
    }
}