using System.Linq;
using Elsa.Models;

namespace Elsa.Persistence.Specifications
{
    public class WorkflowIsBlockedOnActivity: ISpecification<Workflow, IWorkflowSpecificationVisitor>
    {
        public WorkflowIsBlockedOnActivity(string activityName)
        {
            ActivityName = activityName;
        }

        public string ActivityName { get; }

        public bool IsSatisfiedBy(Workflow value)
        {
            var query =
                from activity in value.BlockingActivities 
                where activity.Name == ActivityName
                select activity;

            return query.Any();
        }

        public void Accept(IWorkflowSpecificationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}