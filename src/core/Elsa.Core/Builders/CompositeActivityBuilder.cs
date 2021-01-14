using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using NetBox.Extensions;

namespace Elsa.Builders
{
    [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
    public class CompositeActivityBuilder : ActivityBuilder, ICompositeActivityBuilder
    {
        private readonly Func<ICompositeActivityBuilder> _compositeActivityBuilderFactory;

        public CompositeActivityBuilder(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            ActivityBuilders = new List<IActivityBuilder>();
            ConnectionBuilders = new List<IConnectionBuilder>();

            _compositeActivityBuilderFactory = () =>
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

        public IActivityBuilder New(Type activityType, IDictionary<string, IActivityPropertyValueProvider>? propertyValueProviders = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default)
        {
            var activityBuilder = new ActivityBuilder(activityType, this, propertyValueProviders, lineNumber, sourceFile);
            return activityBuilder;
        }

        public IActivityBuilder New<T>(
            Action<IActivityBuilder>? branch = default,
            IDictionary<string, IActivityPropertyValueProvider>? propertyValueProviders = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default)
            where T : class, IActivity =>
            New(typeof(T), propertyValueProviders, lineNumber, sourceFile);

        public IActivityBuilder New<T>(
            Action<ISetupActivity<T>>? setup = default,
            Action<IActivityBuilder>? branch = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) where T : class, IActivity
        {
            var propertyValuesBuilder = new SetupActivity<T>();
            setup?.Invoke(propertyValuesBuilder);

            var valueProviders = propertyValuesBuilder.ValueProviders.ToDictionary(
                x => x.Key,
                x => (IActivityPropertyValueProvider) new DelegateActivityPropertyValueProvider(x.Value));

            return New<T>(branch, valueProviders, lineNumber, sourceFile);
        }

        public IActivityBuilder StartWith<T>(
            Action<ISetupActivity<T>>? setup = default,
            Action<IActivityBuilder>? branch = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) where T : class, IActivity
        {
            var activityBuilder = New(setup, branch, lineNumber, sourceFile);
            return Add(activityBuilder, branch);
        }

        public IActivityBuilder StartWith<T>(Action<IActivityBuilder>? branch = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default)
            where T : class, IActivity =>
            Add<T>(branch, null, lineNumber, sourceFile);

        public IActivityBuilder Add<T>(
            Action<ISetupActivity<T>>? setup = default,
            Action<IActivityBuilder>? branch = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) where T : class, IActivity
        {
            var activityBuilder = New(setup, branch, lineNumber, sourceFile);
            return Add(activityBuilder, branch);
        }

        public IActivityBuilder Add<T>(
            Action<IActivityBuilder>? branch = default,
            IDictionary<string, IActivityPropertyValueProvider>? propertyValueProviders = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default)
            where T : class, IActivity
        {
            var activityBuilder = new ActivityBuilder(typeof(T), this, propertyValueProviders, lineNumber, sourceFile);
            return Add(activityBuilder, branch);
        }

        public IActivityBuilder Add(
            IActivityBuilder activityBuilder,
            Action<IActivityBuilder>? branch = default)
        {
            branch?.Invoke(activityBuilder);
            ActivityBuilders.Add(activityBuilder);
            return activityBuilder;
        }

        public override IActivityBuilder Then<T>(Action<ISetupActivity<T>>? setup = null, Action<IActivityBuilder>? branch = null, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            StartWith(setup, branch, lineNumber, sourceFile);

        public override IActivityBuilder Then(IActivityBuilder targetActivity) => Add(targetActivity);

        public override IActivityBuilder Then<T>(Action<IActivityBuilder>? branch = null, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) => StartWith<T>(branch, lineNumber, sourceFile);

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
            var compositeActivityBlueprint = new CompositeActivityBlueprint
            {
                Id = ActivityId
            };

            var activityBuilders = ActivityBuilders.ToList();
            var activityBlueprints = new List<IActivityBlueprint>();
            var connections = new List<IConnection>();
            var activityPropertyProviders = new Dictionary<string, IDictionary<string, IActivityPropertyValueProvider>>();

            // Assign automatic ids to activity builders
            var index = 0;

            foreach (var activityBuilder in activityBuilders)
            {
                var activityId = activityBuilder.ActivityId;
                activityBuilder.ActivityId = string.IsNullOrWhiteSpace(activityId) ? $"{activityIdPrefix}-{++index}" : $"{activityIdPrefix}-{activityId}";
            }

            activityBlueprints.AddRange(activityBuilders.Select(x => BuildActivityBlueprint(x, compositeActivityBlueprint)));
            var activityBlueprintDictionary = activityBlueprints.ToDictionary(x => x.Id);
            connections.AddRange(ConnectionBuilders.Select(x => new Connection(activityBlueprintDictionary[x.Source().ActivityId], activityBlueprintDictionary[x.Target().ActivityId], x.Outcome)));

            compositeActivityBlueprint.Connections = connections;
            compositeActivityBlueprint.Activities = activityBlueprints;
            compositeActivityBlueprint.ActivityPropertyProviders = new ActivityPropertyProviders(activityPropertyProviders);

            // Build composite activities.
            var compositeActivityBuilders = activityBuilders.Where(x => typeof(CompositeActivity).IsAssignableFrom(x.ActivityType));
            BuildCompositeActivities(compositeActivityBuilders, activityBlueprints, connections, activityPropertyProviders);

            activityPropertyProviders.AddRange(
                activityBuilders
                    .Select(x => (x.ActivityId, x.PropertyValueProviders))
                    .ToDictionary(x => x.ActivityId!, x => x.PropertyValueProviders!));

            return compositeActivityBlueprint;
        }

        private void BuildCompositeActivities(
            IEnumerable<IActivityBuilder> compositeActivityBuilders,
            ICollection<IActivityBlueprint> activityBlueprints,
            ICollection<IConnection> connections,
            IDictionary<string, IDictionary<string, IActivityPropertyValueProvider>> activityPropertyProviders)
        {
            using var scope = ServiceProvider.CreateScope();
            foreach (var activityBuilder in compositeActivityBuilders)
            {
                var compositeActivity = (CompositeActivity) ActivatorUtilities.CreateInstance(scope.ServiceProvider, activityBuilder.ActivityType);
                var compositeActivityBuilder = _compositeActivityBuilderFactory();
                compositeActivityBuilder.ActivityId = activityBuilder.ActivityId;
                compositeActivity.Build(compositeActivityBuilder);

                var compositeActivityBlueprint = compositeActivityBuilder.Build($"{activityBuilder.ActivityId}:activity");
                var activityDictionary = compositeActivityBlueprint.Activities.ToDictionary(x => x.Id);

                activityBlueprints.AddRange(compositeActivityBlueprint.Activities);
                connections.AddRange(compositeActivityBlueprint.Connections.Select(x => new Connection(activityDictionary[x.Source.Activity.Id], activityDictionary[x.Target.Activity.Id], x.Source.Outcome)));
                activityPropertyProviders.AddRange(compositeActivityBlueprint.ActivityPropertyProviders);

                compositeActivityBlueprint.Activities = compositeActivityBlueprint.Activities;
                compositeActivityBlueprint.Connections = compositeActivityBlueprint.Connections;
                compositeActivityBlueprint.ActivityPropertyProviders = compositeActivityBlueprint.ActivityPropertyProviders;

                // Connect the composite activity to its starting activities.
                var startActivities = compositeActivityBlueprint.GetStartActivities().ToList();
                connections.AddRange(startActivities.Select(x => new Connection(compositeActivityBlueprint, x, CompositeActivity.Enter)));
            }
        }

        private IActivityBlueprint BuildActivityBlueprint(IActivityBuilder builder, ICompositeActivityBlueprint parent)
        {
            var isComposite = typeof(CompositeActivity).IsAssignableFrom(builder.ActivityType);
            return isComposite
                ? new CompositeActivityBlueprint(builder.ActivityId, parent, builder.Name, builder.DisplayName, builder.Description, builder.ActivityType.Name, builder.PersistWorkflowEnabled, builder.LoadWorkflowContextEnabled,
                    builder.SaveWorkflowContextEnabled, builder.PersistOutputEnabled, builder.Source)
                : new ActivityBlueprint(builder.ActivityId, parent, builder.Name, builder.DisplayName, builder.Description, builder.ActivityType.Name, builder.PersistWorkflowEnabled, builder.LoadWorkflowContextEnabled,
                    builder.SaveWorkflowContextEnabled, builder.PersistOutputEnabled, builder.Source);
        }
    }
}