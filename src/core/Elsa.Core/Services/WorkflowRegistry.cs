using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Extensions;
using Elsa.Services.Models;

namespace Elsa.Core.Services
{
    public class WorkflowRegistry : IWorkflowRegistry
    {
        private readonly Func<IWorkflowBuilder> workflowBuilderFactory;
        private readonly IDictionary<string, WorkflowDefinition> workflowDefinitions;

        public WorkflowRegistry(Func<IWorkflowBuilder> workflowBuilderFactory)
        {
            this.workflowBuilderFactory = workflowBuilderFactory;
            workflowDefinitions = new Dictionary<string, WorkflowDefinition>();
        }
        
        public void RegisterWorkflow(WorkflowDefinition definition)
        {
            workflowDefinitions[definition.Id] = definition;
        }

        public void RegisterWorkflow<T>() where T : IWorkflow, new()
        {
            var definition = workflowBuilderFactory().Build<T>();
            RegisterWorkflow(definition);
        }

        public IEnumerable<(WorkflowDefinition, ActivityDefinition)> ListByStartActivity(string activityType)
        {
            var query =
                from workflow in workflowDefinitions.Values
                from activity in workflow.GetStartActivities()
                where activity.TypeName == activityType
                select (workflow, activity);

            return query.Distinct();
        }

        public WorkflowDefinition GetById(string id)
        {
            return workflowDefinitions.ContainsKey(id) ? workflowDefinitions[id] : default;
        }
    }
}