using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Activities.SetOutput;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests;

public class SetOutputTests
{
    private readonly IServiceProvider _services;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    public SetOutputTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .WithCapturingTextWriter(_capturingTextWriter)
            .AddWorkflow<MultipleOutputsWorkflow>()
            .Build();
    }

    [Fact(DisplayName = "SetOutput can set multiple outputs")]
    public async Task SetOutput_Should_Set_Multiple_Outputs()
    {
        // Arrange
        await _services.PopulateRegistriesAsync();

        // Act
        var workflowState = await _services.RunWorkflowUntilEndAsync<MultipleOutputsWorkflow>();

        // Assert
        Assert.Contains("FirstName", workflowState.Output.Keys);
        Assert.Contains("LastName", workflowState.Output.Keys);
        Assert.Contains("Age", workflowState.Output.Keys);

        Assert.Equal("John", workflowState.Output["FirstName"]);
        Assert.Equal("Doe", workflowState.Output["LastName"]);
        Assert.Equal(30, workflowState.Output["Age"]);
    }
}

// Test Workflow

/// <summary>
/// Workflow that sets multiple outputs using SetOutput
/// </summary>
public class MultipleOutputsWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new Sequence
        {
            Activities =
            {
                new SetOutput
                {
                    OutputName = new("FirstName"),
                    OutputValue = new("John")
                },
                new SetOutput
                {
                    OutputName = new("LastName"),
                    OutputValue = new("Doe")
                },
                new SetOutput
                {
                    OutputName = new("Age"),
                    OutputValue = new(30)
                }
            }
        };
    }
}