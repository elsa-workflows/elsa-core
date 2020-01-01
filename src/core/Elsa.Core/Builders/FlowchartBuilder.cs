using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.Containers;
using Elsa.Activities.Workflows.Models;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public class FlowchartBuilder : IFlowchartBuilder
    {
        private readonly IActivityResolver activityResolver;
        private readonly IIdGenerator idGenerator;
        private readonly IList<IActivityBuilder> activityBuilders = new List<IActivityBuilder>();
        private readonly IList<IConnectionBuilder> connectionBuilders = new List<IConnectionBuilder>();

        public FlowchartBuilder(
            IActivityResolver activityResolver,
            IIdGenerator idGenerator)
        {
            this.activityResolver = activityResolver;
            this.idGenerator = idGenerator;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ProcessPersistenceBehavior PersistenceBehavior { get; set; }
        public bool DeleteCompletedWorkflows { get; set; }
        public IReadOnlyList<IActivityBuilder> Activities => activityBuilders.ToList().AsReadOnly();

        public IFlowchartBuilder WithId(string id)
        {
            Id = id;
            return this;
        }

        public IFlowchartBuilder WithName(string name)
        {
            Name = name;
            return this;
        }

        public IFlowchartBuilder WithDescription(string description)
        {
            Description = description;
            return this;
        }

        public IActivityBuilder Add<T>(Action<T>? setupActivity = default, string? name = default) where T : class, IActivity
        {
            var activity = activityResolver.ResolveActivity(setupActivity);
            //var activityBuilder = new ActivityBuilder(this, activity);

            //activityBuilders.Add(activityBuilder);
            //return activityBuilder;
            throw new NotImplementedException();
        }

        public IConnectionBuilder Connect(
            IActivityBuilder source,
            IActivityBuilder target,
            string? outcome = default)
        {
            return Connect(() => source, () => target, outcome);
        }

        public IConnectionBuilder Connect(
            Func<IActivityBuilder> source,
            Func<IActivityBuilder> target,
            string? outcome = default)
        {
            var connectionBuilder = new ConnectionBuilder(this, source, target, outcome);

            connectionBuilders.Add(connectionBuilder);
            return connectionBuilder;
        }


        public IActivityBuilder StartWith<T>(Action<T> setupActivity, string name = default) where T : class, IActivity
        {
            return Add(setupActivity, name);
        }

        public Flowchart Build()
        {
            // Generate deterministic activity ids.
            var id = 1;
            foreach (var activityBuilder in activityBuilders)
            {
                if (activityBuilder.Activity.Id == null)
                    activityBuilder.Activity.Id = $"activity-{id++}";
            }

            var activities = activityBuilders.Select(x => x.BuildActivity()).ToList();
            var connectionDefinitions = connectionBuilders.Select(x => x.BuildConnection());
            var connections = CreateConnections(connectionDefinitions, activities);
            var definitionId = !string.IsNullOrWhiteSpace(Id) ? Id : idGenerator.Generate();

            return new Flowchart();
        }
        
        private IEnumerable<Connection> CreateConnections(IEnumerable<ConnectionDefinition> connectionDefinitions, IEnumerable<IActivity> activities)
        {
            var activityDictionary = activities.ToDictionary(x => x.Id);
            return connectionDefinitions.Select(x => CreateConnection(x, activityDictionary));
        }
        
        private Connection CreateConnection(ConnectionDefinition connectionDefinition, IDictionary<string, IActivity> activityDictionary)
        {
            var source = activityDictionary[connectionDefinition.SourceActivityId];
            var target = activityDictionary[connectionDefinition.TargetActivityId];
            return new Connection(source, target, connectionDefinition.Outcome);
        }
    }
}