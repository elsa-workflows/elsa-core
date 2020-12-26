using System;
using System.Threading.Tasks;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

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
            var activity = _elsaOptions.ActivityFactory.CreateService(type, context.ServiceScope.ServiceProvider);
            activity.Data = context.ActivityInstance.Data;
            activity.Id = context.ActivityInstance.Id;
            await context.WorkflowExecutionContext.WorkflowBlueprint.ActivityPropertyProviders.SetActivityPropertiesAsync(activity, context, context.CancellationToken);
            return activity;
        }
    }
}