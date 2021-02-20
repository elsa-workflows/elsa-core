using System.Collections.Generic;
using System.Linq;
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
    [ActivityAttribute(
        DisplayName = "Switch",
        Category = "Control Flow",
        Description = "Evaluate multiple conditions and continue execution depending on the results.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class Switch : Activity, INotificationHandler<ScopeEvicted>
    {
        [ActivityProperty(Hint = "The conditions to evaluate.")]
        public ICollection<SwitchCase> Cases { get; set; } = new List<SwitchCase>();

        [ActivityProperty(Hint = "The conditions to evaluate.")]
        public SwitchMode Mode { get; set; } = SwitchMode.MatchFirst;
        
        private bool Evaluated
        {
            get => GetState<bool>();
            set => SetState(value);
        }
        
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
            
            if (Evaluated)
            {
                Evaluated = false;
                return Done();
            }
            
            var matches = Cases.Where(x => x.Condition).Select(x => x.Name).ToList();
            var results = Mode == SwitchMode.MatchFirst ? matches.Any() ? new[] { matches.First() } : new string[0] : matches.ToArray();
            var outcomes = results;
            
            Evaluated = true;
            return Outcomes(outcomes);
        }

        public Task Handle(ScopeEvicted notification, CancellationToken cancellationToken)
        {
            if (notification.EvictedScope.Type != nameof(Switch)) 
                return Task.CompletedTask;
            
            var data = notification.WorkflowExecutionContext.WorkflowInstance.ActivityData.GetItem(notification.EvictedScope.Id, () => new JObject());
            data.SetState(nameof(EnteredScope), false);
            
            return Task.CompletedTask;
        }
    }
}