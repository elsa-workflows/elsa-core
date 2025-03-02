using Elsa.Extensions;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Provides a set of extension methods for working with deferred tasks within a workflow execution context.
/// </summary>
public static class DeferredTasksExecutionContextExtensions
{
    private static readonly object DeferredTasksKey = new();

    /// <summary>
    /// Defers the execution of a task within the workflow execution context.
    /// Deferred tasks are executed right after bookmarks have been persisted.
    /// </summary>
    public static void DeferTask(this WorkflowExecutionContext context, Func<Task> task)
    {
        var deferredTasks = context.GetDeferredTasksInternal();
        deferredTasks.Add(task);
    }
    
    /// <summary>
    /// Defers the execution of a task within the workflow execution context.
    /// Deferred tasks are executed right after bookmarks have been persisted.
    /// </summary>
    public static void DeferTask(this ActivityExecutionContext context, Action task)
    {
        context.DeferTask(() =>
        {
            task();
            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Defers the execution of a task within the workflow execution context.
    /// Deferred tasks are executed right after bookmarks have been persisted.
    /// </summary>
    public static void DeferTask(this ActivityExecutionContext context, Func<Task> task)
    {
        context.WorkflowExecutionContext.DeferTask(task);
    }

    /// <summary>
    /// Executes all deferred tasks within the workflow execution context.
    /// </summary>
    public static async Task ExecuteDeferredTasksAsync(this WorkflowExecutionContext context)
    {
        var deferredTasks = context.GetDeferredTasksInternal();
        var tasks = deferredTasks.Select(x => x()).ToList();
        await Task.WhenAll(tasks);
    }

    private static ICollection<Func<Task>> GetDeferredTasksInternal(this WorkflowExecutionContext context)
    {
        return context.TransientProperties.GetOrAdd(DeferredTasksKey, () => new List<Func<Task>>());
    }
}