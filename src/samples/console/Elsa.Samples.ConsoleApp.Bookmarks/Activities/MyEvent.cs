using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Models;

namespace Elsa.Samples.ConsoleApp.Bookmarks.Activities;

public class MyEvent : Activity
{
    protected override void Execute(ActivityExecutionContext context)
    {
        context.CreateBookmark();
    }
}