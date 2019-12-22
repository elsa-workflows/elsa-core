using System;
using System.Linq;
using Elsa.Metadata;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.WorkflowBuilders
{
    public class ActivityBuilder : IActivityBuilder
    {
        public ActivityBuilder(WorkflowBuilder workflowBuilder, IActivity activity)
        {
            WorkflowBuilder = workflowBuilder;
            Activity = activity;
        }

        public WorkflowBuilder WorkflowBuilder { get; }
        public IActivity Activity { get; }

        public IActivityBuilder StartWith<T>(Action<T> setup = default, string name = default) where T : class, IActivity
        {
            return Add(setup, name);
        }

        public IActivityBuilder Add<T>(Action<T> setup = default, string name = default) where T : class, IActivity
        {
            return WorkflowBuilder.Add(setup, name);
        }

        public IOutcomeBuilder When(string outcome)
        {
            return new OutcomeBuilder(WorkflowBuilder, this, outcome);
        }

        public IActivityBuilder Then<T>(Action<T> setup = null, Action<IActivityBuilder> branch = null, string name = default) where T : class, IActivity
        {
            var activityBuilder = When(null).Then(setup, branch, name);
            return activityBuilder;
        }

        public IActivityBuilder WithName(string name)
        {
            Activity.Name = name;
            return this;
        }
        
        public IActivityBuilder WithDisplayName(string displayName)
        {
            Activity.DisplayName = displayName;
            return this;
        }

        public IActivityBuilder WithDescription(string description)
        {
            Activity.Description = description;
            return this;
        }

        public IWorkflowBuilder Then(string activityName)
        {
            WorkflowBuilder.Connect(
                () => this,
                () => WorkflowBuilder.Activities.First(x => x.Activity.Name== activityName)
            );

            return WorkflowBuilder;
        }

        public IActivityBuilder Then(IActivityBuilder targetActivity)
        {
            WorkflowBuilder.Connect(this, targetActivity);
            return this;
        }

        public IActivity BuildActivity() => Activity;

        public WorkflowBlueprint Build() => WorkflowBuilder.Build();
    }
}