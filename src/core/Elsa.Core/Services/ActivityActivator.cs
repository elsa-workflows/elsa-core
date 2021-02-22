using System;
using System.Threading.Tasks;
using Elsa.Activities.ControlFlow;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public class ActivityActivator : IActivityActivator
    {
        private readonly ElsaOptions _elsaOptions;

        public ActivityActivator(ElsaOptions options)
        {
            _elsaOptions = options;
        }
        
        public async Task<IActivity> ActivateActivityAsync(ActivityExecutionContext context, Type type)
        {
            var activity = _elsaOptions.ActivityFactory.CreateService(type, context.ServiceProvider);
            activity.Data = context.GetData();
            activity.Id = context.ActivityId;

            // TODO: Make extensible / apply open/closed.
            if(ShouldSetProperties(activity))
                await context.WorkflowExecutionContext.WorkflowBlueprint.ActivityPropertyProviders.SetActivityPropertiesAsync(activity, context, context.CancellationToken);
            
            return activity;
        }

        private bool ShouldSetProperties(IActivity activity)
        {
            if (IsReturningComposite(activity))
                return false;

            if (IsReturningIf(activity))
                return false;
            
            if (IsReturningSwitch(activity))
                return false;

            return true;
        }
        
        private bool IsReturningComposite(IActivity activity) => activity is CompositeActivity && activity.Data.GetState<bool>(nameof(CompositeActivity.IsScheduled));
        private bool IsReturningIf(IActivity activity) => activity is If && activity.Data.GetState<bool>(nameof(If.EnteredScope));
        private bool IsReturningSwitch(IActivity activity) => activity is Switch && activity.Data.GetState<bool>(nameof(Switch.EnteredScope));
    }
}