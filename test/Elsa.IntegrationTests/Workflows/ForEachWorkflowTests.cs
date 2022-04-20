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

    public ForEachWorkflowTests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
        _workflowRunner = services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "ForEach outputs each iteration")]
    public async Task Test1()
    {
        var items = new[] { "C#", "Rust", "Go"};
        var workflow = new WorkflowDefinitionBuilder().BuildWorkflow(new ForEachWorkflow(items));
        await _workflowRunner.RunAsync(workflow);
        var lines = _capturingTextWriter.Lines.ToList();
        Assert.Equal(items, lines);
    }

    private class ForEachWorkflow : IWorkflow
    {
        private readonly ICollection<string> _items;

        public ForEachWorkflow(ICollection<string> items)
        {
            _items = items;
        }
        
        public void Build(IWorkflowDefinitionBuilder workflow)
        {
            var currentItem = new Variable<string>();

            workflow.WithRoot(new Sequence
            {
                Activities =
                {
                    new ForEach<string>
                    {
                        Items = new Input<ICollection<string>>(_items),
                        CurrentValue = currentItem,
                        Body = new WriteLine(currentItem)
                    },
                }
            });
        }
    }
}