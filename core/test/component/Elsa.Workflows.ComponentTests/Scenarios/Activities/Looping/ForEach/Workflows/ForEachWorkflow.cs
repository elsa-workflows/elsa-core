using Elsa.Extensions;
using Elsa.Scheduling.Activities;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Looping.ForEach.Workflows;

public class ForEachWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        builder.Root = new Sequence
        {
            Activities =
            {
                new ForEach<string>(["a", "b", "c"])
                {
                    Body = new Sequence
                    {
                        Activities =
                        {
                            new WriteLine(context => $"Processing item: {context.GetVariable<string>("CurrentValue")}"),
                            Delay.FromMilliseconds(100)
                        }
                    }
                }
            }
        };
    }
}