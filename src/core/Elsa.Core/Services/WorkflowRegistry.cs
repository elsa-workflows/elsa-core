using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Extensions;
using Elsa.Services.Models;

namespace Elsa.Core.Services
{
    public class WorkflowRegistry : IWorkflowRegistry
    {
        private readonly IDictionary<string, WorkflowDefinition> workflowBlueprints;

        public WorkflowRegistry()
        {
            workflowBlueprints = new Dictionary<string, WorkflowDefinition>();
        }
        
        public void RegisterWorkflow(WorkflowDefinition definition)
        {
            workflowBlueprints[definition.Id] = definition;
        }

        public IEnumerable<(WorkflowDefinition, ActivityDefinition)> ListByStartActivity(string activityType, CancellationToken cancellationToken)
        {
            var query =
                from workflow in workflowBlueprints.Values
                from activity in workflow.GetStartActivities()
                where activity.TypeName == activityType
                select (workflow, activity);

            return query.Distinct();
        }

        public WorkflowDefinition GetById(string id, CancellationToken cancellationToken = default)
        {
            return workflowBlueprints.ContainsKey(id) ? workflowBlueprints[id] : default;
        }
    }
}