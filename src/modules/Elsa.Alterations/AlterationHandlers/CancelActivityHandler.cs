using Elsa.Alterations.AlterationTypes;
using Elsa.Alterations.Core.Abstractions;
using Elsa.Alterations.Core.Contexts;
using Elsa.Workflows.Core;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Alterations.AlterationHandlers;

/// <summary>
/// Cancels an activity.
/// </summary>
public class CancelActivityHandler : AlterationHandlerBase<CancelActivity>
{
    private readonly IBookmarkManager _bookmarkManager;
    private readonly IActivityExecutionManager _activityExecutionManager;
    private readonly IActivityExecutionMapper _activityExecutionMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="CancelActivityHandler"/> class.
    /// </summary>
    public CancelActivityHandler(IBookmarkManager bookmarkManager, IActivityExecutionManager activityExecutionManager, IActivityExecutionMapper activityExecutionMapper)
    {
        _bookmarkManager = bookmarkManager;
        _activityExecutionManager = activityExecutionManager;
        _activityExecutionMapper = activityExecutionMapper;
    }

    /// <inheritdoc />
    protected override ValueTask HandleAsync(AlterationContext context, CancelActivity alteration)
    {
        if (alteration.ActivityInstanceId == null && alteration.ActivityId == null)
        {
            context.Fail("Either ActivityInstanceId or ActivityId must be specified");
            return ValueTask.CompletedTask;
        }

        var activityExecutionContexts = GetActivityExecutionContexts(context, alteration).ToList();

        if (!activityExecutionContexts.Any())
        {
            context.Fail(
                alteration.ActivityInstanceId != null
                    ? $"Activity execution context with ID {alteration.ActivityInstanceId} not found"
                    : $"Activity execution contexts for activity with ID {alteration.ActivityId} not found");

            return ValueTask.CompletedTask;
        }

        context.Succeed(() => CleanupAsync(activityExecutionContexts));
        return ValueTask.CompletedTask;
    }

    private async Task CleanupAsync(IEnumerable<ActivityExecutionContext> activityExecutionContexts)
    {
        foreach (var activityExecutionContext in activityExecutionContexts) 
            await CleanupAsync(activityExecutionContext);
    }
    
    private async Task CleanupAsync(ActivityExecutionContext activityExecutionContext)
    {
        await RemoveBookmarksAsync(activityExecutionContext);
        await SaveActivityExecutionRecordAsync(activityExecutionContext);
    }

    private async Task RemoveBookmarksAsync(ActivityExecutionContext activityExecutionContext)
    {
        var filter = new BookmarkFilter { ActivityInstanceId = activityExecutionContext.Id };
        await _bookmarkManager.DeleteManyAsync(filter, activityExecutionContext.CancellationToken);
    }

    private async Task SaveActivityExecutionRecordAsync(ActivityExecutionContext activityExecutionContext)
    {
        var activityExecutionRecord = _activityExecutionMapper.Map(activityExecutionContext);
        await _activityExecutionManager.SaveAsync(activityExecutionRecord, CancellationToken.None);
    }

    private static IEnumerable<ActivityExecutionContext> GetActivityExecutionContexts(AlterationContext context, CancelActivity alteration)
    {
        var workflowExecutionContext = context.WorkflowExecutionContext;

        return alteration.ActivityInstanceId != null
            ? workflowExecutionContext.ActivityExecutionContexts.Where(x => x.Id == alteration.ActivityInstanceId)
            : alteration.ActivityId != null
                ? workflowExecutionContext.ActivityExecutionContexts.Where(x => x.Activity.Id == alteration.ActivityId)
                : Enumerable.Empty<ActivityExecutionContext>();
    }
}