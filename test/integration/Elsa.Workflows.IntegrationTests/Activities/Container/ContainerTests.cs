using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Activities.Container;

/// <summary>
/// Integration tests for the <see cref="Container"/> activity.
/// </summary>
public class ContainerTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture = new(TestContext.Current!.Output.StandardOutput);

    [Test]
    [DisplayName("Container executes child activities in order")]
    public async Task Container_ExecutesChildActivitiesInOrder()
    {
        // Arrange & Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new SimpleContainerWorkflow());

        // Assert
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(lines).IsEquivalentTo(new[] { "Activity 1", "Activity 2", "Activity 3" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("Container with variables makes them available to child activities")]
    public async Task Container_WithVariables_MakesThemAvailableToChildren()
    {
        // Arrange & Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new ContainerWithVariablesWorkflow());

        // Assert
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(lines[0]).Contains("Counter: 0", StringComparison.CurrentCulture);
        await Assert.That(lines[1]).Contains("Counter: 1", StringComparison.CurrentCulture);
        await Assert.That(lines[2]).Contains("Counter: 2", StringComparison.CurrentCulture);
    }

    [Test]
    [DisplayName("Container with nested containers scopes variables correctly")]
    public async Task Container_WithNestedContainers_ScopesVariablesCorrectly()
    {
        // Arrange & Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new NestedContainersWorkflow());

        // Assert
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(lines[0]).Contains("Outer: 10", StringComparison.CurrentCulture);
        await Assert.That(lines[1]).Contains("Inner: 20", StringComparison.CurrentCulture);
        await Assert.That(lines[2]).Contains("Outer again: 10", StringComparison.CurrentCulture);
    }

    [Test]
    [DisplayName("Container with unnamed variables auto-names them")]
    public async Task Container_WithUnnamedVariables_AutoNamesThem()
    {
        // Arrange & Act
        var (result, _) = await RunWorkflowAndCaptureOutput(new ContainerWithUnnamedVariablesWorkflow());

        // Assert
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        // The workflow should complete successfully even with unnamed variables
    }

    [Test]
    [DisplayName("Container with no activities completes successfully")]
    public async Task Container_WithNoActivities_CompletesSuccessfully()
    {
        // Arrange & Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new EmptyContainerWorkflow());

        // Assert
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(lines).IsEmpty();
    }

    [Test]
    [DisplayName("Container with multiple variable types handles them correctly")]
    public async Task Container_WithMultipleVariableTypes_HandlesThemCorrectly()
    {
        // Arrange & Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new ContainerWithMixedVariableTypesWorkflow());

        // Assert
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(lines[0]).Contains("Int: 42", StringComparison.CurrentCulture);
        await Assert.That(lines[1]).Contains("String: Hello", StringComparison.CurrentCulture);
        await Assert.That(lines[2]).Contains("Bool: True", StringComparison.CurrentCulture);
    }

    [Test]
    [DisplayName("Container handles different numbers of child activities: $activityCount")]
    [Arguments(1)]
    [Arguments(5)]
    [Arguments(10)]
    public async Task Container_HandlesDifferentNumbersOfChildActivities(int activityCount)
    {
        // Arrange & Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new DynamicContainerWorkflow(activityCount));

        // Assert
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(lines.Count).IsEqualTo(activityCount);
        for (var i = 0; i < activityCount; i++)
        {
            await Assert.That(lines[i]).IsEqualTo($"Activity {i + 1}");
        }
    }
    
    private async Task<(RunWorkflowResult Result, List<string> Lines)> RunWorkflowAndCaptureOutput(IWorkflow workflow)
    {
        var result = await _fixture.RunWorkflowAsync(workflow);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        return (result, lines);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_fixture);
}
