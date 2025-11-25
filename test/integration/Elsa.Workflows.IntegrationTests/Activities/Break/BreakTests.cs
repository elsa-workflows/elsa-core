using Elsa.Testing.Shared;
using Elsa.Workflows.IntegrationTests.Activities.Workflows;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Activities;

/// <summary>
/// Integration tests for the <see cref="Workflows.Activities.Break"/> activity.
/// Tests Break behavior across different looping constructs (ForEach, For, While, Fork).
/// </summary>
public class BreakTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);

    [Fact(DisplayName = "Break exits ForEach loop")]
    public async Task Break_ExitsForEachLoop()
    {
        // Act
        var result = await _fixture.RunWorkflowAsync(new BreakForEachWorkflow());
        var lines = _fixture.CapturingTextWriter.Lines.ToList();

        // Assert
        Assert.Equal(new[] { "Start", "C#", "End" }, lines);
    }

    [Fact(DisplayName = "Break exits only immediate loop in nested ForEach")]
    public async Task Break_ExitsOnlyImmediateLoopInNestedForEach()
    {
        // Act
        var result = await _fixture.RunWorkflowAsync(new NestedForEachWithBreakWorkflow());
        var lines = _fixture.CapturingTextWriter.Lines.ToList();

        // Assert
        Assert.Equal(new[] { "C#", "Classes", "Rust", "Classes", "Go", "Classes" }, lines);
    }

    [Fact(DisplayName = "Break exits For loop")]
    public async Task Break_ExitsForLoop()
    {
        // Act
        var result = await _fixture.RunWorkflowAsync(new BreakForWorkflow());
        var lines = _fixture.CapturingTextWriter.Lines.ToList();

        // Assert
        Assert.Equal(new[] { "Start", "0", "1", "End" }, lines);
    }

    [Fact(DisplayName = "Break exits While loop")]
    public async Task Break_ExitsWhileLoop()
    {
        // Act
        var result = await _fixture.RunWorkflowAsync(new BreakWhileWorkflow());
        var lines = _fixture.CapturingTextWriter.Lines.ToList();

        // Assert
        Assert.Equal(new[] { "Start", "1", "2", "End" }, lines);
    }
}
