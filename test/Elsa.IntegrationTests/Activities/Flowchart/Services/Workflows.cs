using System.Collections.Generic;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Implementations;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.IntegrationTests.Activities.Flowchart.Services;

class Workflow1 : WorkflowBase
{
    private readonly ICollection<string> _items;

    public Workflow1(ICollection<string> items)
    {
        _items = items;
    }

    protected override void Build(IWorkflowBuilder workflow)
    {
        var currentItem = workflow.WithVariable<string>("CurrentValue", "").WithMemoryStorage();
        var writeLine1 = new WriteLine { Id = "WriteLine1", Text = new Input<string>("Start!") };
        var forEach1 = new ForEach<string> { Id = "ForEach1", Items = new Input<ICollection<string>>(_items), CurrentValue = new Output<string?>(currentItem) };
        var writeLine2 = new WriteLine { Id = "WriteLine2", Text = new Input<string>("Current Item") };
        var writeLine3 = new WriteLine { Id = "WriteLine3", Text = new Input<string>(currentItem) };
        var writeLine4 = new WriteLine { Id = "WriteLine4", Text = new Input<string>("Done!") };

        workflow.WithRoot(new Workflows.Core.Activities.Flowchart.Activities.Flowchart
        {
            Start = writeLine1,
            Activities = { writeLine1, writeLine2, writeLine3, writeLine4, forEach1 },
            Connections =
            {
                new Connection(writeLine1, forEach1),
                new Connection(forEach1, writeLine2, nameof(ForEach.Body)),
                new Connection(writeLine2, writeLine3),
                new Connection(forEach1, writeLine4)
            }
        });
    }
}

class Workflow2 : WorkflowBase
{
    private readonly ICollection<string> _items;

    public Workflow2(ICollection<string> items)
    {
        _items = items;
    }

    protected override void Build(IWorkflowBuilder workflow)
    {
        var currentItem = workflow.WithVariable<string>("CurrentValue", "").WithMemoryStorage();
        var writeLine1 = new WriteLine { Id = "WriteLine1", Text = new Input<string>("Start!") };
        var forEach1 = new ForEach<string> { Id = "ForEach1", Items = new Input<ICollection<string>>(_items), CurrentValue = new Output<string?>(currentItem) };
        var writeLine2 = new WriteLine { Id = "WriteLine2", Text = new Input<string>(currentItem) };
        var writeLine3 = new WriteLine { Id = "WriteLine3", Text = new Input<string>("Done!") };

        workflow.WithRoot(new Workflows.Core.Activities.Flowchart.Activities.Flowchart
        {
            Start = writeLine1,
            Activities = { writeLine1, writeLine2, writeLine3, forEach1 },
            Connections =
            {
                new Connection(writeLine1, forEach1),
                new Connection(forEach1, writeLine2, nameof(ForEach.Body)),
                new Connection(forEach1, writeLine3)
            }
        });
    }
}

class Workflow3 : WorkflowBase
{
    private readonly ICollection<string> _items;

    public Workflow3(ICollection<string> items)
    {
        _items = items;
    }

    protected override void Build(IWorkflowBuilder workflow)
    {
        var currentItem = workflow.WithVariable<string>("CurrentValue", "").WithMemoryStorage();
        var writeLine1 = new WriteLine { Id = "WriteLine1", Text = new Input<string>("Start!") };

        var forEach1 = new ForEach<string>
        {
            Id = "ForEach1",
            Items = new Input<ICollection<string>>(_items), CurrentValue = new Output<string?>(currentItem),
            Body = new WriteLine { Id = "WriteLine2", Text = new Input<string>(currentItem) }
        };

        var writeLine3 = new WriteLine { Id = "WriteLine3", Text = new Input<string>("Done!") };

        workflow.WithRoot(new Workflows.Core.Activities.Flowchart.Activities.Flowchart
        {
            Start = writeLine1,
            Activities = { writeLine1, writeLine3, forEach1 },
            Connections =
            {
                new Connection(writeLine1, forEach1),
                new Connection(forEach1, writeLine3)
            }
        });
    }
}