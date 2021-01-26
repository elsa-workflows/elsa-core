using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityProviders;
using Elsa.Metadata;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.ActivityTypeProviders
{
    public class TypeBasedActivityProvider : IActivityTypeProvider
    {
        private readonly IActivityDescriber _activityDescriber;
        private readonly IActivityActivator _activityActivator;
        private readonly ElsaOptions _elsaOptions;

        public TypeBasedActivityProvider(ElsaOptions options,
            IActivityDescriber activityDescriber, 
            IActivityActivator activityActivator)
        {
            _activityDescriber = activityDescriber;
            _activityActivator = activityActivator;
            _elsaOptions = options;
        }
        
        public ValueTask<IEnumerable<ActivityType>> GetActivityTypesAsync(CancellationToken cancellationToken) => new(GetActivityTypesInternal());
       
        private IEnumerable<ActivityType> GetActivityTypesInternal() => GetActivityTypes().Select(CreateActivityType);
       
        private ActivityType CreateActivityType(Type activityType)
        {
            var info = _activityDescriber.Describe(activityType)!;

            return new ActivityType
            {
                Type = info.Type,
                Description = info.Description,
                DisplayName = info.DisplayName,
                ActivateAsync = async context => await ActivateActivity(context, activityType),
                CanExecuteAsync = async context =>
                {
                    var instance = await ActivateActivity(context, activityType);
                    return await instance.CanExecuteAsync(context);
                },
                ExecuteAsync = async context =>
                {
                    var instance = await ActivateActivity(context, activityType);
                    return await instance.ExecuteAsync(context);
                },
                ResumeAsync = async context =>
                {
                    var instance = await ActivateActivity(context, activityType);
                    return await instance.ResumeAsync(context);
                }
            };
        }

        private IEnumerable<Type> GetActivityTypes() => _elsaOptions.ActivityTypes;
        private Task<IActivity> ActivateActivity(ActivityExecutionContext context, Type type) => _activityActivator.ActivateActivityAsync(context, type);
    }
}