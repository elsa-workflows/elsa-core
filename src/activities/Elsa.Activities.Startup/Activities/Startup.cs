using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Startup.Activities
{
    [Activity(Category = "Startup", Description = "Triggers at startup.")]
    public class Startup : Activity
    {
        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            return Done();
        }
    }
}