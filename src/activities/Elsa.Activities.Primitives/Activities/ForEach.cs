using Elsa.Attributes;
using Elsa.Models;

namespace Elsa.Activities.Primitives.Activities
{
    [ActivityCategory("Control Flow")]
    [ActivityDisplayName("For Each")]
    [ActivityDescription("Iterate over a list of items.")]
    [Endpoints(EndpointNames.Next, EndpointNames.Done)]
    public class ForEach : Activity
    {
    }
}