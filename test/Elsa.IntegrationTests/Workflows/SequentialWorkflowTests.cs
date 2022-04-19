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
    private readonly Workflow _workflow;

    public SequentialWorkflowTests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
        _workflow = new WorkflowDefinitionBuilder().BuildWorkflow(new SequentialWorkflow());
    }

    [Fact(DisplayName = "Sequence completes only after its child activities complete")]
    public async Task Test1()
    {
        await _workflowRunner.RunAsync(_workflow);
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