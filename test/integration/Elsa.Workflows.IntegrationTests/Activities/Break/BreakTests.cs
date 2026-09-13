using Elsa.Testing.Shared;
using Elsa.Workflows.IntegrationTests.Activities.Workflows;

namespace Elsa.Workflows.IntegrationTests.Activities;

/// <summary>
/// Integration tests for the <see cref="Workflows.Activities.Break"/> activity.
/// Tests Break behavior across different looping constructs (ForEach, For, While, Fork).
/// </summary>
public class BreakTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture = new(TestContext.Current!.Output.StandardOutput);

    [Test]
    [DisplayName("Break exits ForEach loop")]
    public async Task Break_ExitsForEachLoop()
    {
        // Act
        await _fixture.RunWorkflowAsync(new BreakForEachWorkflow());
        var lines = _fixture.CapturingTextWriter.Lines.ToList();

        // Assert
        await Assert.That(lines).IsEquivalentTo(new[] { "Start", "C#", "End" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("Break exits only immediate loop in nested ForEach")]
    public async Task Break_ExitsOnlyImmediateLoopInNestedForEach()
    {
        // Act
        await _fixture.RunWorkflowAsync(new NestedForEachWithBreakWorkflow());
        var lines = _fixture.CapturingTextWriter.Lines.ToList();

        // Assert
        await Assert.That(lines).IsEquivalentTo(new[] { "C#", "Classes", "Rust", "Classes", "Go", "Classes" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("Break exits For loop")]
    public async Task Break_ExitsForLoop()
    {
        // Act
        await _fixture.RunWorkflowAsync(new BreakForWorkflow());
        var lines = _fixture.CapturingTextWriter.Lines.ToList();

        // Assert
        await Assert.That(lines).IsEquivalentTo(new[] { "Start", "0", "1", "End" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("Break exits While loop")]
    public async Task Break_ExitsWhileLoop()
    {
        // Act
        await _fixture.RunWorkflowAsync(new BreakWhileWorkflow());
        var lines = _fixture.CapturingTextWriter.Lines.ToList();

        // Assert
        await Assert.That(lines).IsEquivalentTo(new[] { "Start", "1", "2", "End" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_fixture);
}
