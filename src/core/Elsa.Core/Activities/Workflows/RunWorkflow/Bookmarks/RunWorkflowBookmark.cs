using System.Collections.Generic;
using Elsa.Bookmarks;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Workflows
{
    public class RunWorkflowBookmark : IBookmark
    {
        public string ChildWorkflowInstanceId { get; set; } = default!;
    }
    
    public class RunWorkflowBookmarkProvider : BookmarkProvider<RunWorkflowBookmark, RunWorkflow>
    {
        public override IEnumerable<IBookmark> GetTriggers(BookmarkProviderContext<RunWorkflow> context)
        {
            var childWorkflowInstanceId = context.GetActivity<RunWorkflow>().GetState(x => x.ChildWorkflowInstanceId);

            if (string.IsNullOrWhiteSpace(childWorkflowInstanceId))
                yield break;
            
            yield return new RunWorkflowBookmark
            {
                ChildWorkflowInstanceId = childWorkflowInstanceId!
            };
        }
    }
}