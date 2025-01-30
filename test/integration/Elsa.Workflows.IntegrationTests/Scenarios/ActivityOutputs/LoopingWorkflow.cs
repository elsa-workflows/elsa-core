using Elsa.Extensions;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.ActivityOutputs;

public class LoopingWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var readCurrentValue = new Inline<string>(context => context.GetVariable<string>("CurrentValue")!);

        builder.Root = new ForEach<string>(
        [
            "Item 1",
            "Item 2"
        ])
        {
            Body = new Sequence
            {
                Activities =
                [
                    readCurrentValue,
                    new WriteLine(context =>
                    {
                        var currentValue = context.GetVariable<string>("CurrentValue");
                        var activityResult = context.GetActivityExecutionContext().GetResult(readCurrentValue);
                        return $"Current value: {currentValue}, Activity result: {activityResult}";
                    })
                ]
            }
        };
    }
}