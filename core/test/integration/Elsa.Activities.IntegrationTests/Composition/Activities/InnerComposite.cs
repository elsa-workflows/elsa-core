using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Activities.IntegrationTests.Composition.Activities;

/// <summary>
/// Inner composite activity that uses Complete.
/// </summary>
public class InnerComposite : Composite
{
    private readonly WriteLine _completedMessage = new("Inner composite completed");

    public InnerComposite()
    {
        Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Inner composite started"),
                new Complete(),
                new WriteLine("This should not execute in inner"),
                _completedMessage
            }
        };
    }

    protected override async ValueTask OnCompletedAsync(ActivityCompletedContext context)
    {
        // Schedule the completion message
        await context.TargetContext.ScheduleActivityAsync(_completedMessage);
    }
}