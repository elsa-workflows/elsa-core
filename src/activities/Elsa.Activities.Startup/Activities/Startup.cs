using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Activities.Startup.Activities
{
    [ActivityDefinition(
        Category = "Startup",
        Description = "Triggers at startup."
    )]
    public class Startup : Activity
    {

        public Startup()
        {
        }


        protected override ActivityExecutionResult OnExecute(WorkflowExecutionContext context)
        {
            return Halt();
        }

        protected override async Task<ActivityExecutionResult> OnResumeAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
        {
            return Done();
        }
    }
}