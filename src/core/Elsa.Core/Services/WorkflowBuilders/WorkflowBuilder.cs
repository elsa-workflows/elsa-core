using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Serialization.Models;
using Elsa.Services;
using Elsa.Services.Extensions;
using Elsa.Services.Models;
using Esprima.Ast;
using Newtonsoft.Json.Linq;
using Connection = Elsa.Services.Models.Connection;

namespace Elsa.Core.Services.WorkflowBuilders
{
    public class WorkflowBuilder : IWorkflowBuilder
    {
        private readonly IActivityResolver activityResolver;
        private readonly IList<ActivityBuilder> activityBuilders = new List<ActivityBuilder>();
        private readonly IList<ConnectionBuilder> connectionBuilders = new List<ConnectionBuilder>();

        public WorkflowBuilder(IActivityResolver activityResolver)
        {
            this.activityResolver = activityResolver;
        }

        public IActivityBuilder Add<T>(Action<T> setupActivity) where T : IActivity
        {
            var activity = activityResolver.ResolveActivity(setupActivity);
            var activityBuilder = new ActivityBuilder(this, activity);

            activityBuilders.Add(activityBuilder);
            return activityBuilder;
        }

        public IConnectionBuilder Connect(IActivityBuilder source, IActivityBuilder target, string outcome = null)
        {
            var connectionBuilder = new ConnectionBuilder(source, target, outcome);

            connectionBuilders.Add(connectionBuilder);
            return connectionBuilder;
        }

        public IActivityBuilder StartWith<T>(Action<T> setupActivity) where T : IActivity
        {
            return Add(setupActivity);
        }

        public Workflow Build()
        {
            var activities = activityBuilders.Select(x => x.Activity).ToList();
            var connections = connectionBuilders.Select(x => x.BuildConnection()).ToList();

            // Generate deterministic activity ids.
            var id = 1;
            foreach (var activity in activities)
            {
                if (activity.Id == null)
                    activity.Id = $"activity-{id++}";
            }

            return new Workflow(activities, connections, Variables.Empty);
        }

        public Workflow Build(WorkflowDefinition definition)
        {
            var activities =
                from activityDefinition in definition.Activities
                let activity = activityResolver.ResolveActivity(activityDefinition.TypeName, x =>
                    {
                        x.Id = activityDefinition.Id;
                        x.State = new JObject(activityDefinition.State);
                    }
                )
                select activity;
            
            var activityDictionary = activities.ToDictionary(x => x.Id);
            
            var connections =
                from connection in definition.Connections
                let source = activityDictionary[connection.Source.ActivityId]
                let target = activityDictionary[connection.Target.ActivityId]
                select new Connection(source, target, connection.Source.Outcome);

            return new Workflow(activityDictionary.Values, connections, Variables.Empty);
        }
    }
}