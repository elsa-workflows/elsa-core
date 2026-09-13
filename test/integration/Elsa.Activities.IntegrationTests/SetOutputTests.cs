using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Activities.SetOutput;

namespace Elsa.Activities.IntegrationTests;

public class SetOutputTests : IAsyncDisposable
{
    private readonly IServiceProvider _services;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    public SetOutputTests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .WithCapturingTextWriter(_capturingTextWriter)
            .AddWorkflow<MultipleOutputsWorkflow>()
            .Build();
    }

    public async ValueTask DisposeAsync()
    {
        if (_services is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (_services is IDisposable disposable)
            disposable.Dispose();
    }

    [Test]
    [DisplayName("SetOutput can set multiple outputs")]
    public async Task SetOutput_Should_Set_Multiple_Outputs()
    {
        // Arrange
        await _services.PopulateRegistriesAsync();

        // Act
        var workflowState = await _services.RunWorkflowUntilEndAsync<MultipleOutputsWorkflow>();

        // Assert
        await Assert.That(workflowState.Output.Keys).Contains("FirstName");

        await Assert.That(workflowState.Output.Keys).Contains("LastName");

        await Assert.That(workflowState.Output.Keys).Contains("Age");


        await Assert.That(workflowState.Output["FirstName"]).IsEqualTo("John");

        await Assert.That(workflowState.Output["LastName"]).IsEqualTo("Doe");

        await Assert.That(workflowState.Output["Age"]).IsEqualTo(30);

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
