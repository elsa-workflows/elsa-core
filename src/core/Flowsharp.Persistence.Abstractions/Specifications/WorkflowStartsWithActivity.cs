using System.Linq;
using Flowsharp.Models;

namespace Flowsharp.Persistence.Specifications
{
    public class WorkflowStartsWithActivity : ISpecification<Workflow, IWorkflowSpecificationVisitor>
    {
        public WorkflowStartsWithActivity(string activityName)
        {
            ActivityName = activityName;
        }

        public string ActivityName { get; }

        public bool IsSatisfiedBy(Workflow value)
        {
            var query =
                from activity in value.Activities 
                where activity.Name == ActivityName
                where !value.Connections.Select(x => x.Target.Activity).Contains(activity)
                select activity;

            return query.Any();
        }

        public void Accept(IWorkflowSpecificationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
