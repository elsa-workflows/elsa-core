using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services.Extensions;
using Elsa.Services.Models;
using Newtonsoft.Json.Linq;
using NodaTime;
using Connection = Elsa.Services.Models.Connection;

namespace Elsa.Services
{
    public class WorkflowFactory : IWorkflowFactory
    {
        private readonly IActivityResolver activityResolver;
        private readonly Func<IWorkflowBuilder> workflowBuilder;
        private readonly IClock clock;
        private readonly IIdGenerator idGenerator;

        public WorkflowFactory(
            IActivityResolver activityResolver,
            Func<IWorkflowBuilder> workflowBuilder,
            IClock clock,
            IIdGenerator idGenerator)
        {
            this.activityResolver = activityResolver;
            this.workflowBuilder = workflowBuilder;
            this.clock = clock;
            this.idGenerator = idGenerator;
        }

        public Workflow CreateWorkflow<T>(
            Variables input = default,
            WorkflowInstance workflowInstance = default,
            string correlationId = default) where T : IWorkflow, new()
        {
            var workflowDefinition = workflowBuilder().Build<T>();
            return CreateWorkflow(workflowDefinition, input, workflowInstance, correlationId);
        }
        
        public WorkflowBlueprint CreateWorkflowBlueprint(WorkflowDefinitionVersion definition)
        {
            var activities = CreateActivities(definition.Activities).ToList();
            var connections = CreateConnections(definition.Connections, activities).ToList();
            return new WorkflowBlueprint(definition.DefinitionId, definition.Version, definition.IsSingleton, definition.IsDisabled, definition.Name, definition.Description, activities, connections);
        }

        public Workflow CreateWorkflow(
            WorkflowBlueprint blueprint,
            Variables input = default,
            WorkflowInstance workflowInstance = default,
            string correlationId = default)
        {
            if (blueprint.IsDisabled)
                throw new InvalidOperationException("Cannot instantiate disabled workflow definitions.");

            var activities = blueprint.Activities.ToList();
            var connections = blueprint.Connections;
            var id = idGenerator.Generate();
            var workflow = new Workflow(
                id,
                blueprint,
                clock.GetCurrentInstant(),
                input,
                correlationId);

            if (workflowInstance != default)
                workflow.Initialize(workflowInstance);

            return workflow;
        }

        private IEnumerable<Connection> CreateConnections(
            IEnumerable<ConnectionDefinition> connectionBlueprints,
            IEnumerable<IActivity> activities)
        {
            var activityDictionary = activities.ToDictionary(x => x.Id);
            return connectionBlueprints.Select(x => CreateConnection(x, activityDictionary));
        }

        private IEnumerable<IActivity> CreateActivities(IEnumerable<ActivityDefinition> activityDefinitions)
        {
            return activityDefinitions.Select(CreateActivity);
        }

        private IActivity CreateActivity(ActivityDefinition definition)
        {
            var activity = activityResolver.ResolveActivity(definition.Type);

            activity.State = new JObject(definition.State);
            activity.Id = definition.Id;

            return activity;
        }

        private Connection CreateConnection(
            ConnectionDefinition connectionDefinition,
            IDictionary<string, IActivity> activityDictionary)
        {
            var source = activityDictionary[connectionDefinition.SourceActivityId];
            var target = activityDictionary[connectionDefinition.DestinationActivityId];
            return new Connection(source, target, connectionDefinition.Outcome);
        }
    }
}