using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.ComponentTests.Scenarios.VariablesArray.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.VariablesArray.Workflows;

public class VariableArrayWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        var elements = builder.WithVariable<string[]>("Elements", ["Element 1", "Element 2", "Element 3"]).WithWorkflowStorage();

        builder.Root = new Sequence
        {
            Activities =
            {
                new While(context => elements.Get(context)!.Length > 0)
                {
                    Body = new Sequence
                    {
                        Activities =
                        {
                            new WriteLine(context => $"Top Element: {elements.Get(context)![0]}"),
                            new RemoveTopElementStep()
                        }
                    }
                }
            }
        };
    }
}