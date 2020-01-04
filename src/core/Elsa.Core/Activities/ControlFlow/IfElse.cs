using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.ControlFlow
{
    [ActivityDefinition(
        DisplayName = "If/Else",
        Category = "Control Flow",
        Description = "Evaluate a Boolean expression and continue execution depending on the result.",
        RuntimeDescription = "x => !!x.state.expression ? `Evaluate <strong>${ x.state.expression.expression }</strong> and continue execution depending on the result.` : x.definition.description",
        Outcomes = new[] { OutcomeNames.True, OutcomeNames.False }
    )]
    public class IfElse : Activity
    {
        public IfElse(IWorkflowExpression<bool> condition, IActivity trueBranch, IActivity falseBranch)
        {
            Condition = condition;
            True = trueBranch;
            False = falseBranch;
        }
        
        public IfElse(Func<WorkflowExecutionContext, ActivityExecutionContext, bool> condition, Action trueBranch, Action falseBranch)
        {
            Condition = new CodeExpression<bool>(condition);
            True = new Inline(trueBranch);
            False = new Inline(falseBranch);
        }
        
        [ActivityProperty(Hint = "The expression to evaluate. The evaluated value will be used to switch on.")]
        public IWorkflowExpression<bool> Condition
        {
            get => GetState<IWorkflowExpression<bool>>();
            set => SetState(value);
        }

        [Outlet(OutcomeNames.True)]
        public IActivity True
        {
            get => GetState<IActivity>();
            set => SetState(value);
        }
        
        [Outlet(OutcomeNames.False)]
        public IActivity False
        {
            get => GetState<IActivity>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var result = await workflowExecutionContext.EvaluateAsync(Condition, activityExecutionContext, cancellationToken);
            var nextActivity = result ? True : False;

            return Schedule(nextActivity);
        }
    }
}