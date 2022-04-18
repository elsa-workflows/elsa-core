using System.Collections.Generic;
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

public class BreakWorkflowTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly Workflow _workflow;

    public BreakWorkflowTests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
        _workflow = new WorkflowDefinitionBuilder().BuildWorkflow(new BreakForEachWorkflow());
    }

    [Fact(DisplayName = "Break exists out of ForEach")]
    public async Task Test1()
    {
        await _workflowRunner.RunAsync(_workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "C#", "Test", "End" }, lines);
    }

    private class BreakForEachWorkflow : IWorkflow
    {
        public void Build(IWorkflowDefinitionBuilder workflow)
        {
            var items = new[] { "C#", "Rust", "Go" };
            var currentItem = new Variable<string>();

            workflow.WithRoot(new Sequence
            {
                Activities =
                {
                    new WriteLine("Start"),
                    new ForEach<string>
                    {
                        Items = new Input<ICollection<string>>(items),
                        CurrentValue = currentItem,
                        Body = new Sequence
                        {
                            Activities =
                            {
                                new WriteLine(currentItem),
                                new If(context => currentItem.Get(context) == "Rust")
                                {
                                    Then = new Break()
                                },
                                new WriteLine("Test")
                            }
                        }
                    },
                    new WriteLine("End"),
                }
            });
        }
    }
}