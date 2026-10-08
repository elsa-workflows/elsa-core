using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests;

public class NestedLoopMemoryBlockTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);
    private const string CurrentValue = "CurrentValue";
    private const string CurrentIndex = "CurrentIndex";

    [Fact(DisplayName = "Nested ParallelForEach does not overwrite the parent ForEach CurrentValue or CurrentIndex")]
    public async Task NestedParallelForEach_PreservesParentForEachCurrentValueAndIndex()
    {
        var outerItems = new[] { "outer-a", "outer-b" };
        var innerItems = new[] { "inner-1", "inner-2", "inner-3" };
        var forEach = new ForEach<string>(outerItems)
        {
            Body = new Sequence
            {
                Activities =
                {
                    new ParallelForEach<string>(innerItems)
                    {
                        Body = WriteLoopState("inner")
                    },
                    WriteLoopState("outer")
                }
            }
        };

        await _fixture.RunActivityAsync(forEach);

        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        Assert.Contains("inner:inner-1:0", lines);
        Assert.Contains("inner:inner-2:1", lines);
        Assert.Contains("inner:inner-3:2", lines);
        Assert.Contains("outer:outer-a:0", lines);
        Assert.Contains("outer:outer-b:1", lines);
        Assert.DoesNotContain("outer:inner-3:2", lines);
        Assert.DoesNotContain("outer:inner-1:0", lines);
    }

    [Fact(DisplayName = "Nested ForEach does not overwrite the parent ForEach CurrentValue or CurrentIndex")]
    public async Task NestedForEach_PreservesParentForEachCurrentValueAndIndex()
    {
        var outerItems = new[] { "outer-a", "outer-b" };
        var innerItems = new[] { "inner-1", "inner-2" };
        var forEach = new ForEach<string>(outerItems)
        {
            Body = new Sequence
            {
                Activities =
                {
                    new ForEach<string>(innerItems)
                    {
                        Body = WriteLoopState("inner")
                    },
                    WriteLoopState("outer")
                }
            }
        };

        await _fixture.RunActivityAsync(forEach);

        Assert.Equal(
            [
                "inner:inner-1:0",
                "inner:inner-2:1",
                "outer:outer-a:0",
                "inner:inner-1:0",
                "inner:inner-2:1",
                "outer:outer-b:1"
            ],
            _fixture.CapturingTextWriter.Lines);
    }

    private static WriteLine WriteLoopState(string scope) =>
        new(context => $"{scope}:{context.GetVariable<string>(CurrentValue)}:{context.GetVariable<int>(CurrentIndex)}");
}
