using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Elsa.Services;
using Elsa.Services.Extensions;
using Elsa.Services.Models;

namespace Elsa.Core.Services
{
    public class WorkflowRegistry : IWorkflowRegistry
    {
        private readonly IDictionary<string, WorkflowBlueprint> workflowBlueprints;

        public WorkflowRegistry()
        {
            workflowBlueprints = new Dictionary<string, WorkflowBlueprint>();
        }
        
        public void RegisterWorkflow(WorkflowBlueprint blueprint)
        {
            workflowBlueprints[blueprint.Id] = blueprint;
        }

        public IEnumerable<(WorkflowBlueprint, ActivityBlueprint)> ListByStartActivity(string activityType, CancellationToken cancellationToken)
        {
            var query =
                from workflow in workflowBlueprints.Values
                from activity in workflow.GetStartActivities()
                where activity.TypeName == activityType
                select (workflow, activity);

            return query.Distinct();
        }

        public WorkflowBlueprint GetById(string id, CancellationToken cancellationToken = default)
        {
            return workflowBlueprints.ContainsKey(id) ? workflowBlueprints[id] : default;
        }
    }
}