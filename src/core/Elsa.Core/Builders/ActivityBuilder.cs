using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public class ActivityBuilder : IActivityBuilder
    {
        public ActivityBuilder(
            Type activityType,
            IWorkflowBuilder workflowBuilder,
            IDictionary<string, IActivityPropertyValueProvider>? propertyValueProviders)
        {
            ActivityType = activityType;
            WorkflowBuilder = workflowBuilder;
            PropertyValueProviders = propertyValueProviders;
        }

        public Type ActivityType { get; }
        public IWorkflowBuilder WorkflowBuilder { get; }
        public string ActivityId { get; set; } = default!;
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool PersistWorkflow { get; set; }
        public IDictionary<string, IActivityPropertyValueProvider>? PropertyValueProviders { get; }

        public IActivityBuilder Add<T>(
            Action<ISetupActivity<T>>? setup = default)
            where T : class, IActivity => WorkflowBuilder.Add(setup);

        public IOutcomeBuilder When(string outcome) => new OutcomeBuilder(WorkflowBuilder, this, outcome);

        public IActivityBuilder Then<T>(
            Action<ISetupActivity<T>>? setup = null,
            Action<IActivityBuilder>? branch = null)
            where T : class, IActivity => When(OutcomeNames.Done).Then(setup, branch);

        public IActivityBuilder Then<T>(Action<IActivityBuilder>? branch = null)
            where T : class, IActivity => When(OutcomeNames.Done).Then<T>(branch);

        public IActivityBuilder Then(IActivityBuilder targetActivity)
        {
            WorkflowBuilder.Connect(this, targetActivity);
            return this;
        }

        public IActivityBuilder WithId(string? id)
        {
            ActivityId = id!;
            return this;
        }

        public IActivityBuilder WithName(string? name)
        {
            Name = name;
            return this;
        }

        public Func<ActivityExecutionContext, CancellationToken, ValueTask<IActivity>> BuildActivityAsync() =>
            async (context, cancellationToken) =>
            {
                var activity = context.ActivateActivity(context.ActivityBlueprint.Type);
                activity.Id = ActivityId;
                activity.Name = Name;
                activity.Description = Description;
                await context.SetActivityPropertiesAsync(activity, cancellationToken);
                return activity;
            };

        public IWorkflowBlueprint Build() => WorkflowBuilder.Build();
    }
}