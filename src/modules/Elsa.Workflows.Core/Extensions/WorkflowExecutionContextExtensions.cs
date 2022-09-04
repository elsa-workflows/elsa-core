using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core;

public static class WorkflowExecutionContextExtensions
{
    /// <summary>
    /// Remove the specified set of <see cref="ActivityExecutionContext"/> from the workflow execution context.
    /// </summary>
    public static void RemoveActivityExecutionContexts(this WorkflowExecutionContext workflowExecutionContext, IEnumerable<ActivityExecutionContext> contexts)
    {
        // Copy each item into a new list to avoid changing the source enumerable while removing elements from it.
        var list = contexts.ToList();

        // Remove each context.
        foreach (var context in list) workflowExecutionContext.ActivityExecutionContexts.Remove(context);
    }

    /// <summary>
    /// Schedules the root activity of the workflow.
    /// </summary>
    public static void ScheduleRoot(this WorkflowExecutionContext workflowExecutionContext)
    {
        var activityInvoker = workflowExecutionContext.GetRequiredService<IActivityInvoker>();
        var workflow = workflowExecutionContext.Workflow;
        var workItem = new ActivityWorkItem(workflow.Root.Id, async () => await activityInvoker.InvokeAsync(workflowExecutionContext, workflow.Root));
        workflowExecutionContext.Scheduler.Push(workItem);
    }

    /// <summary>
    /// Schedules the activity of the specified bookmark.
    /// </summary>
    public static void ScheduleBookmark(this WorkflowExecutionContext workflowExecutionContext, Bookmark bookmark)
    {
        // Construct bookmark.
        var bookmarkedActivityContext = workflowExecutionContext.ActivityExecutionContexts.First(x => x.Id == bookmark.ActivityInstanceId);
        var bookmarkedActivity = bookmarkedActivityContext.Activity;

        // Schedule the activity to resume.
        var activityInvoker = workflowExecutionContext.GetRequiredService<IActivityInvoker>();
        var workItem = new ActivityWorkItem(bookmarkedActivity.Id, async () => await activityInvoker.InvokeAsync(bookmarkedActivityContext));
        workflowExecutionContext.Scheduler.Push(workItem);

        // If no resumption point was specified, use Noop to prevent the regular "ExecuteAsync" method to be invoked.
        workflowExecutionContext.ExecuteDelegate = bookmark.CallbackMethodName != null ? bookmarkedActivity.GetResumeActivityDelegate(bookmark.CallbackMethodName) : WorkflowExecutionContext.Complete;
        
        // Remove the bookmark.
        workflowExecutionContext.Bookmarks.Remove(bookmark);
    }

    /// <summary>
    /// Schedules the specified activity.
    /// </summary>
    public static void Schedule(
        this WorkflowExecutionContext workflowExecutionContext,
        IActivity activity,
        ActivityExecutionContext owner,
        ActivityCompletionCallback? completionCallback = default,
        IEnumerable<MemoryBlockReference>? references = default, object? tag = default)
    {
        var activityInvoker = workflowExecutionContext.GetRequiredService<IActivityInvoker>();
        var workItem = new ActivityWorkItem(activity.Id, async () => await activityInvoker.InvokeAsync(workflowExecutionContext, activity, owner, references), tag);
        workflowExecutionContext.Scheduler.Push(workItem);

        if (completionCallback != null)
            workflowExecutionContext.AddCompletionCallback(owner, activity, completionCallback);
    }

    /// <summary>
    /// Gets the specified workflow variable by name. 
    /// </summary>
    public static T? GetVariable<T>(this WorkflowExecutionContext workflowExecutionContext, string name) => (T?)workflowExecutionContext.GetVariable(name);

    /// <summary>
    /// Gets the specified workflow variable by name, where the name is implied by the type name. 
    /// </summary>
    public static T? GetVariable<T>(this WorkflowExecutionContext workflowExecutionContext) => (T?)workflowExecutionContext.GetVariable(typeof(T).Name);

    /// <summary>
    /// Gets the specified workflow variable by name. 
    /// </summary>
    public static object? GetVariable(this WorkflowExecutionContext workflowExecutionContext, string name)
    {
        var variable = workflowExecutionContext.Workflow.Variables.FirstOrDefault(x => x.Name == name);
        return variable?.Get(workflowExecutionContext.MemoryRegister);
    }

    /// <summary>
    /// Sets the specified workflow variable by name, where the name is implied by the type name. 
    /// </summary>
    public static Variable SetVariable<T>(this WorkflowExecutionContext workflowExecutionContext, T? value) => workflowExecutionContext.SetVariable(typeof(T).Name, value);

    /// <summary>
    /// Sets the specified workflow variable by name. 
    /// </summary>
    public static Variable SetVariable<T>(this WorkflowExecutionContext workflowExecutionContext, string name, T? value) => workflowExecutionContext.SetVariable(name, (object?)value);

    /// <summary>
    /// Sets the specified workflow variable by name. 
    /// </summary>
    public static Variable SetVariable(this WorkflowExecutionContext workflowExecutionContext, string name, object? value)
    {
        var variable = workflowExecutionContext.Workflow.Variables.FirstOrDefault(x => x.Name == name);

        if (variable == null)
        {
            variable = new Variable(name, value);
            workflowExecutionContext.Workflow.Variables.Add(variable);
        }

        variable.Set(workflowExecutionContext.MemoryRegister, value);
        return variable;
    }
}