using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Activities.Container;

/// <summary>
/// Integration tests for the <see cref="Container"/> activity.
/// </summary>
public class ContainerTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);

    [Fact(DisplayName = "Container executes child activities in order")]
    public async Task Container_ExecutesChildActivitiesInOrder()
    {
        // Arrange & Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new SimpleContainerWorkflow());

        // Assert
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Equal(new[] { "Activity 1", "Activity 2", "Activity 3" }, lines);
    }

    [Fact(DisplayName = "Container with variables makes them available to child activities")]
    public async Task Container_WithVariables_MakesThemAvailableToChildren()
    {
        // Arrange & Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new ContainerWithVariablesWorkflow());

        // Assert
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Contains("Counter: 0", lines[0]);
        Assert.Contains("Counter: 1", lines[1]);
        Assert.Contains("Counter: 2", lines[2]);
    }

    [Fact(DisplayName = "Container with nested containers scopes variables correctly")]
    public async Task Container_WithNestedContainers_ScopesVariablesCorrectly()
    {
        // Arrange & Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new NestedContainersWorkflow());

        // Assert
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Contains("Outer: 10", lines[0]);
        Assert.Contains("Inner: 20", lines[1]);
        Assert.Contains("Outer again: 10", lines[2]);
    }

    [Fact(DisplayName = "Container with unnamed variables auto-names them")]
    public async Task Container_WithUnnamedVariables_AutoNamesThem()
    {
        // Arrange & Act
        var (result, _) = await RunWorkflowAndCaptureOutput(new ContainerWithUnnamedVariablesWorkflow());

        // Assert
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        // The workflow should complete successfully even with unnamed variables
    }

    [Fact(DisplayName = "Container with no activities completes successfully")]
    public async Task Container_WithNoActivities_CompletesSuccessfully()
    {
        // Arrange & Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new EmptyContainerWorkflow());

        // Assert
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Empty(lines);
    }

    [Fact(DisplayName = "Container with multiple variable types handles them correctly")]
    public async Task Container_WithMultipleVariableTypes_HandlesThemCorrectly()
    {
        // Arrange & Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new ContainerWithMixedVariableTypesWorkflow());

        // Assert
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Contains("Int: 42", lines[0]);
        Assert.Contains("String: Hello", lines[1]);
        Assert.Contains("Bool: True", lines[2]);
    }

    [Theory(DisplayName = "Container handles different numbers of child activities")]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task Container_HandlesDifferentNumbersOfChildActivities(int activityCount)
    {
        // Arrange & Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new DynamicContainerWorkflow(activityCount));

        // Assert
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Equal(activityCount, lines.Count);
        for (var i = 0; i < activityCount; i++)
        {
            Assert.Equal($"Activity {i + 1}", lines[i]);
        }
    }
    
    private async Task<(RunWorkflowResult Result, List<string> Lines)> RunWorkflowAndCaptureOutput(IWorkflow workflow)
    {
        var result = await _fixture.RunWorkflowAsync(workflow);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        return (result, lines);
    }
}
