using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services.Extensions;
using Elsa.Services.Models;
using Newtonsoft.Json.Linq;
using Connection = Elsa.Services.Models.Connection;

namespace Elsa.Services
{
    public class WorkflowFactory : IWorkflowFactory
    {
        private readonly IActivityResolver activityResolver;
        private readonly IWorkflowBuilder workflowBuilder;
        private readonly IIdGenerator idGenerator;

        public WorkflowFactory(IActivityResolver activityResolver, IWorkflowBuilder workflowBuilder, IIdGenerator idGenerator)
        {
            this.activityResolver = activityResolver;
            this.workflowBuilder = workflowBuilder;
            this.idGenerator = idGenerator;
        }

        public Workflow CreateWorkflow<T>(Variables input = null, WorkflowInstance workflowInstance = null) where T : IWorkflow, new()
        {
            var workflowDefinition = workflowBuilder.Build<T>();
            return CreateWorkflow(workflowDefinition, input, workflowInstance);
        }

        public Workflow CreateWorkflow(WorkflowDefinition definition, Variables input = null, WorkflowInstance workflowInstance = null)
        {
            var activities = CreateActivities(definition.Activities).ToList();
            var connections = CreateConnections(definition.Connections, activities);
            var id = idGenerator.Generate();
            var workflow = new Workflow(id, definition.Id, definition.Version, activities, connections, input);

            if(workflowInstance != null)
                workflow.Initialize(workflowInstance);
            
            return workflow;
        }

        private IEnumerable<Connection> CreateConnections(IEnumerable<ConnectionDefinition> connectionBlueprints, IEnumerable<IActivity> activities)
        {
            var activityDictionary = activities.ToDictionary(x => x.Id);
            return connectionBlueprints.Select(x => CreateConnection(x, activityDictionary));
        }

        private IEnumerable<IActivity> CreateActivities(IEnumerable<ActivityDefinition> activityBlueprints)
        {
            return activityBlueprints.Select(CreateActivity);
        }

        private IActivity CreateActivity(ActivityDefinition definition)
        {
            var activity = activityResolver.ResolveActivity(definition.Type);

            activity.State = new JObject(definition.State);
            activity.Id = definition.Id;
            
            return activity;
        }
        
        private Connection CreateConnection(ConnectionDefinition connectionDefinition, IDictionary<string, IActivity> activityDictionary)
        {
            var source = activityDictionary[connectionDefinition.SourceActivityId];
            var target = activityDictionary[connectionDefinition.DestinationActivityId];
            return new Connection(source, target, connectionDefinition.Outcome);   
        }
    }
}