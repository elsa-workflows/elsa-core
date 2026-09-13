using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Activities.IntegrationTests;

public class ParallelForEachTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture = new(TestContext.Current!.Output.StandardOutput);
    private const string CurrentValueVar = "CurrentValue";
    private static string[] ThreeItems => ["a", "b", "c"];

    private CapturingTextWriter CapturingTextWriter => _fixture.CapturingTextWriter;

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Test]
    [DisplayName("ParallelForEach executes body for all items ($items)")]
    [MethodDataSource(nameof(ItemTestCases))]
    public async Task ParallelForEach_ExecutesBody_ForAllItems(string[] items)
    {
        await ExecuteAndAssertAllItems(items);
    }

    [Test]
    [DisplayName("ParallelForEach completes when collection is empty")]
    public async Task ParallelForEach_Completes_WhenCollectionEmpty()
    {
        await ExecuteAndAssertStatus([], ActivityStatus.Completed);
        await Assert.That(CapturingTextWriter.Lines).IsEmpty();

    }

    [Test]
    [DisplayName("ParallelForEach completes when collection is null")]
    public async Task ParallelForEach_Completes_WhenCollectionNull()
    {
        await ExecuteAndAssertStatus(null, ActivityStatus.Completed);
    }

    [Test]
    [DisplayName("ParallelForEach executes all items when one faults")]
    public async Task ParallelForEach_ExecutesAllItems_WhenOneFaults()
    {
        var body = new Sequence
        {
            Activities =
            [
                new If(context => context.GetVariable<string>(CurrentValueVar) == "b")
                {
                    Then = new Fault { Message = new("Faulted") },
                },
                WriteCurrentValue()
            ]
        };

        await RunActivityAsync(ThreeItems, body);

        await Assert.That(CapturingTextWriter.Lines).Contains("a");

        await Assert.That(CapturingTextWriter.Lines).DoesNotContain("b");

        await Assert.That(CapturingTextWriter.Lines).Contains("c");

    }

    [Test]
    [DisplayName("ParallelForEach executes body for different item types")]
    public async Task ParallelForEach_ExecutesBody_ForDifferentItemTypes()
    {
        var items = new object?[]
        {
            "a", 2, null, new Foo()
        };
        var parallelForEach = new ParallelForEach<object?>(items)
        {
            Body = new WriteLine(context => context.GetVariable<object>(CurrentValueVar)?.ToString() ?? "")
        };

        await _fixture.RunActivityAsync(parallelForEach);

        await Assert.That(CapturingTextWriter.Lines.Count).IsEqualTo(items.Length);

        await Assert.That(CapturingTextWriter.Lines).Contains("a");

        await Assert.That(CapturingTextWriter.Lines).Contains("2");

        await Assert.That(CapturingTextWriter.Lines).Contains("");

        await Assert.That(CapturingTextWriter.Lines).Contains("Baz");

    }

    [Test]
    [DisplayName("ParallelForEach provides CurrentIndex variable")]
    public async Task ParallelForEach_ProvidesCurrentIndex_ForEachIteration()
    {
        var body = new WriteLine(context => context.GetVariable<int>("CurrentIndex").ToString());

        await RunActivityAsync(ThreeItems, body);

        await Assert.That(CapturingTextWriter.Lines.Count).IsEqualTo(ThreeItems.Length);

        await Assert.That(CapturingTextWriter.Lines).Contains("0");

        await Assert.That(CapturingTextWriter.Lines).Contains("1");

        await Assert.That(CapturingTextWriter.Lines).Contains("2");

    }

    [Test]
    [DisplayName("ParallelForEach completes when all bodies complete")]
    public async Task ParallelForEach_Completes_WhenAllBodiesComplete()
    {
        await ExecuteAndAssertStatus(ThreeItems, ActivityStatus.Completed);
    }

    public static IEnumerable<Func<string[]>> ItemTestCases =>
    [
        () => ["a", "b", "c"],
        () => ["single"]
    ];

    private async Task ExecuteAndAssertAllItems(string[] items)
    {
        await RunActivityAsync(items, WriteCurrentValue());

        await Assert.That(CapturingTextWriter.Lines.Count).IsEqualTo(items.Length);

        foreach (var item in items)
            await Assert.That(CapturingTextWriter.Lines).Contains(item);
    }

    private async Task ExecuteAndAssertStatus(string[]? items, ActivityStatus expectedStatus)
    {
        var result = await RunActivityAsync(items, WriteCurrentValue());
        var context = await GetParallelForEachContext(result);
        await Assert.That(context.Status).IsEqualTo(expectedStatus);

    }

    private async Task<RunWorkflowResult> RunActivityAsync(string[]? items, IActivity body)
    {
        var parallelForEach = new ParallelForEach<string>(items!) { Body = body };
        return await _fixture.RunActivityAsync(parallelForEach);
    }

    private static WriteLine WriteCurrentValue() => new(context => context.GetVariable<string>(CurrentValueVar));

    private static async Task<ActivityExecutionContext> GetParallelForEachContext(RunWorkflowResult result)
    {
        var context = result.Journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity is ParallelForEach<string>);
        await Assert.That(context).IsNotNull();

        return context!;
    }
}
