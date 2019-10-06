using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Services.Extensions;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Services
{
    public class WorkflowRegistry : IWorkflowRegistry
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IDictionary<(string, int), WorkflowDefinitionVersion> workflowDefinitions;

        public WorkflowRegistry(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            workflowDefinitions = new Dictionary<(string, int), WorkflowDefinitionVersion>();
        }

        public void RegisterWorkflow(WorkflowDefinitionVersion definition)
        {
            workflowDefinitions[(definition.DefinitionId, definition.Version)] = definition;
        }

        public WorkflowDefinitionVersion RegisterWorkflow<T>() where T : IWorkflow, new()
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var workflowBuilder = scope.ServiceProvider.GetRequiredService<IWorkflowBuilder>();
                var definition = workflowBuilder.Build<T>();
                RegisterWorkflow(definition);
                return definition;
            }
        }

        public IEnumerable<(WorkflowDefinitionVersion, ActivityDefinition)> ListByStartActivity(string activityType)
        {
            var query =
                from workflow in workflowDefinitions.Values
                from activity in workflow.GetStartActivities()
                where activity.Type == activityType
                select (workflow, activity);

            return query.Distinct();
        }

        public WorkflowDefinitionVersion GetById(string id, int version)
        {
            var identifier = (id, version);
            return workflowDefinitions.ContainsKey(identifier) ? workflowDefinitions[identifier] : default;
        }
    }
}