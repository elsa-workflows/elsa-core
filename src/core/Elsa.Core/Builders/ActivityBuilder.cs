using System;
using System.Linq;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public class ActivityBuilder : IActivityBuilder
    {
        public ActivityBuilder(IActivity activity)
        {
            Activity = activity;
        }

        public IFlowchartBuilder FlowchartBuilder { get; }
        public IActivity Activity { get; }

        public IActivityBuilder StartWith<T>(Action<T>? setup = default, string? name = default) where T : class, IActivity
        {
            return Add(setup, name);
        }

        public IActivityBuilder Add<T>(Action<T>? setup = default, string? name = default) where T : class, IActivity
        {
            return FlowchartBuilder.Add(setup, name);
        }

        public IOutcomeBuilder When(string outcome)
        {
            return new OutcomeBuilder(FlowchartBuilder, this, outcome);
        }

        public IActivityBuilder Then<T>(Action<T>? setup = null, Action<IActivityBuilder>? branch = null, string? name = default) where T : class, IActivity
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

        public IFlowchartBuilder Then(string activityName)
        {
            FlowchartBuilder.Connect(
                () => this,
                () => FlowchartBuilder.Activities.First(x => x.Activity.Name== activityName)
            );

            return FlowchartBuilder;
        }

        public IActivityBuilder Then(IActivityBuilder targetActivity)
        {
            FlowchartBuilder.Connect(this, targetActivity);
            return this;
        }

        public IActivity BuildActivity() => Activity;
    }
}