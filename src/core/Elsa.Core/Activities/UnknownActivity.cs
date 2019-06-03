using Elsa.Attributes;
using Elsa.Models;

namespace Elsa.Core.Activities
{
    [ActivityCategory("System")]
    [ActivityDisplayName("Unknown Activity")]
    [ActivityDescription("Used when a workflow references an activity type that could not be found.")]
    [Browsable(false)]
    public class UnknownActivity : Activity
    {
    }
}