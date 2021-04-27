using System;
using System.Threading;
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
        
        public async Task<IActivity> ActivateActivityAsync(ActivityExecutionContext context, Type type, CancellationToken cancellationToken = default)
        {
            var activity = _elsaOptions.ActivityFactory.CreateService(type, context.ServiceProvider);
            activity.Data = context.GetData();
            activity.Id = context.ActivityId;

            // TODO: Make extensible / apply open/closed.
            if(ShouldSetProperties(activity))
            {
                // TODO: Figure out how to deal with dynamically defined properties and what it means to set values to these.
                // ActivityTypes can have dynamic properties, so they need to be able to "intercept" when values are being applied.
                // Right now, we can only set these values on properties of the IActivity implementation.
                //var activityType = await _activityTypeService.GetActivityTypeAsync(activity.Type, cancellationToken);
                await context.WorkflowExecutionContext.WorkflowBlueprint.ActivityPropertyProviders.SetActivityPropertiesAsync(activity, context, context.CancellationToken);
            }
            
            return activity;
        }

        private bool ShouldSetProperties(IActivity activity)
        {
            if (IsReturningComposite(activity))
                return false;

            if (IsReturningIf(activity))
            {
                activity.Data.SetState("Unwinding", false);
                return false;
            }
            
            if (IsReturningSwitch(activity))
            {
                activity.Data.SetState("Unwinding", false);
                return false;
            }

            return true;
        }
        
        private bool IsReturningComposite(IActivity activity) => activity is CompositeActivity && activity.Data.GetState<bool>(nameof(CompositeActivity.IsScheduled));
        private bool IsReturningIf(IActivity activity) => activity is If && activity.Data.GetState<bool>("Unwinding");
        private bool IsReturningSwitch(IActivity activity) => activity is Switch && activity.Data.GetState<bool>("Unwinding");
    }
}