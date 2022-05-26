using System.Reflection;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Implementations;

public class WorkflowStateSerializer : IWorkflowStateSerializer
{
    private readonly IServiceProvider _serviceProvider;

    public WorkflowStateSerializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public WorkflowState ReadState(WorkflowExecutionContext workflowExecutionContext)
    {
        var state = new WorkflowState
        {
            Id = workflowExecutionContext.Id,
            CorrelationId = workflowExecutionContext.CorrelationId,
            Status = workflowExecutionContext.Status,
            SubStatus = workflowExecutionContext.SubStatus
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
        workflowExecutionContext.CorrelationId = state.CorrelationId;
        workflowExecutionContext.SubStatus = state.SubStatus; 
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
        foreach (var entry in state.ActivityOutput)
        {
            var activityId = entry.Key;
            var node = workflowExecutionContext.FindActivityNodeById(activityId);
            var activityType = node.Activity.GetType();

            foreach (var outputEntry in entry.Value)
            {
                var propertyName = outputEntry.Key;
                var propertyValue = outputEntry.Value;
                var propertyInfo = activityType.GetProperty(propertyName, BindingFlags.Public)!;
                propertyInfo.SetValue(node, propertyValue);
            }
        }
    }

    private void SetCompletionCallbacks(WorkflowState state, WorkflowExecutionContext workflowExecutionContext)
    {
        foreach (var completionCallbackEntry in state.CompletionCallbacks)
        {
            var owner = workflowExecutionContext.ActivityExecutionContexts.First(x => x.Id == completionCallbackEntry.OwnerId);
            var child = workflowExecutionContext.FindActivityNodeById(completionCallbackEntry.ChildId).Activity;
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
                Properties = activityExecutionContext.ApplicationProperties,
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
            var cancellationToken = workflowExecutionContext.CancellationToken;
            var activity = workflowExecutionContext.FindActivityById(activityExecutionContextState.ScheduledActivityId);
            var register = workflowExecutionContext.Register;
            var workflow = workflowExecutionContext.Workflow;
            var expressionInput = workflowExecutionContext.Input;
            var transientProperties = workflowExecutionContext.TransientProperties;
            var applicationProperties = ExpressionExecutionContextExtensions.CreateApplicationPropertiesFrom(workflow, transientProperties, expressionInput);
            var expressionExecutionContext = new ExpressionExecutionContext(_serviceProvider, register, null, applicationProperties, cancellationToken);
            var properties = activityExecutionContextState.Properties;
            var activityExecutionContext = new ActivityExecutionContext(workflowExecutionContext, default, expressionExecutionContext, activity, cancellationToken)
            {
                Id = activityExecutionContextState.Id,
                ApplicationProperties = properties
            };
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

    private IDictionary<string, object?> GetOutputFrom(ActivityNode activityNode) =>
        activityNode.GetType().GetProperties(BindingFlags.Public).Where(x => x.GetCustomAttribute<OutputAttribute>() != null).ToDictionary(x => x.Name, x => (object?)x.GetValue(activityNode));
}