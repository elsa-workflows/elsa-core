using Elsa.Workflows;

namespace Elsa.Samples.ConsoleApp.Bookmarks.Activities;

public class MyEvent : Activity
{
    protected override void Execute(ActivityExecutionContext context)
    {
        context.CreateBookmark();
    }
}