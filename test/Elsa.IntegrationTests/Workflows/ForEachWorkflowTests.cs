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

public class ForEachWorkflowTests
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly Workflow _workflow;

    public ForEachWorkflowTests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestContainerBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
        _workflow = new WorkflowDefinitionBuilder().BuildWorkflow(new ForEachWorkflow());
    }

    [Fact(DisplayName = "ForEach outputs each iteration")]
    public async Task Test1()
    {
        await _workflowRunner.RunAsync(_workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal("C#", lines[0]);
        Assert.Equal("Rust", lines[1]);
        Assert.Equal("Go", lines[2]);
    }

    private class ForEachWorkflow : IWorkflow
    {
        public void Build(IWorkflowDefinitionBuilder workflow)
        {
            var items = new[] { "C#", "Rust", "Go" };
            var currentItem = new Variable<string>();

            workflow.WithRoot(new Sequence
            {
                Activities =
                {
                    new ForEach<string>
                    {
                        Items = new Input<ICollection<string>>(items),
                        CurrentValue = currentItem,
                        Body = new WriteLine(context => currentItem.Get(context))
                    },
                }
            });
        }
    }
}