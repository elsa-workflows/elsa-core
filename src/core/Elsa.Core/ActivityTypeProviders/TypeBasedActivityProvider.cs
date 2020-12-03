using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityProviders;
using Elsa.Metadata;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ActivityTypeProviders
{
    public class TypeBasedActivityProvider : IActivityTypeProvider
    {
        private readonly IActivityDescriber _activityDescriber;
        private readonly IActivityActivator _activityActivator;
        private readonly IDictionary<string, Type> _activityTypeLookup;

        public TypeBasedActivityProvider(IServiceProvider serviceProvider, IActivityDescriber activityDescriber, IActivityActivator activityActivator)
        {
            _activityDescriber = activityDescriber;
            _activityActivator = activityActivator;

            using var scope = serviceProvider.CreateScope();
            var activities = scope.ServiceProvider.GetServices<IActivity>();
            _activityTypeLookup = activities.Select(x => x.GetType()).Distinct().ToDictionary(x => x.Name);
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

        private IEnumerable<Type> GetActivityTypes() => _activityTypeLookup.Values.ToList();

        private Task<IActivity> ActivateActivity(ActivityExecutionContext context, Type type) => _activityActivator.ActivateActivityAsync(context, type);
    }
}