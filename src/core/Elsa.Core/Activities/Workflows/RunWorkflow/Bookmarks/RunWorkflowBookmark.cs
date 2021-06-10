using System.Collections.Generic;
using Elsa.Services;
using Elsa.Services.Bookmarks;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Workflows
{
    public class RunWorkflowBookmark : IBookmark
    {
        public string ChildWorkflowInstanceId { get; set; } = default!;
    }

    public class RunWorkflowBookmarkProvider : BookmarkProvider<RunWorkflowBookmark, RunWorkflow>
    {
        public override IEnumerable<BookmarkResult> GetBookmarks(BookmarkProviderContext<RunWorkflow> context)
        {
            var childWorkflowInstanceId = context.GetActivity<RunWorkflow>().GetPropertyValue(x => x.ChildWorkflowInstanceId);

            if (string.IsNullOrWhiteSpace(childWorkflowInstanceId))
                yield break;

            yield return new BookmarkResult(new RunWorkflowBookmark
            {
                ChildWorkflowInstanceId = childWorkflowInstanceId!
            });
        }
    }
}