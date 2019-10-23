using System;
using System.Linq;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.WorkflowBuilders
{
    public class ActivityBuilder : IActivityBuilder
    {
        public ActivityBuilder(WorkflowBuilder workflowBuilder, ActivityDefinition activity, string id)
        {
            WorkflowBuilder = workflowBuilder;
            Activity = activity;
            Id = id;
        }

        public WorkflowBuilder WorkflowBuilder { get; }
        public ActivityDefinition Activity { get; }
        public string Id { get; set; }

        public IActivityBuilder StartWith<T>(Action<T> setup = default, string id = default) where T : class, IActivity
        {
            return Add(setup, id);
        }

        public IActivityBuilder Add<T>(Action<T> setup = default, string id = null) where T : class, IActivity
        {
            return WorkflowBuilder.Add(setup, id);
        }

        public IOutcomeBuilder When(string outcome)
        {
            return new OutcomeBuilder(WorkflowBuilder, this, outcome);
        }

        public IActivityBuilder Then<T>(Action<T> setup = null, Action<IActivityBuilder> branch = null, string id = null) where T : class, IActivity
        {
            var activityBuilder = When(null).Then(setup, branch, id);
            return activityBuilder;
        }

        public IWorkflowBuilder Then(string activityId)
        {
            WorkflowBuilder.Connect(
                () => this,
                () => WorkflowBuilder.Activities.First(x => x.Id == activityId)
            );

            return WorkflowBuilder;
        }

        public ActivityDefinition BuildActivity()
        {
            Activity.Id = Id;
            return Activity;
        }

        public WorkflowDefinitionVersion Build() => WorkflowBuilder.Build();
    }
}