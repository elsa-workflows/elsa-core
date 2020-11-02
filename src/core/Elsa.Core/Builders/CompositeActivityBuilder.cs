using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using NetBox.Extensions;

namespace Elsa.Builders
{
    public class CompositeActivityBuilder : ActivityBuilder, ICompositeActivityBuilder
    {
        private readonly IActivityActivator _activityActivator;
        private readonly Func<ICompositeActivityBuilder> _workflowBuilderFactory;

        public CompositeActivityBuilder(
            IIdGenerator idGenerator, 
            IActivityActivator activityActivator,
            IServiceProvider serviceProvider)
        {
            _activityActivator = activityActivator;
            ServiceProvider = serviceProvider;
            ActivityId = idGenerator.Generate();
            ActivityBuilders = new List<IActivityBuilder>();
            ConnectionBuilders = new List<IConnectionBuilder>();
            
            _workflowBuilderFactory = () =>
            {
                var builder = serviceProvider.GetRequiredService<ICompositeActivityBuilder>();
                builder.WorkflowBuilder = WorkflowBuilder;
                return builder;
            };
        }

        public IServiceProvider ServiceProvider { get; }
        protected IList<IActivityBuilder> ActivityBuilders { get; }
        protected IList<IConnectionBuilder> ConnectionBuilders { get; }

        public IReadOnlyCollection<IActivityBuilder> Activities => ActivityBuilders.ToList().AsReadOnly();

        public IActivityBuilder New(Type activityType, IDictionary<string, IActivityPropertyValueProvider>? propertyValueProviders = default)
        {
            var activityBuilder = new ActivityBuilder(activityType, this, propertyValueProviders);
            return activityBuilder;
        }

        public IActivityBuilder New<T>(
            Action<IActivityBuilder>? branch = default,
            IDictionary<string, IActivityPropertyValueProvider>? propertyValueProviders = default)
            where T : class, IActivity =>
            New(typeof(T), propertyValueProviders);

        public IActivityBuilder New<T>(
            Action<ISetupActivity<T>>? setup = default,
            Action<IActivityBuilder>? branch = default) where T : class, IActivity
        {
            var propertyValuesBuilder = new SetupActivity<T>();
            setup?.Invoke(propertyValuesBuilder);

            var valueProviders = propertyValuesBuilder.ValueProviders.ToDictionary(
                x => x.Key,
                x => (IActivityPropertyValueProvider)new DelegateActivityPropertyValueProvider(x.Value));

            return New<T>(branch, valueProviders);
        }

        public IActivityBuilder StartWith<T>(
            Action<ISetupActivity<T>>? setup = default,
            Action<IActivityBuilder>? branch = default) where T : class, IActivity
        {
            var activityBuilder = New(setup, branch);
            return Add(activityBuilder, branch);
        }

        public IActivityBuilder StartWith<T>(Action<IActivityBuilder>? branch = default)
            where T : class, IActivity =>
            Add<T>(branch);

        public IActivityBuilder Add<T>(
            Action<ISetupActivity<T>>? setup = default,
            Action<IActivityBuilder>? branch = default) where T : class, IActivity
        {
            var activityBuilder = New(setup, branch);
            return Add(activityBuilder, branch);
        }

        public IActivityBuilder Add<T>(
            Action<IActivityBuilder>? branch = default,
            IDictionary<string, IActivityPropertyValueProvider>? propertyValueProviders = default)
            where T : class, IActivity
        {
            var activityBuilder = new ActivityBuilder(typeof(T), this, propertyValueProviders);
            return Add(activityBuilder);
        }

        public IActivityBuilder Add(
            IActivityBuilder activityBuilder,
            Action<IActivityBuilder>? branch = default)
        {
            branch?.Invoke(activityBuilder);
            ActivityBuilders.Add(activityBuilder);
            return activityBuilder;
        }
        
        public override IActivityBuilder Then<T>(Action<ISetupActivity<T>>? setup = null, Action<IActivityBuilder>? branch = null) => StartWith(setup, branch);

        public override IActivityBuilder Then(IActivityBuilder targetActivity) => Add(targetActivity);

        public override IActivityBuilder Then<T>(Action<IActivityBuilder>? branch = null) => StartWith<T>(branch);

        public IConnectionBuilder Connect(
            IActivityBuilder source,
            IActivityBuilder target,
            string outcome = OutcomeNames.Done) =>
            Connect(() => source, () => target, outcome);


