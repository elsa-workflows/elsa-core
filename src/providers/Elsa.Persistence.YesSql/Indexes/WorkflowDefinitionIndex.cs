using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Persistence.YesSql.Documents;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql.Indexes
{
    public class WorkflowDefinitionIndex : MapIndex
    {
        public string VersionId { get; set; }
        public string WorkflowDefinitionId { get; set; }
        public int Version { get; set; }
        public bool IsLatest { get; set; }
        public bool IsPublished { get; set; }
        public bool IsDisabled { get; set; }
    }

    public class WorkflowDefinitionStartActivitiesIndex : MapIndex
    {
        public string StartActivityId { get; set; }
        public string StartActivityType { get; set; }
        public bool IsDisabled { get; set; }
    }

    public class WorkflowDefinitionIndexProvider : IndexProvider<WorkflowDefinitionVersionDocument>
    {
        public override void Describe(DescribeContext<WorkflowDefinitionVersionDocument> context)
        {
            context.For<WorkflowDefinitionIndex>()
                .Map(
                    document => new WorkflowDefinitionIndex
                    {
                        VersionId =  document.VersionId,
                        WorkflowDefinitionId = document.WorkflowDefinitionId,
                        Version = document.Version,
                        IsPublished = document.IsPublished,
                        IsLatest = document.IsLatest,
                        IsDisabled = document.IsDisabled
                    }
                );

            context.For<WorkflowDefinitionStartActivitiesIndex>()
                .Map(
                    document => GetStartActivities(document)
                        .Select(
                            activity => new WorkflowDefinitionStartActivitiesIndex
                            {
                                StartActivityId = activity.Id,
                                StartActivityType = activity.Type,
                                IsDisabled = document.IsDisabled
                            }
                        )
                );
        }
        
        private static IEnumerable<ActivityDefinition> GetStartActivities(WorkflowDefinitionVersionDocument workflow)
        {
            var targetActivityIds = workflow.Connections.Select(x => x.TargetActivityId).Distinct().ToLookup(x => x);
            
            var query =
                from activity in workflow.Activities
                where !targetActivityIds.Contains(activity.Id)
                select activity;

            return query;
        }
    }
}