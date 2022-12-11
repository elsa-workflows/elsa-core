using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Samples.TelnyxIntegration;

[FlowNode("Over 18", "Under 18")]
public class GetAge : Composite<int>
{
    public GetAge()
    {
        var ageVariable = new Variable<int>();

        Root = new Sequence
        {
            Variables = { ageVariable },
            Activities =
            {
                Inline(() =>
                {
                    var random = new Random();
                    return random.Next(36);
                }, ageVariable),

                new Complete(context => ageVariable.Get<int>(context) < 18 ? "Under 18" : "Over 18")
            }
        };
    }
}