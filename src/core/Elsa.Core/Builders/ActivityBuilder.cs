using System;
using System.Collections.Generic;
using System.Linq;
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
            ICompositeActivityBuilder workflowBuilder,
            IDictionary<string, IActivityPropertyValueProvider>? propertyValueProviders)
        {
            ActivityType = activityType;
            WorkflowBuilder = workflowBuilder;
            PropertyValueProviders = propertyValueProviders;
        }
        
        protected ActivityBuilder()
        {
        }

        public Type ActivityType { get; protected set; } = default!;
        public ICompositeActivityBuilder WorkflowBuilder { get; set; } = default!;
        public string ActivityId { get; set; } = default!;
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool PersistWorkflow { get; set; }
        public bool LoadWorkflowContext { get; set; }
        public bool SaveWorkflowContext { get; set; }
        public IDictionary<string, IActivityPropertyValueProvider>? PropertyValueProviders { get; protected set; }

        public IActivityBuilder Add<T>(
            Action<ISetupActivity<T>>? setup = default)
            where T : class, IActivity =>
            WorkflowBuilder.Add(setup);

        public IOutcomeBuilder When(string outcome) => new OutcomeBuilder(WorkflowBuilder, this, outcome);

        public virtual IActivityBuilder Then<T>(
            Action<ISetupActivity<T>>? setup = null,
            Action<IActivityBuilder>? branch = null)
            where T : class, IActivity =>
            When(OutcomeNames.Done).Then(setup, branch);

        public virtual IActivityBuilder Then<T>(Action<IActivityBuilder>? branch = null)
            where T : class, IActivity =>
            When(OutcomeNames.Done).Then<T>(branch);

        public virtual IActivityBuilder Then(IActivityBuilder targetActivity)
        {
            WorkflowBuilder.Connect(this, targetActivity);
            return this;
        }

        public virtual IConnectionBuilder Then(string activityName) =>
            WorkflowBuilder.Connect(
                () => this,
                () => WorkflowBuilder.Activities.First(x => x.Name == activityName));

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
        
        public IActivityBuilder WithLoadWorkflowContext(bool value)
        {
            LoadWorkflowContext = value;
            return this;
        }
        
        public IActivityBuilder WithSaveWorkflowContext(bool value)
        {
            SaveWorkflowContext = value;
            return this;
        }

        public Func<ActivityExecutionContext, CancellationToken, ValueTask<IActivity>> BuildActivityAsync() =>
            async (context, cancellationToken) =>
            {
                var activity = await context.ActivateActivityAsync(cancellationToken);
                activity.Id = ActivityId;
                activity.Name = Name;
                activity.Description = Description;
                return activity;
            };
    }
}