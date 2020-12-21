using System.Linq;
using Elsa.ActivityResults;
using Elsa.Builders;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public class CompositeActivity : Activity
    {
        public virtual void Build(ICompositeActivityBuilder composite)
        {
        }

        private bool IsScheduled
        {
            get => GetState<bool>();
            set => SetState(value);
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            if (IsScheduled)
            {
                if (HasPendingChildren(context))
                    return PostSchedule(Id);

                context.WorkflowExecutionContext.WorkflowInstance.ParentActivities.Pop();
                IsScheduled = false;
                return Complete(context);
            }

            var compositeActivityBlueprint = (ICompositeActivityBlueprint) context.ActivityBlueprint;
            var startActivities = compositeActivityBlueprint.GetStartActivities().Select(x => x.Id).ToList();
            context.WorkflowExecutionContext.WorkflowInstance.ParentActivities.Push(Id);
            context.WorkflowExecutionContext.PostScheduleActivity(Id);
            IsScheduled = true;
            return Schedule(startActivities, context.Input);
        }

        protected virtual IActivityExecutionResult Complete(ActivityExecutionContext context) => Done();

        private static bool HasPendingChildren(ActivityExecutionContext context)
        {
            var children = ((CompositeActivityBlueprint) context.ActivityBlueprint).Activities.Select(x => x.Id).ToList();
            var workflowInstance = context.WorkflowExecutionContext.WorkflowInstance;
            var hasPendingPostScheduledChildren = workflowInstance.PostScheduledActivities.Any(x => children.Contains(x.ActivityId));
            var hasPendingScheduledChildren = workflowInstance.ScheduledActivities.Any(x => children.Contains(x.ActivityId));
            return hasPendingPostScheduledChildren || hasPendingScheduledChildren;
        }
    }
}