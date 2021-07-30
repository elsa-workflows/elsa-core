using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Events;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using MediatR;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [ActivityAttribute(
        DisplayName = "Switch",
        Category = "Control Flow",
        Description = "Evaluate multiple conditions and continue execution depending on the results.",
        Outcomes = new[] { OutcomeNames.Default }
    )]
    public class Switch : Activity, INotificationHandler<ScopeEvicted>
    {
        [ActivityInput(Hint = "The conditions to evaluate.", UIHint = "switch-case-builder", DefaultSyntax = "Switch", IsDesignerCritical = true)]
        public ICollection<SwitchCase> Cases { get; set; } = new List<SwitchCase>();

        [ActivityInput(
            Hint = "The switch mode determines whether the first match should be scheduled, or all matches.",
            DefaultValue = SwitchMode.MatchFirst,
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public SwitchMode Mode { get; set; } = SwitchMode.MatchFirst;

        public bool EnteredScope
        {
            get => GetState<bool>();
            set => SetState(value);
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            if (!context.WorkflowInstance.Scopes.Contains(x => x.ActivityId == Id))
            {
                if (!EnteredScope)
                {
                    context.CreateScope();
                    EnteredScope = true;
                }
                else
                {
                    EnteredScope = false;
                    return Done();
                }
            }

            var matches = Cases.Where(x => x.Condition).Select(x => x.Name).ToList();
            var hasAnyMatches = matches.Any();
            var results = Mode == SwitchMode.MatchFirst ? hasAnyMatches ? new[] { matches.First() } : new string[0] : matches.ToArray();
            var outcomes = hasAnyMatches ? results : new[] { OutcomeNames.Default };

            return Outcomes(outcomes);
        }

        Task INotificationHandler<ScopeEvicted>.Handle(ScopeEvicted notification, CancellationToken cancellationToken)
        {
            if (notification.EvictedScope.Type != nameof(Switch))
                return Task.CompletedTask;

            var data = notification.WorkflowExecutionContext.WorkflowInstance.ActivityData.GetItem(notification.EvictedScope.Id, () => new Dictionary<string, object?>());
            data.SetState(nameof(EnteredScope), false);
            data.SetState("Unwinding", false);

            return Task.CompletedTask;
        }
    }
}