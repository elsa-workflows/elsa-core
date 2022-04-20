using System.Linq;
using System.Threading.Tasks;
using Elsa.Activities;
using Elsa.Builders;
using Elsa.Contracts;
using Elsa.Models;
using Elsa.Modules.Activities.Activities.Console;
using Elsa.Testing.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.IntegrationTests.Workflows;

public class SequentialWorkflowTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();

    public SequentialWorkflowTests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
    }
    
    [Fact(DisplayName = "Sequence completes after its child activities complete")]
    public async Task Test1()
    {
        var workflow = new WorkflowDefinitionBuilder().BuildWorkflow(new SequentialWorkflow());
        await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Line 1", "Line 2", "Line 3" }, lines);
    }

    [Fact(DisplayName = "Sequence completes after its child sequence activity complete")]
    public async Task Test2()
    {
        var workflow = new WorkflowDefinitionBuilder().BuildWorkflow(new NestedSequentialWorkflow());
        await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "Line 1", "Line 2", "Line 3", "End" }, lines);
    }
    
    private class SequentialWorkflow : IWorkflow
    {
        public void Build(IWorkflowDefinitionBuilder workflow)
        {
            workflow.WithRoot(new Sequence
            {
                Activities =
                {
                    new WriteLine("Line 1"),
                    new WriteLine("Line 2"),
                    new WriteLine("Line 3")
                }
            });
        }
    }

    private class NestedSequentialWorkflow : IWorkflow
    {
        public void Build(IWorkflowDefinitionBuilder workflow)
        {
            workflow.WithRoot(new Sequence
            {
                Activities =
                {
                    new WriteLine("Start"),
                    new Sequence
                    {
                        Activities =
                        {
                            new WriteLine("Line 1"),
                            new WriteLine("Line 2"),
                            new WriteLine("Line 3")
                        }
                    },
                    new WriteLine("End"),
                }
            });
        }
    }
}