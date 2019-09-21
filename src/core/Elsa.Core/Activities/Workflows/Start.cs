using Elsa.Attributes;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Workflows
{
    [ActivityDefinition(
        Category = "Workflows",
        Description = "Useful when you need to invoke the workflow manually."
    )]
    public class Start : Activity
    {
        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext context)
        {
            return Done();
        }
    }
}