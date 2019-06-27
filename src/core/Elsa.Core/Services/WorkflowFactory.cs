using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Serialization.Models;
using Elsa.Services;
using Elsa.Services.Extensions;
using Elsa.Services.Models;
using Newtonsoft.Json.Linq;
using Connection = Elsa.Services.Models.Connection;

namespace Elsa.Core.Services
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
            return new Workflow(id, definition.Id, activities, connections, input, workflowInstance);
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
            var activity = activityResolver.ResolveActivity(definition.TypeName);

            activity.State = new JObject(definition.State);
            activity.Id = definition.Id;
            
            return activity;
        }
        
        private Connection CreateConnection(ConnectionDefinition connectionDefinition, IDictionary<string, IActivity> activityDictionary)
        {
            var source = activityDictionary[connectionDefinition.Source.ActivityId];
            var target = activityDictionary[connectionDefinition.Target.ActivityId];
            return new Connection(source, target, connectionDefinition.Source.Outcome);   
        }
    }
}