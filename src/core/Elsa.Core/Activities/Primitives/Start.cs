using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Workflows.Activities
{
    [ActivityDefinition(
        Category = "Workflows",
        Description = "The start of the workflow.",
        Icon = "far fa-flag"
    )]
    public class Start : Activity
    {
        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => Done();
    }
}