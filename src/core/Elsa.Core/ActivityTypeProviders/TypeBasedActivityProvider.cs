using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Metadata;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.ActivityTypeProviders
{
    public class TypeBasedActivityProvider : IActivityTypeProvider
    {
        private readonly IDescribesActivityType _describesActivityType;
        private readonly IActivityActivator _activityActivator;
        private readonly ElsaOptions _elsaOptions;

        public TypeBasedActivityProvider(ElsaOptions options,
            IDescribesActivityType describesActivityType, 
            IActivityActivator activityActivator)
        {
            _describesActivityType = describesActivityType;
            _activityActivator = activityActivator;
            _elsaOptions = options;
        }
        
        public ValueTask<IEnumerable<ActivityType>> GetActivityTypesAsync(CancellationToken cancellationToken) => new(GetActivityTypesInternal());
       
        private IEnumerable<ActivityType> GetActivityTypesInternal() => GetActivityTypes().Select(CreateActivityType).Where(x => x != null).Select(x => x!);
       
        private ActivityType? CreateActivityType(Type activityType)
        {
            var info = _describesActivityType.Describe(activityType);

            if (info == null)
                return default;

            return new ActivityType
            {
                TypeName = info.Type,
                Type = activityType,
                Description = info.Description,
                DisplayName = info.DisplayName,
                ActivateAsync = async context => await ActivateActivity(context, activityType),
                Describe = () => info, 
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