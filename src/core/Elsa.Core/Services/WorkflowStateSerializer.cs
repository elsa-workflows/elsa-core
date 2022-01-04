using System.Reflection;
using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.State;
using Microsoft.Extensions.Logging;

namespace Elsa.Services;

public class WorkflowStateSerializer : IWorkflowStateSerializer
{
    private readonly ILogger _logger;

    public WorkflowStateSerializer(ILogger<WorkflowStateSerializer> logger)
    {
        _logger = logger;
    }

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
                ScheduledActivityId = activityExecutionContext.Activity.Id,
                OwnerActivityId = activityExecutionContext.ParentActivityExecutionContext?.Activity.Id,
                Properties = activityExecutionContext.Properties,
                Register = registerState
            };
            return activityExecutionContextState;
        }

        // Create a tupled list of contexts and states.
        var tuples = workflowExecutionContext.ActivityExecutionContexts.Reverse().Select(x => (x, CreateActivityExecutionContextState(x))).ToList();

        // Construct hierarchy.
        foreach (var tuple in tuples)
            tuple.Item2.ParentActivityExecutionContext = tuples.FirstOrDefault(x => tuple.x.ParentActivityExecutionContext == x.x).Item2;

        state.ActivityExecutionContexts = tuples.Select(x => x.Item2).ToList();
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

        // Create a tupled list of states and contexts.
        var tuples = state.ActivityExecutionContexts.Select(x => (x, CreateActivityExecutionContext(x))).ToList();

        // Reconstruct hierarchy.
        foreach (var tuple in tuples)
        {
            var activityExecutionContext = tuple.Item2;
            activityExecutionContext.ParentActivityExecutionContext = tuples.FirstOrDefault(x => tuple.x.ParentActivityExecutionContext == x.x).Item2;
            activityExecutionContext.ExpressionExecutionContext.ParentContext = activityExecutionContext.ParentActivityExecutionContext?.ExpressionExecutionContext;
        }

        workflowExecutionContext.ActivityExecutionContexts = new List<ActivityExecutionContext>(tuples.Select(x => x.Item2));
    }

    private IDictionary<string, object?> GetOutputFrom(ActivityNode activityNode) =>
        activityNode.GetType().GetProperties(BindingFlags.Public).Where(x => x.GetCustomAttribute<OutputAttribute>() != null).ToDictionary(x => x.Name, x => (object?)x.GetValue(activityNode));
}