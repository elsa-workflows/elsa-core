using System.Linq;
using Elsa.Models;
using Elsa.Services.Extensions;
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

    public class WorkflowDefinitionIndexProvider : IndexProvider<WorkflowDefinition>
    {
        public override void Describe(DescribeContext<WorkflowDefinition> context)
        {
            context.For<WorkflowDefinitionIndex>()
                .Map(
                    workflowDefinition => new WorkflowDefinitionIndex
                    {
                        WorkflowDefinitionId = workflowDefinition.Id
                    }
                );

            context.For<WorkflowDefinitionStartActivitiesIndex>()
                .Map(
                    workflowDefinition => workflowDefinition.GetStartActivities()
                        .Select(
                            activity => new WorkflowDefinitionStartActivitiesIndex
                            {
                                WorkflowDefinitionId = workflowDefinition.Id,
                                StartActivityId = activity.Id,
                                StartActivityType = activity.Type
                            }
                        )
                );
        }
    }
}