using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using ScheduledActivity = Elsa.Services.Models.ScheduledActivity;

namespace Elsa.Activities.Flowcharts
{
    public class Flowchart : Activity
    {
        public ICollection<IActivity> Activities
        {
            get => GetState<ICollection<IActivity>>(() => new HashSet<IActivity>());
            set => SetState(new HashSet<IActivity>(value));
        }

        public ICollection<Connection> Connections
        {
            get => GetState<ICollection<Connection>>(() => new List<Connection>());
            set => SetState(value);
        }

        protected override IActivityExecutionResult OnExecute(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext)
        {
            workflowExecutionContext.BeginScope(this);
            var activity = Activities.FirstOrDefault();

            if (activity != null)
            {
                var input = activityExecutionContext.Input;
                return Schedule(activity, input);
            }
            
            workflowExecutionContext.EndScope();
            return Done(activityExecutionContext.Input);
        }

        protected override IActivityExecutionResult OnChildExecuted(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, ActivityExecutionContext childActivityExecutionContext)
        {
            if(workflowExecutionContext.Status != WorkflowStatus.Running)
                return Noop();
            
            var executedActivity = childActivityExecutionContext.Activity;
            var outcomes = childActivityExecutionContext.Outcomes;
            var connections = FindConnections(executedActivity, outcomes);
            var scheduledActivities = CreateScheduledActivities(childActivityExecutionContext, connections).ToList();

            if(scheduledActivities.Any())
                return Schedule(scheduledActivities);
            
            workflowExecutionContext.EndScope();
            return Done(activityExecutionContext.Input);
        }

        private Connection FindConnection(IActivity source, string outcome) => Connections.FirstOrDefault(x => x.Source.Activity == source && (string.Equals(x.Source.Outcome, outcome) || string.IsNullOrEmpty(x.Source.Outcome)));
        private IEnumerable<Connection> FindConnections(IActivity source, IEnumerable<string> outcomes) => outcomes.Select(x => FindConnection(source, x)).Where(x => x != null);
        private IEnumerable<ScheduledActivity> CreateScheduledActivities(ActivityExecutionContext activityExecutionContext, IEnumerable<Connection> connections) => connections.Select(x => new ScheduledActivity(x.Target.Activity, activityExecutionContext.Output));
    }
}