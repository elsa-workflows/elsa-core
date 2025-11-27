using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests;

public class ParallelForEachTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);
    private const string CurrentValueVar = "CurrentValue";
    private static readonly string[] ThreeItems = ["a", "b", "c"];

    private CapturingTextWriter CapturingTextWriter => _fixture.CapturingTextWriter;

    [Theory(DisplayName = "ParallelForEach executes body for all items")]
    [MemberData(nameof(ItemTestCases))]
    public async Task ParallelForEach_ExecutesBody_ForAllItems(string[] items)
    {
        await ExecuteAndAssertAllItems(items);
    }

    [Fact(DisplayName = "ParallelForEach completes when collection is empty")]
    public async Task ParallelForEach_Completes_WhenCollectionEmpty()
    {
        await ExecuteAndAssertStatus([], ActivityStatus.Completed);
        Assert.Empty(CapturingTextWriter.Lines);
    }

    [Fact(DisplayName = "ParallelForEach completes when collection is null")]
    public async Task ParallelForEach_Completes_WhenCollectionNull()
    {
        await ExecuteAndAssertStatus(null, ActivityStatus.Completed);
    }

    [Fact(DisplayName = "ParallelForEach executes all items when one faults")]
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

        Assert.Contains("a", CapturingTextWriter.Lines);
        Assert.DoesNotContain("b", CapturingTextWriter.Lines);
        Assert.Contains("c", CapturingTextWriter.Lines);
    }

    [Fact(DisplayName = "ParallelForEach executes body for different item types")]
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

        Assert.Equal(items.Length, CapturingTextWriter.Lines.Count);
        Assert.Contains("a", CapturingTextWriter.Lines);
        Assert.Contains("2", CapturingTextWriter.Lines);
        Assert.Contains("", CapturingTextWriter.Lines);
        Assert.Contains("Baz", CapturingTextWriter.Lines);
    }

    [Fact(DisplayName = "ParallelForEach provides CurrentIndex variable")]
    public async Task ParallelForEach_ProvidesCurrentIndex_ForEachIteration()
    {
        var body = new WriteLine(context => context.GetVariable<int>("CurrentIndex").ToString());

        await RunActivityAsync(ThreeItems, body);

        Assert.Equal(ThreeItems.Length, CapturingTextWriter.Lines.Count);
        Assert.Contains("0", CapturingTextWriter.Lines);
        Assert.Contains("1", CapturingTextWriter.Lines);
        Assert.Contains("2", CapturingTextWriter.Lines);
    }

    [Fact(DisplayName = "ParallelForEach completes when all bodies complete")]
    public async Task ParallelForEach_Completes_WhenAllBodiesComplete()
    {
        await ExecuteAndAssertStatus(ThreeItems, ActivityStatus.Completed);
    }

    public static TheoryData<string[]> ItemTestCases =>
    [
        ThreeItems,
        ["single"]
    ];

    private async Task ExecuteAndAssertAllItems(string[] items)
    {
        await RunActivityAsync(items, WriteCurrentValue());

        Assert.Equal(items.Length, CapturingTextWriter.Lines.Count);
        Assert.All(items, item => Assert.Contains(item, CapturingTextWriter.Lines));
    }

    private async Task ExecuteAndAssertStatus(string[]? items, ActivityStatus expectedStatus)
    {
        var result = await RunActivityAsync(items, WriteCurrentValue());
        var context = GetParallelForEachContext(result);
        Assert.Equal(expectedStatus, context.Status);
    }

    private async Task<RunWorkflowResult> RunActivityAsync(string[]? items, IActivity body)
    {
        var parallelForEach = new ParallelForEach<string>(items!) { Body = body };
        return await _fixture.RunActivityAsync(parallelForEach);
    }

    private static WriteLine WriteCurrentValue() => new(context => context.GetVariable<string>(CurrentValueVar));

    private static ActivityExecutionContext GetParallelForEachContext(RunWorkflowResult result)
    {
        var context = result.Journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity is ParallelForEach<string>);
        Assert.NotNull(context);
        return context;
    }
}