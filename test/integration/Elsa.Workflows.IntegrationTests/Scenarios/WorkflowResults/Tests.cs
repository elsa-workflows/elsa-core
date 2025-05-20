using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.WorkflowResults;

public class Tests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _services;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Setting a variable should be captured when the ResultVariable is set")]
    public async Task Test1()
    {
        await _services.PopulateRegistriesAsync();
        var expectedValue = "Some value";
        var variable1 = new Variable("Variable1");
        
        var workflow = Workflow.FromActivity(new Sequence
        {
            Activities =
            {
                new SetVariable
                {
                    Variable = variable1,
                    Value = new (expectedValue)
                }
            }
        });
        workflow.ResultVariable = variable1;
        var runWorkflowResult = await _workflowRunner.RunAsync(workflow);
        Assert.Equal(expectedValue, runWorkflowResult.Result);
    }
    
    [Fact(DisplayName = "Setting a variable should be captured when the ResultVariable is set")]
    public async Task Test2()
    {
        await _services.PopulateRegistriesAsync();
        var expectedValue = "Some value";
        var variable = new Variable<string>("Variable", "");
        
        var workflow = Workflow.FromActivity(new Sequence
        {
            Activities =
            {
                new SetVariable
                {
                    Variable = variable,
                    Value = new (expectedValue)
                }
            }
        });
        workflow.ResultVariable = variable;
        var runWorkflowResult = await _workflowRunner.RunAsync(workflow);
        Assert.Equal(expectedValue, runWorkflowResult.Result);
    }
}