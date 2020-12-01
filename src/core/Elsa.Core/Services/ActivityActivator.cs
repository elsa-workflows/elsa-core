using System;
using System.Threading.Tasks;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Services
{
    public class ActivityActivator : IActivityActivator
    {
        public async Task<IActivity> ActivateActivityAsync(ActivityExecutionContext context, Type type)
        {
            var activity = (IActivity) ActivatorUtilities.GetServiceOrCreateInstance(context.ServiceScope.ServiceProvider, type);
            activity.Data = context.ActivityInstance.Data;
            activity.Id = context.ActivityInstance.Id;
            await context.WorkflowExecutionContext.WorkflowBlueprint.ActivityPropertyProviders.SetActivityPropertiesAsync(activity, context, context.CancellationToken);
            return activity;
        }
    }
}