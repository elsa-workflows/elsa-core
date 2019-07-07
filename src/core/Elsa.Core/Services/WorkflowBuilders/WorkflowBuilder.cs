using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Core.Services.WorkflowBuilders
{
    public class WorkflowBuilder : IWorkflowBuilder
    {
        private readonly IActivityResolver activityResolver;
        private readonly IList<IActivityBuilder> activityBuilders = new List<IActivityBuilder>();
        private readonly IList<IConnectionBuilder> connectionBuilders = new List<IConnectionBuilder>();

        public WorkflowBuilder(IActivityResolver activityResolver)
        {
            this.activityResolver = activityResolver;
        }

        public string Id { get; set; }
        public bool IsSingleton { get; set; }
        public IReadOnlyList<IActivityBuilder> Activities => activityBuilders.ToList().AsReadOnly();

        public IWorkflowBuilder WithId(string id)
        {
            Id = id;
            return this;
        }

        public IWorkflowBuilder AsSingleton(bool value)
        {
            IsSingleton = value;
            return this;
        }

        public IActivityBuilder Add<T>(Action<T> setupActivity = default, string id = default) where T : class, IActivity
        {
            var activity = activityResolver.ResolveActivity(setupActivity);
            var activityBlueprint = ActivityDefinition.FromActivity(activity);
            var activityBuilder = new ActivityBuilder(this, activityBlueprint, id);

            if (id != null)
                activity.Id = id;
            
            activityBuilders.Add(activityBuilder);
            return activityBuilder;
        }

        public IConnectionBuilder Connect(IActivityBuilder source, IActivityBuilder target, string outcome = default)
        {
            return Connect(() => source, () => target, outcome);
        }

        public IConnectionBuilder Connect(Func<IActivityBuilder> source, Func<IActivityBuilder> target, string outcome = default)
        {
            var connectionBuilder = new ConnectionBuilder(this, source, target, outcome);

            connectionBuilders.Add(connectionBuilder);
            return connectionBuilder;
        }


        public IActivityBuilder StartWith<T>(Action<T> setupActivity, string id = null) where T : class, IActivity
        {
            return Add(setupActivity, id);
        }

        public WorkflowDefinition Build()
        {
            // Generate deterministic activity ids.
            var id = 1;
            foreach (var activityBuilder in activityBuilders)
            {
                if (activityBuilder.Id == null)
                    activityBuilder.Id = $"activity-{id++}";
            }
            
            var activities = activityBuilders.Select(x => x.BuildActivity()).ToList();
            var connections = connectionBuilders.Select(x => x.BuildConnection()).ToList();

            return new WorkflowDefinition(Id, activities, connections, IsSingleton, Variables.Empty);
        }
    }
}