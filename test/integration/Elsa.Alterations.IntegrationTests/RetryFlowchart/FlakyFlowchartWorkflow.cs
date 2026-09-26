using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.IncidentStrategies;

namespace Elsa.Alterations.IntegrationTests.RetryFlowchart;

/// <summary>
/// A simple flowchart workflow with three sequential steps. The middle step is a <see cref="FlakyActivity"/>
/// that fails on its first execution. The workflow uses <see cref="ContinueWithIncidentsStrategy"/> so the
/// workflow does not transition to Faulted when the activity throws.
/// </summary>
public class FlakyFlowchartWorkflow : WorkflowBase
{
    public const string FlakyActivityId = "Flaky";

    protected override void Build(IWorkflowBuilder builder)
    {
        var start = new WriteLine("Start");
        var flaky = new FlakyActivity { Id = FlakyActivityId };
        var end = new WriteLine("End");

        builder.WorkflowOptions.IncidentStrategyType = typeof(ContinueWithIncidentsStrategy);

        builder.Root = new Flowchart
        {
            Activities =
            {
                start,
                flaky,
                end
            },
            Connections =
            {
                new Connection(start, flaky),
                new Connection(flaky, end)
            }
        };
    }
}
