using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Services.Extensions;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public class WorkflowRegistry : IWorkflowRegistry
    {
        private readonly Func<IWorkflowBuilder> workflowBuilderFactory;
        private readonly IDictionary<(string, int), WorkflowDefinition> workflowDefinitions;

        public WorkflowRegistry(Func<IWorkflowBuilder> workflowBuilderFactory)
        {
            this.workflowBuilderFactory = workflowBuilderFactory;
            workflowDefinitions = new Dictionary<(string, int), WorkflowDefinition>();
        }
        
        public void RegisterWorkflow(WorkflowDefinition definition)
        {
            workflowDefinitions[(definition.Id, definition.Version)] = definition;
        }

        public WorkflowDefinition RegisterWorkflow<T>() where T : IWorkflow, new()
        {
            var definition = workflowBuilderFactory().Build<T>();
            RegisterWorkflow(definition);
            return definition;
        }

        public IEnumerable<(WorkflowDefinition, ActivityDefinition)> ListByStartActivity(string activityType)
        {
            var query =
                from workflow in workflowDefinitions.Values
                from activity in workflow.GetStartActivities()
                where activity.Type == activityType
                select (workflow, activity);

            return query.Distinct();
        }

        public WorkflowDefinition GetById(string id, int version)
        {
            var identifier = (id, version);
            return workflowDefinitions.ContainsKey(identifier) ? workflowDefinitions[identifier] : default;
        }
    }
}