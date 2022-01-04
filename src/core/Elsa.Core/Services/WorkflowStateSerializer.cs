using System.Reflection;
using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.State;

namespace Elsa.Services;

public class WorkflowStateSerializer : IWorkflowStateSerializer
{
    public WorkflowState ReadState(WorkflowExecutionContext workflowExecutionContext)
    {
        var state = new WorkflowState
        {
            Id = workflowExecutionContext.Id
        };

        //GetOutput(state, workflowExecutionContext);
        GetProperties(state, workflowExecutionContext);
        GetCompletionCallbacks(state, workflowExecutionContext);
        GetActivityExecutionContexts(state, workflowExecutionContext);

        return state;
    }

    public void WriteState(WorkflowExecutionContext workflowExecutionContext, WorkflowState state)
    {
        workflowExecutionContext.Id = state.Id;
        //SetOutput(state, workflowExecutionContext);
        SetProperties(state, workflowExecutionContext);
        SetActivityExecutionContexts(state, workflowExecutionContext);
        SetCompletionCallbacks(state, workflowExecutionContext);
    }

    private void GetProperties(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        state.Properties = workflowExecutionContext.Properties;
    }
        
    private void SetProperties(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        workflowExecutionContext.Properties = state.Properties;
    }

    private void GetOutput(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        foreach (var node in workflowExecutionContext.Nodes)
            GetOutput(state, node);
    }

    private void GetOutput(WorkflowState state, ActivityNode activityNode)
    {
        var output = GetOutputFrom(activityNode);

        if (output.Any())
            state.ActivityOutput.Add(activityNode.NodeId, output);
    }

    private void SetOutput(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        foreach (var nodeEntry in state.ActivityOutput)
        {
            var nodeId = nodeEntry.Key;
            var node = workflowExecutionContext.FindNodeById(nodeId);
            var nodeType = node.GetType();

            foreach (var outputEntry in nodeEntry.Value)
            {
                var propertyName = outputEntry.Key;
                var propertyValue = outputEntry.Value;
                var propertyInfo = nodeType.GetProperty(propertyName, BindingFlags.Public)!;
                propertyInfo.SetValue(node, propertyValue);
            }
        }
    }

    private void SetCompletionCallbacks(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        foreach (var completionCallbackEntry in state.CompletionCallbacks)
        {
            var owner = workflowExecutionContext.ActivityExecutionContexts.First(x => x.Id == completionCallbackEntry.OwnerId);
            var child = workflowExecutionContext.FindNodeById(completionCallbackEntry.ChildId).Activity;
            var callbackName = completionCallbackEntry.MethodName;
            var callbackDelegate = owner.Activity.GetActivityCompletionCallback(callbackName);
            workflowExecutionContext.AddCompletionCallback(owner, child, callbackDelegate);
        }
    }

    private void GetCompletionCallbacks(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        var completionCallbacks = workflowExecutionContext.CompletionCallbacks.Select(x => new CompletionCallbackState(x.Owner.Id, x.Child.Id, x.CompletionCallback.Method.Name));
        state.CompletionCallbacks = completionCallbacks.ToList();
    }

    private void GetActivityExecutionContexts(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        ActivityExecutionContextState CreateActivityExecutionContextState(ActivityExecutionContext activityExecutionContext)
        {
            var registerState = new RegisterState(activityExecutionContext.ExpressionExecutionContext.Register.Locations);
            var activityExecutionContextState = new ActivityExecutionContextState
            {
                Id = activityExecutionContext.Id,
                ParentContextId = activityExecutionContext.ParentActivityExecutionContext?.Id,
                ScheduledActivityId = activityExecutionContext.Activity.Id,
                OwnerActivityId = activityExecutionContext.ParentActivityExecutionContext?.Activity.Id,
                Properties = activityExecutionContext.Properties,
                Register = registerState
            };
            return activityExecutionContextState;
        }

        state.ActivityExecutionContexts = workflowExecutionContext.ActivityExecutionContexts.Reverse().Select(CreateActivityExecutionContextState).ToList();
    }

    private void SetActivityExecutionContexts(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        ActivityExecutionContext CreateActivityExecutionContext(ActivityExecutionContextState activityExecutionContextState)
        {
            var activity = workflowExecutionContext.FindActivityById(activityExecutionContextState.ScheduledActivityId);
            var register = new Register(activityExecutionContextState.Register.Locations);
            var expressionExecutionContext = new ExpressionExecutionContext(register, default);
            var properties = activityExecutionContextState.Properties;
            var activityExecutionContext = new ActivityExecutionContext(workflowExecutionContext, default, expressionExecutionContext, activity, workflowExecutionContext.CancellationToken)
            {
                Id = activityExecutionContextState.Id,
                Properties = properties
            };
            return activityExecutionContext;
        }
        
        var activityExecutionContexts = state.ActivityExecutionContexts.Select(CreateActivityExecutionContext).ToList();
        var lookup = activityExecutionContexts.ToDictionary(x => x.Id);

        // Reconstruct hierarchy.
        foreach (var contextState in state.ActivityExecutionContexts.Where(x => !string.IsNullOrWhiteSpace(x.ParentContextId)))
        {
            var contextId = contextState.Id;
            var context = lookup[contextId];
            context.ParentActivityExecutionContext = lookup[contextState.ParentContextId!];
        }
        
        workflowExecutionContext.ActivityExecutionContexts = activityExecutionContexts;
    }

    private IDictionary<string, object?> GetOutputFrom(ActivityNode activityNode) =>
        activityNode.GetType().GetProperties(BindingFlags.Public).Where(x => x.GetCustomAttribute<OutputAttribute>() != null).ToDictionary(x => x.Name, x => (object?)x.GetValue(activityNode));
}