using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ProjectedEnumerableToArray;

/// <summary>
/// A test activity that produces an IEnumerable&lt;string&gt; via Select projection, which has two generic arguments and was triggering the bug in ConvertIEnumerableToArray.
/// </summary>
public class TestEnumerableActivity : Activity
{
    public Output<object?> EnumerableResult { get; set; } = new();

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Create a list of items
        var items = Enumerable
            .Range(1, 5)
            .Select(x => new TestOutputItem())
            .ToList();

        // Use Select to project to strings - this creates a ListSelectIterator<TestOutputItem, string>
        // which has TWO generic arguments, triggering the bug
        var messages = items.Select(e => $"Name: {e.Name} ID: {e.Id}");

        context.Set(EnumerableResult, messages);
        return context.CompleteActivityWithOutcomesAsync("Done");
    }
}