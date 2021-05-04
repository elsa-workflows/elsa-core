using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Signaling
{
    public static class InterruptTriggerExtensions
    {
        public static ISetupActivity<InterruptTrigger> WithWorkflowInstanceId(this ISetupActivity<InterruptTrigger> activity, Func<ActivityExecutionContext, ValueTask<string?>> value) => activity.Set(x => x.WorkflowInstanceId, value);
        public static ISetupActivity<InterruptTrigger> WithWorkflowInstanceId(this ISetupActivity<InterruptTrigger> activity, Func<ActivityExecutionContext, string?> value) => activity.Set(x => x.WorkflowInstanceId, value);
        public static ISetupActivity<InterruptTrigger> WithWorkflowInstanceId(this ISetupActivity<InterruptTrigger> activity, Func<ValueTask<string?>> value) => activity.Set(x => x.WorkflowInstanceId, value);
        public static ISetupActivity<InterruptTrigger> WithWorkflowInstanceId(this ISetupActivity<InterruptTrigger> activity, Func<string?> value) => activity.Set(x => x.WorkflowInstanceId, value);
        public static ISetupActivity<InterruptTrigger> WithWorkflowInstanceId(this ISetupActivity<InterruptTrigger> activity, string? value) => activity.Set(x => x.WorkflowInstanceId, value);
        
        public static ISetupActivity<InterruptTrigger> WithBlockingActivityId(this ISetupActivity<InterruptTrigger> activity, Func<ActivityExecutionContext, ValueTask<string?>> value) => activity.Set(x => x.BlockingActivityId, value);
        public static ISetupActivity<InterruptTrigger> WithBlockingActivityId(this ISetupActivity<InterruptTrigger> activity, Func<ActivityExecutionContext, string?> value) => activity.Set(x => x.BlockingActivityId, value);
        public static ISetupActivity<InterruptTrigger> WithBlockingActivityId(this ISetupActivity<InterruptTrigger> activity, Func<ValueTask<string?>> value) => activity.Set(x => x.BlockingActivityId, value);
        public static ISetupActivity<InterruptTrigger> WithBlockingActivityId(this ISetupActivity<InterruptTrigger> activity, Func<string?> value) => activity.Set(x => x.BlockingActivityId, value);
        public static ISetupActivity<InterruptTrigger> WithBlockingActivityId(this ISetupActivity<InterruptTrigger> activity, string? value) => activity.Set(x => x.BlockingActivityId, value);
        
        public static ISetupActivity<InterruptTrigger> WithInput(this ISetupActivity<InterruptTrigger> activity, Func<ActivityExecutionContext, ValueTask<object?>> value) => activity.Set(x => x.Input, value);
        public static ISetupActivity<InterruptTrigger> WithInput(this ISetupActivity<InterruptTrigger> activity, Func<ActivityExecutionContext, object?> value) => activity.Set(x => x.Input, value);
        public static ISetupActivity<InterruptTrigger> WithInput(this ISetupActivity<InterruptTrigger> activity, Func<ValueTask<object?>> value) => activity.Set(x => x.Input, value);
        public static ISetupActivity<InterruptTrigger> WithInput(this ISetupActivity<InterruptTrigger> activity, Func<object?> value) => activity.Set(x => x.Input, value);
        public static ISetupActivity<InterruptTrigger> WithInput(this ISetupActivity<InterruptTrigger> activity, object? value) => activity.Set(x => x.Input, value);
    }
}