using System.Threading.Tasks;
using Elsa.Testing.Shared;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Scenarios.WorkflowResults;

public class Tests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    public Tests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Setting a named variable should be captured when the ResultVariable is set")]
    public async Task Test1()
    {
        var expectedValue = "Some value";
        var variableName = "MyVar";
        
        var workflow = Workflow.FromActivity(new Sequence
        {
            Activities =
            {
                new SetVariable()
                {
                    Variable = new (variableName),
                    Value = new (expectedValue)
                }
            }
        });
        workflow.ResultVariable = new (variableName);
        var runWorkflowResult = await _workflowRunner.RunAsync(workflow);
        Assert.Equal(expectedValue, runWorkflowResult.Result);
    }
    
    [Fact(DisplayName = "Setting a variable should be captured when the ResultVariable is set")]
    public async Task Test2()
    {
        var expectedValue = "Some value";
        var variable = new Variable<string>();
        
        var workflow = Workflow.FromActivity(new Sequence
        {
            Activities =
            {
                new SetVariable()
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