using Elsa.Attributes;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Workflows
{
    [ActivityDefinition(
        Category = "Workflows",
        Description = "The start of the workflow."
    )]
    public class Start : Activity
    {
        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext context)
        {
            return Done();
        }
    }
}