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
        private readonly IActivityActivator _activityActivator;

        public ActivityBuilder(
            Type activityType,
            Action<IActivity>? setupActivity,
            IWorkflowBuilder workflowBuilder,
            IActivityActivator activityActivator,
            IDictionary<string, IActivityPropertyValueProvider>? propertyValueProviders)
        {
            _activityActivator = activityActivator;
            ActivityType = activityType;
            SetupActivity = setupActivity;
            WorkflowBuilder = workflowBuilder;
            PropertyValueProviders = propertyValueProviders;
        }

        public Type ActivityType { get; }
        public Action<IActivity>? SetupActivity { get; }
        public IWorkflowBuilder WorkflowBuilder { get; }
        public string? ActivityId { get; private set; }
        public IDictionary<string, IActivityPropertyValueProvider>? PropertyValueProviders { get; }

        public IActivityBuilder Add<T>(
            Action<ISetupActivity<T>>? setup = default)
            where T : class, IActivity => WorkflowBuilder.Add(setup);

        public IOutcomeBuilder When(string outcome) => new OutcomeBuilder(WorkflowBuilder, this, outcome);

        public IActivityBuilder Then<T>(
            Action<ISetupActivity<T>>? setup = null,
            Action<IActivityBuilder>? branch = null)
            where T : class, IActivity => When(OutcomeNames.Done).Then(setup, branch);

        public IActivityBuilder Then<T>(
            Action<T> setup,
            Action<IActivityBuilder>? branch = null)
            where T : class, IActivity => When(OutcomeNames.Done).Then(setup, branch);

        public IActivityBuilder Then<T>(Action<IActivityBuilder>? branch = null)
            where T : class, IActivity => When(OutcomeNames.Done).Then<T>(branch);

        public IActivityBuilder Then(IActivityBuilder targetActivity)
        {
            WorkflowBuilder.Connect(this, targetActivity);
            return this;
        }

        public IActivityBuilder WithId(string id)
        {
            ActivityId = id;
            return this;
        }

        public Func<ActivityExecutionContext, CancellationToken, Task<IActivity>> BuildActivityAsync() =>
            async (context, cancellationToken) =>
            {
                var activity = _activityActivator.ActivateActivity(SetupActivity);
                await context.SetActivityPropertiesAsync(activity, cancellationToken);
                return activity;
            };

        public IWorkflowBlueprint Build() => WorkflowBuilder.Build();
    }
}