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
            .AddWorkflow<BasicWorkflowOutputWorkflow>()
            .AddWorkflow<MultipleOutputsWorkflow>()
            .AddWorkflow<DynamicOutputValueWorkflow>()
            .Build();
    }

    [Fact(DisplayName = "SetOutput sets workflow execution context output")]
    public async Task SetOutput_Should_Set_Workflow_Output()
    {
        // Arrange
        await _services.PopulateRegistriesAsync();

        // Act
        var workflowState = await _services.RunWorkflowUntilEndAsync<BasicWorkflowOutputWorkflow>();

        // Assert
        Assert.Contains("Message", workflowState.Output.Keys);
        Assert.Equal("Hello, World!", workflowState.Output["Message"]);
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

    [Fact(DisplayName = "SetOutput handles dynamic output values")]
    public async Task SetOutput_Should_Handle_Dynamic_Values()
    {
        // Arrange
        await _services.PopulateRegistriesAsync();

        // Act
        var workflowState = await _services.RunWorkflowUntilEndAsync<DynamicOutputValueWorkflow>();

        // Assert
        Assert.Contains("Greeting", workflowState.Output.Keys);
        var greeting = workflowState.Output["Greeting"] as string;
        Assert.NotNull(greeting);
        Assert.StartsWith("Hello at ", greeting);
    }
}

// Test Workflows

/// <summary>
/// Basic workflow that uses SetOutput to set a workflow output
/// </summary>
public class BasicWorkflowOutputWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new SetOutput
        {
            OutputName = new("Message"),
            OutputValue = new("Hello, World!")
        };
    }
}

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

/// <summary>
/// Workflow that uses dynamic values with SetOutput
/// </summary>
public class DynamicOutputValueWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        workflow.Root = new SetOutput
        {
            OutputName = new("Greeting"),
            OutputValue = new(context => $"Hello at {DateTime.UtcNow:yyyy-MM-dd HH:mm}")
        };
    }
}
