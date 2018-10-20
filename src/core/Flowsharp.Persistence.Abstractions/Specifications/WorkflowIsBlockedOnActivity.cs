using System.Linq;
using Flowsharp.Persistence.Models;

namespace Flowsharp.Persistence.Specifications
{
    public class WorkflowIsBlockedOnActivity: ISpecification<WorkflowInstance, IWorkflowInstanceSpecificationVisitor>
    {
        public WorkflowIsBlockedOnActivity(string activityName)
        {
            ActivityName = activityName;
        }

        public string ActivityName { get; }

        public bool IsSatisfiedBy(WorkflowInstance value)
        {
            var query =
                from activity in value.Workflow.BlockingActivities 
                where activity.Name == ActivityName
                select activity;

            return query.Any();
        }

        public void Accept(IWorkflowInstanceSpecificationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}