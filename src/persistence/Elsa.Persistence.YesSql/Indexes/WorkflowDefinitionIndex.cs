using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Persistence.YesSql.Documents;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql.Indexes
{
    public class WorkflowDefinitionIndex : MapIndex
    {
        public string WorkflowDefinitionId { get; set; }
    }

    public class WorkflowDefinitionStartActivitiesIndex : WorkflowDefinitionIndex
    {
        public string StartActivityId { get; set; }
        public string StartActivityType { get; set; }
    }

    public class WorkflowDefinitionIndexProvider : IndexProvider<WorkflowDefinitionDocument>
    {
        public override void Describe(DescribeContext<WorkflowDefinitionDocument> context)
        {
            context.For<WorkflowDefinitionIndex>()
                .Map(
                    workflowDefinition => new WorkflowDefinitionIndex
                    {
                        WorkflowDefinitionId = workflowDefinition.WorkflowDefinitionId
                    }
                );

            context.For<WorkflowDefinitionStartActivitiesIndex>()
                .Map(
                    workflowDefinition => GetStartActivities(workflowDefinition)
                        .Select(
                            activity => new WorkflowDefinitionStartActivitiesIndex
                            {
                                WorkflowDefinitionId = workflowDefinition.WorkflowDefinitionId,
                                StartActivityId = activity.Id,
                                StartActivityType = activity.Type
                            }
                        )
                );
        }
        
        private static IEnumerable<ActivityDefinition> GetStartActivities(WorkflowDefinitionDocument workflow)
        {
            var destinationActivityIds = workflow.Connections.Select(x => x.DestinationActivityId).Distinct().ToLookup(x => x);
            
            var query =
                from activity in workflow.Activities
                where !destinationActivityIds.Contains(activity.Id)
                select activity;

            return query;
        }
    }
}