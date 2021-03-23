using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Events;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using MediatR;
using Newtonsoft.Json.Linq;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    [Activity(Category = "Control Flow", Description = "Fork workflow execution into multiple branches.")]
    public class Fork : Activity, INotificationHandler<ScopeEvicted>
    {
        private readonly IMediator _mediator;

        public Fork(IMediator mediator)
        {
            _mediator = mediator;
        }
        
        [ActivityProperty(
            UIHint = ActivityPropertyUIHints.MultiText,
            Hint = "Enter one or more branch names.")]
        public ISet<string> Branches { get; set; } = new HashSet<string>();

        public bool EnteredScope
        {
            get => GetState<bool>();
            set => SetState(value);
        }
        
        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
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
                    await UnwindAsync(context);
                    return Done();
                }
            }   
            
            return Combine(Done(), Outcomes(Branches));
        }

        private async Task UnwindAsync(ActivityExecutionContext context)
        {
            RemoveBlockingActivities(context);
            await RemoveScopedActivitiesAsync(context);
        }

        private async Task RemoveScopedActivitiesAsync(ActivityExecutionContext context)
        {
            var workflowExecutionContext = context.WorkflowExecutionContext;
            var scopes = workflowExecutionContext.WorkflowInstance.Scopes.ToList();
            var descendants = workflowExecutionContext.GetOutboundActivityPath(Id).ToHashSet();

            for (var i = 0; i < scopes.Count; i++)
            {
                var activityScope = scopes.ElementAt(i);
                
                if(!descendants.Contains(activityScope.ActivityId))
                    continue;

                var evictedScopes = scopes.Skip(i).ToList();
                scopes = scopes.Take(i).ToList();

                foreach (var evictedScope in evictedScopes)
                {
                    var activity = workflowExecutionContext.WorkflowBlueprint.GetActivity(evictedScope.ActivityId)!;
                    await _mediator.Publish(new ScopeEvicted(workflowExecutionContext, activity));
                }
                
                workflowExecutionContext.WorkflowInstance.Scopes = new SimpleStack<ActivityScope>(scopes.AsEnumerable());
                break;
            }
        }

        private void RemoveBlockingActivities(ActivityExecutionContext context)
        {
            var workflowExecutionContext = context.WorkflowExecutionContext;
            var workflowInstance = workflowExecutionContext.WorkflowInstance;
            var blockingActivities = workflowInstance.BlockingActivities.ToList();
            var outboundActivityIds = workflowExecutionContext.GetOutboundActivityPath(Id).ToHashSet();
            var blockingActivityIdsInFork = blockingActivities.Where(x => outboundActivityIds.Contains(x.ActivityId)).Select(x => x.ActivityId).ToHashSet();
            
            workflowInstance.BlockingActivities.RemoveWhere(x => blockingActivityIdsInFork.Contains(x.ActivityId));
        }

        Task INotificationHandler<ScopeEvicted>.Handle(ScopeEvicted notification, CancellationToken cancellationToken)
        {
            if (notification.EvictedScope.Type != nameof(Fork)) 
                return Task.CompletedTask;
            
            var data = notification.WorkflowExecutionContext.WorkflowInstance.ActivityData.GetItem(notification.EvictedScope.Id, () => new JObject());
            data.SetState(nameof(EnteredScope), false);
            data.SetState("Unwinding", false);
            
            return Task.CompletedTask;
        }
    }
}