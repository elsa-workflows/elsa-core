using Elsa.Alterations.AlterationTypes;
using Elsa.Alterations.Core.Abstractions;
using Elsa.Alterations.Core.Contexts;
using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Alterations.AlterationHandlers;

/// <summary>
/// Schedules an activity for execution.
/// </summary>
public class CancelActivityHandler : AlterationHandlerBase<CancelActivity>
{
    private readonly IBookmarkManager _bookmarkManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="CancelActivityHandler"/> class.
    /// </summary>
    public CancelActivityHandler(IBookmarkManager bookmarkManager)
    {
        _bookmarkManager = bookmarkManager;
    }

    /// <inheritdoc />
    protected override async ValueTask HandleAsync(AlterationHandlerContext context, CancelActivity alteration)
    {
        if (alteration.ActivityInstanceId == null && alteration.ActivityId == null)
        {
            context.Fail("Either ActivityInstanceId or ActivityId must be specified");
            return;
        }

        var activityExecutionContext = GetActivityExecutionContext(context, alteration);

        if (activityExecutionContext == null)
        {
            context.Fail(
                alteration.ActivityInstanceId != null
                    ? $"Activity execution context with ID {alteration.ActivityInstanceId} not found"
                    : $"Activity execution context for activity with ID {alteration.ActivityId} not found");

            return;
        }

        await activityExecutionContext.CancelActivityAsync();
        var cancellationToken = context.CancellationToken;
        context.Succeed(() => RemoveBookmarksAsync(activityExecutionContext.Id, cancellationToken));
    }

    private async Task RemoveBookmarksAsync(string activityInstanceId, CancellationToken cancellationToken)
    {
        var filter = new BookmarkFilter { ActivityInstanceId = activityInstanceId };
        await _bookmarkManager.DeleteManyAsync(filter, cancellationToken);
    }

    private static ActivityExecutionContext? GetActivityExecutionContext(AlterationHandlerContext context, CancelActivity alteration)
    {
        var workflowExecutionContext = context.WorkflowExecutionContext;

        if (alteration.ActivityInstanceId != null)
            return workflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.Id == alteration.ActivityInstanceId);

        if (alteration.ActivityId != null)
            return workflowExecutionContext.ActiveActivityExecutionContexts.FirstOrDefault(x => x.Activity.Id == alteration.ActivityId);

        return null;
    }
}