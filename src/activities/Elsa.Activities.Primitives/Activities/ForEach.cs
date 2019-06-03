using Elsa.Attributes;
using Elsa.Models;

namespace Elsa.Activities.Primitives.Activities
{
    [Category("Control Flow")]
    [DisplayName("For Each")]
    [Description("Iterate over a list of items.")]
    [Endpoints(EndpointNames.Next, EndpointNames.Done)]
    public class ForEach : Activity
    {
    }
}