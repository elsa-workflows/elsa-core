using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Signals;

namespace Elsa.Samples.TelnyxIntegration;

[FlowNode("Over 18", "Under 18")]
public class GetAge : Composite<int>
{
    public GetAge()
    {
        Root = new Inline(async context =>
        {
            var random = new Random();
            var n = random.Next(2);
            var outcome = n == 0 ? "Under 18" : "Over 18";
            await context.SendSignalAsync(new CompleteCompositeSignal(new Outcomes(outcome)));
        });
    }
}