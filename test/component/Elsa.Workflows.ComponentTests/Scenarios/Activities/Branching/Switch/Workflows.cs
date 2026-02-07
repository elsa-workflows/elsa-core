using Elsa.Workflows.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Branching.Switch;

public class SwitchMatchingCaseWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(SwitchMatchingCaseWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        var value = builder.WithVariable<string>("Value", "B");

        builder.Root = new Elsa.Workflows.Activities.Switch
        {
            Cases =
            {
                new("Case A", context => value.Get(context) == "A", new WriteLine("Case A matched")),
                new("Case B", context => value.Get(context) == "B", new WriteLine("Case B matched")),
                new("Case C", context => value.Get(context) == "C", new WriteLine("Case C matched"))
            },
            Default = new WriteLine("Default case")
        };
    }
}

public class SwitchDefaultCaseWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = nameof(SwitchDefaultCaseWorkflow);

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        var value = builder.WithVariable<string>("Value", "X");

        builder.Root = new Elsa.Workflows.Activities.Switch
        {
            Cases =
            {
                new("Case A", context => value.Get(context) == "A", new WriteLine("Case A matched")),
                new("Case B", context => value.Get(context) == "B", new WriteLine("Case B matched")),
                new("Case C", context => value.Get(context) == "C", new WriteLine("Case C matched"))
            },
            Default = new WriteLine("Default case")
        };
    }
}

