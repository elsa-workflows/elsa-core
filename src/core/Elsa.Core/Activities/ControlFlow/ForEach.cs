using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.ControlFlow.Activities
{
    [ActivityDefinition(Category = "Control Flow", Description = "Iterate over a collection.", Icon = "far fa-circle")]
    public class ForEach : Activity
    {
        [ActivityProperty(Hint = "Enter an expression that evaluates to an array of items to iterate over.")]
        public IWorkflowExpression<IList<object>> Collection
        {
            get => GetState<IWorkflowExpression<IList<object>>>();
            set => SetState(value);
        }

        public IActivity Activity
        {
            get => GetState<IActivity>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var collection = await context.EvaluateAsync(Collection, cancellationToken) ?? new object[0];
            var results = new List<IActivityExecutionResult> { Done() };

            foreach (var item in collection.Reverse()) 
                results.Add(ScheduleActivity(Activity, Variable.From(item)));

            return Combine(results);
        }
    }
}