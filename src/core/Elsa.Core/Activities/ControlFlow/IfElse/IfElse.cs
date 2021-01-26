using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Events;
using Elsa.Services;
using Elsa.Services.Models;
using MediatR;
using Newtonsoft.Json.Linq;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [Activity(
        DisplayName = "If/Else",
        Category = "Control Flow",
        Description = "Evaluate a Boolean expression and continue execution depending on the result.",
        Outcomes = new[] { True, False, OutcomeNames.Done }
    )]
    public class IfElse : Activity, INotificationHandler<ScopeEvicted>
    {
        public const string True = "True";
        public const string False = "False";

        [ActivityProperty(Hint = "The condition to evaluate.")]
        public bool Condition { get; set; }

        public bool EnteredScope
        {
            get => GetState<bool>();
            set => SetState(value);
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            if (!context.WorkflowInstance.Scopes.Contains(Id))
            {
                if (!EnteredScope)
                {
                    context.WorkflowInstance.Scopes.Push(Id);
                    EnteredScope = true;
                }
                else
                {
                    EnteredScope = false;
                    return Done();
                }
            }

            var outcome = Condition ? True : False;
            return Outcome(outcome);
        }

        public Task Handle(ScopeEvicted notification, CancellationToken cancellationToken)
        {
            if (notification.EvictedScope.Type != nameof(IfElse)) 
                return Task.CompletedTask;
            
            var data = notification.WorkflowExecutionContext.WorkflowInstance.ActivityData.GetItem(notification.EvictedScope.Id, () => new JObject());
            data.SetState(nameof(EnteredScope), false);
            
            return Task.CompletedTask;
        }
    }
}