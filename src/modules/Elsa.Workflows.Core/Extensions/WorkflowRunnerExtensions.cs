using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Contains extension methods for <see cref="IWorkflowRunner"/>.
/// </summary>
[PublicAPI]
public static class WorkflowRunnerExtensions
{
    /// <summary>
    /// Runs a workflow until its end, automatically resuming any bookmark it encounters.
    /// </summary>
    public static async Task<WorkflowState> RunUntilEndAsync(
        this IWorkflowRunner workflowRunner, 
        IActivity activity,
        Func<WorkflowState, Bookmark, RunWorkflowOptions, bool>? processBookmark = default,
        CancellationToken cancellationToken = default)
    {
        var result = await workflowRunner.RunAsync(activity, cancellationToken: cancellationToken);
        var workflow = result.Workflow;
        var workflowState = result.WorkflowState;
        var bookmarks = new Stack<Bookmark>(workflowState.Bookmarks);

        // Continue resuming the workflow for as long as there are bookmarks to resume.
        while (bookmarks.TryPop(out var bookmark))
        {
            var options = new RunWorkflowOptions(bookmarkId: bookmark.Id);
            
            // Give caller a chance to determine whether or not to resume the bookmark, as well as configure any input to provide.
            if (processBookmark != null && !processBookmark(workflowState, bookmark, options))
                continue;
            
            result = await workflowRunner.RunAsync(workflow, workflowState, options, cancellationToken: cancellationToken);
            workflowState = result.WorkflowState;

            foreach (var newBookmark in workflowState.Bookmarks)
                bookmarks.Push(newBookmark);
        }

        // Return the workflow state.
        return workflowState;
    }
}