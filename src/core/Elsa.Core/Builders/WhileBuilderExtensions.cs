using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public static class WhileBuilderExtensions
    {
        public static OutcomeBuilder While(this IBuilder builder, IWorkflowExpression<bool> condition, Action<OutcomeBuilder> iteration) => While(builder.Then<While>(x => x.Condition = condition), iteration);
        public static OutcomeBuilder While(this IBuilder builder, Func<ActivityExecutionContext, bool> condition, Action<OutcomeBuilder> iteration) => builder.While(new CodeExpression<bool>(condition), iteration);
        public static OutcomeBuilder While(this IBuilder builder, Func<bool> condition, Action<OutcomeBuilder> iteration) => builder.While(new CodeExpression<bool>(condition), iteration);
        public static OutcomeBuilder While(this IBuilder builder, bool condition, Action<OutcomeBuilder> iteration) => builder.While(new CodeExpression<bool>(() => condition), iteration);

        private static OutcomeBuilder While(ActivityBuilder @while, Action<OutcomeBuilder> iteration)
        {
            iteration.Invoke(@while.When(OutcomeNames.Iterate));
            return @while.When(OutcomeNames.Done);
        }
    }
}