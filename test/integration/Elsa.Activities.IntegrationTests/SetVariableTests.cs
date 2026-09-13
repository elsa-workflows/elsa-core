using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.IntegrationTests;

public class SetVariableTests : IAsyncDisposable
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public SetVariableTests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    public async ValueTask DisposeAsync()
    {
        if (_services is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (_services is IDisposable disposable)
            disposable.Dispose();
    }

    [Test]
    [DisplayName("SetVariable sets variable in nearest scope when multiple variables with same name exist")]
    public async Task SetVariable_SetsVariableInNearestScope_WhenMultipleVariablesWithSameNameExist()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<VariableScopingWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();

        // The sequence-level variable should be set to "Sequence Value"
        await Assert.That(lines).IsEquivalentTo(new[] { "Sequence Value" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);

    }
}

class VariableScopingWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var workflowLevelVariable = new Variable<string>("Foo", "Workflow Value");
        var sequenceLevelVariable = new Variable<string>("Foo", "Initial Value");

        workflow.Root = new Sequence
        {
            Variables = { workflowLevelVariable },
            Activities =
            {
                new Sequence
                {
                    Variables = { sequenceLevelVariable },
                    Activities =
                    {
                        new SetVariable
                        {
                            Variable = sequenceLevelVariable,
                            Value = new("Sequence Value")
                        },
                        new WriteLine(context => context.GetVariable<string>("Foo"))
                    }
                }
            }
        };
    }
}
