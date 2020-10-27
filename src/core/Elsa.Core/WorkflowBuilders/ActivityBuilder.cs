using System;
using System.Linq;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.WorkflowBuilders
{
    public class ActivityBuilder : IActivityBuilder
    {
        public ActivityBuilder(WorkflowBuilder workflowBuilder, ActivityDefinition activity)
        {
            WorkflowBuilder = workflowBuilder;
            Activity = activity;
            Id = Activity.Id;
            Name = Activity.Name;
            Description = Activity.Description;
            DisplayName = Activity.DisplayName;
        }

        public WorkflowBuilder WorkflowBuilder { get; }
        public ActivityDefinition Activity { get; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string DisplayName { get; set; }

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
            Name = name;
            return this;
        }
        
        public IActivityBuilder WithDisplayName(string displayName)
        {
            DisplayName = displayName;
            return this;
        }

        public IActivityBuilder WithDescription(string description)
        {
            Description = description;
            return this;
        }

        public IWorkflowBuilder Then(string activityName)
        {
            WorkflowBuilder.Connect(
                () => this,
                () => WorkflowBuilder.Activities.First(x => x.Name== activityName)
            );

            return WorkflowBuilder;
        }

        public IActivityBuilder Then(IActivityBuilder targetActivity)
        {
            WorkflowBuilder.Connect(this, targetActivity);
            return this;
        }

        public ActivityDefinition BuildActivity()
        {
            Activity.Id = Id;
            Activity.Name = Name;
            Activity.DisplayName = Name;
            Activity.Description = Description;
            Activity.DisplayName = DisplayName;
            return Activity;
        }

        public WorkflowDefinitionVersion Build() => WorkflowBuilder.Build();
    }
}