        public IConnectionBuilder Connect(
            Func<IActivityBuilder> source,
            Func<IActivityBuilder> target,
            string outcome = OutcomeNames.Done)
        {
            var connectionBuilder = new ConnectionBuilder(this, source, target, outcome);
            ConnectionBuilders.Add(connectionBuilder);
            return connectionBuilder;
        }
        
        public ICompositeActivityBlueprint Build(string activityIdPrefix = "activity")
        {
            var activityBuilders = ActivityBuilders.ToList();
            var activityBlueprints = new List<IActivityBlueprint>();
            var connections = new List<IConnection>();
            var activityPropertyProviders = new Dictionary<string, IDictionary<string, IActivityPropertyValueProvider>>();
        
            // Assign automatic ids to activity builders
            var index = 0;
        
            foreach (var activityBuilder in activityBuilders.Where(x => string.IsNullOrWhiteSpace(x.ActivityId)))
                activityBuilder.ActivityId = $"{activityIdPrefix}-{++index}";
        
            activityBlueprints.AddRange(activityBuilders.Select(BuildActivityBlueprint));
        
            // Build composite activities.
            var compositeActivityBuilders = activityBuilders.Where(x => typeof(CompositeActivity).IsAssignableFrom(x.ActivityType));
            BuildCompositeActivities(compositeActivityBuilders, activityBlueprints, connections, activityPropertyProviders);
        
            var activityBlueprintDictionary = activityBlueprints.ToDictionary(x => x.Id);
        
            connections.AddRange(ConnectionBuilders.Select(x => new Connection(activityBlueprintDictionary[x.Source().ActivityId], activityBlueprintDictionary[x.Target().ActivityId], x.Outcome)));
        
            activityPropertyProviders.AddRange(
                activityBuilders
                    .Select(x => (x.ActivityId, x.PropertyValueProviders))
                    .ToDictionary(x => x.ActivityId!, x => x.PropertyValueProviders!));

            return new CompositeActivityBlueprint
            {
                Id = ActivityId,
                Connections = connections,
                Activities = activityBlueprints,
                ActivityPropertyProviders = new ActivityPropertyProviders(activityPropertyProviders)
            };
        }
        
        private void BuildCompositeActivities(
            IEnumerable<IActivityBuilder> compositeActivityBuilders,
            ICollection<IActivityBlueprint> activityBlueprints,
            ICollection<IConnection> connections,
            IDictionary<string, IDictionary<string, IActivityPropertyValueProvider>> activityPropertyProviders)
        {
            foreach (var activityBuilder in compositeActivityBuilders)
            {
                var compositeActivity = (CompositeActivity)_activityActivator.ActivateActivity(activityBuilder.ActivityType.Name);
                var workflowBuilder = _workflowBuilderFactory();
        
                compositeActivity.Build(workflowBuilder);
        
                var workflow = workflowBuilder.Build($"{activityBuilder.ActivityId}:activity");
                var activityDictionary = workflow.Activities.ToDictionary(x => x.Id);
        
                activityBlueprints.AddRange(workflow.Activities);
                connections.AddRange(workflow.Connections.Select(x => new Connection(activityDictionary[x.Source.Activity.Id], activityDictionary[x.Target.Activity.Id], x.Source.Outcome)));
                activityPropertyProviders.AddRange(workflow.ActivityPropertyProviders);
        
                var compositeActivityBlueprint = (ICompositeActivityBlueprint)activityBlueprints.Single(x => x.Id == activityBuilder.ActivityId);
                compositeActivityBlueprint.Activities = workflow.Activities;
                compositeActivityBlueprint.Connections = workflow.Connections;
                compositeActivityBlueprint.ActivityPropertyProviders = workflow.ActivityPropertyProviders;
            }
        }
        
        private IActivityBlueprint BuildActivityBlueprint(IActivityBuilder builder, int index)
        {
            var isComposite = typeof(CompositeActivity).IsAssignableFrom(builder.ActivityType);
            return isComposite
                ? new CompositeActivityBlueprint(builder.ActivityId, builder.Name, builder.ActivityType.Name, builder.PersistWorkflow, builder.BuildActivityAsync())
                : new ActivityBlueprint(builder.ActivityId, builder.Name, builder.ActivityType.Name, builder.PersistWorkflow, builder.BuildActivityAsync());
        }
    }
}