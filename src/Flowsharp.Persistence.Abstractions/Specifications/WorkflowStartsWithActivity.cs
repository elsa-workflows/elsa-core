using System.Linq;
using Flowsharp.Persistence.Models;

namespace Flowsharp.Persistence.Specifications
{
    public class WorkflowStartsWithActivity : ISpecification<WorkflowDefinition, IWorkflowDefinitionSpecificationVisitor>
    {
        public WorkflowStartsWithActivity(string activityName)
        {
            ActivityName = activityName;
        }

        public string ActivityName { get; }

        public bool IsSatisfiedBy(WorkflowDefinition value)
        {
            var query =
                from activity in value.Workflow.Activities 
                where activity.Name == ActivityName
                where !value.Workflow.Connections.Select(x => x.Target.Activity).Contains(activity)
                select activity;

            return query.Any();
        }

        public void Accept(IWorkflowDefinitionSpecificationVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
