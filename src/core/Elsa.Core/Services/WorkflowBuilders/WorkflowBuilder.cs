using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Serialization.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Newtonsoft.Json.Linq;
using Connection = Elsa.Services.Models.Connection;

namespace Elsa.Core.Services.WorkflowBuilders
{
    public class WorkflowBuilder : IWorkflowBuilder
    {
        private readonly IActivityResolver activityResolver;
        private readonly IList<IActivityBuilder> activityBuilders = new List<IActivityBuilder>();
        private readonly IList<IConnectionBuilder> connectionBuilders = new List<IConnectionBuilder>();
        private readonly IIdGenerator idGenerator;

        public WorkflowBuilder(IActivityResolver activityResolver, IIdGenerator idGenerator)
        {
            this.activityResolver = activityResolver;
            this.idGenerator = idGenerator;
        }

        public string Id { get; private set; }
        public IReadOnlyList<IActivityBuilder> Activities => activityBuilders.ToList().AsReadOnly();

        public IWorkflowBuilder WithId(string id)
        {
            Id = id;
            return this;
        }
        
        public IActivityBuilder Add<T>(Action<T> setupActivity, string id = null) where T : class, IActivity
        {
            var activity = activityResolver.ResolveActivity(setupActivity);
            var activityBlueprint = ActivityBlueprint.FromActivity(activity);
            var activityBuilder = new ActivityBuilder(this, activityBlueprint, id);

            if (id != null)
                activity.Id = id;
            
            activityBuilders.Add(activityBuilder);
            return activityBuilder;
        }

        public IConnectionBuilder Connect(IActivityBuilder source, IActivityBuilder target, string outcome = null)
        {
            return Connect(() => source, () => target, outcome);
        }

        public IConnectionBuilder Connect(Func<IActivityBuilder> source, Func<IActivityBuilder> target, string outcome = null)
        {
            var connectionBuilder = new ConnectionBuilder(this, source, target, outcome);

            connectionBuilders.Add(connectionBuilder);
            return connectionBuilder;
        }


        public IActivityBuilder StartWith<T>(Action<T> setupActivity, string id = null) where T : class, IActivity
        {
            return Add(setupActivity, id);
        }

        public WorkflowBlueprint Build()
        {
            var activities = activityBuilders.Select(x => x.BuildActivity()).ToList();
            var connections = connectionBuilders.Select(x => x.BuildConnection()).ToList();

            // Generate deterministic activity ids.
            var id = 1;
            foreach (var activity in activities)
            {
                if (activity.Id == null)
                    activity.Id = $"activity-{id++}";
            }
            
            return new WorkflowBlueprint(activities, connections, Variables.Empty)
            {
                Id = Id ?? idGenerator.Generate()
            };
        }
    }
}