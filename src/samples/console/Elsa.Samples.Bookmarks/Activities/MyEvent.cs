using Elsa.Workflows.Core.Models;

namespace Elsa.Samples.Bookmarks.Activities;

public class MyEvent : Activity
{
    protected override void Execute(ActivityExecutionContext context)
    {
        context.CreateBookmark();
    }
}