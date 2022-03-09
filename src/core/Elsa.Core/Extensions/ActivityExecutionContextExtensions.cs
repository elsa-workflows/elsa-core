using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Extensions;

public static class ActivityExecutionContextExtensions
{
    public static bool TryGetInput<T>(this ActivityExecutionContext context, string key, out T value) => context.Input.TryGetValue(key, out value!);
    public static T GetInput<T>(this ActivityExecutionContext context) => context.GetInput<T>(typeof(T).Name);
    public static T GetInput<T>(this ActivityExecutionContext context, string key) => (T)context.Input[key];

    public static WorkflowExecutionLogEntry AddExecutionLogEntry(this ActivityExecutionContext context, string eventName, string? message = default, object? payload = default) =>
        context.AddExecutionLogEntry(eventName, message, default, payload);

    public static WorkflowExecutionLogEntry AddExecutionLogEntry(this ActivityExecutionContext context, string eventName, string? message = default, string? source = default, object? payload = default)
    {
        var activity = context.Activity;
        var workflowExecutionContext = context.WorkflowExecutionContext;
        var now = context.GetRequiredService<ISystemClock>().UtcNow;
        var logEntry = new WorkflowExecutionLogEntry(activity.Id, activity.TypeName, now, eventName, message, source, payload);
        workflowExecutionContext.ExecutionLog.Add(logEntry);
        return logEntry;
    }

    public static Variable SetVariable(this ActivityExecutionContext context, string name, object? value) => context.ExpressionExecutionContext.SetVariable(name, value);
    public static T? GetVariable<T>(this ActivityExecutionContext context, string name) => context.ExpressionExecutionContext.GetVariable<T?>(name);
}