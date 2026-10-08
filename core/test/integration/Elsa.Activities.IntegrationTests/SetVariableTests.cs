using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests;

public class SetVariableTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public SetVariableTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "SetVariable sets variable in nearest scope when multiple variables with same name exist")]
    public async Task SetVariable_SetsVariableInNearestScope_WhenMultipleVariablesWithSameNameExist()
    {
        await _services.PopulateRegistriesAsync();
        await _workflowRunner.RunAsync<VariableScopingWorkflow>();
        var lines = _capturingTextWriter.Lines.ToList();

        // The sequence-level variable should be set to "Sequence Value"
        Assert.Equal(new[] { "Sequence Value" }, lines);
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
