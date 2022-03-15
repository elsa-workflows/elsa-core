using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Metadata;
using Elsa.Options;
using Elsa.Providers.Activities;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Providers.ActivityTypes
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
        
        public async ValueTask<IEnumerable<ActivityType>> GetActivityTypesAsync(CancellationToken cancellationToken) => await GetActivityTypesInternal(cancellationToken).ToListAsync(cancellationToken);
       
        private async IAsyncEnumerable<ActivityType> GetActivityTypesInternal([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var types = GetActivityTypes();

            foreach (var type in types)
            {
                var activityType = await CreateActivityTypeAsync(type, cancellationToken);
                
                yield return activityType;
            }
        }

        private async Task<ActivityType> CreateActivityTypeAsync(Type activityType, CancellationToken cancellationToken)
        {
            var info = await _describesActivityType.DescribeAsync(activityType, cancellationToken);

            return new ActivityType
            {
                TypeName = info.Type,
                Type = activityType,
                Description = info.Description,
                DisplayName = info.DisplayName,
                IsBrowsable =  activityType.GetCustomAttribute<BrowsableAttribute>(false)?.Browsable ?? true,
                ActivateAsync = async context => await ActivateActivity(context, activityType),
                DescribeAsync = async () => (await _describesActivityType.DescribeAsync(activityType, cancellationToken))!, 
                CanExecuteAsync = async (context, instance) => await instance.CanExecuteAsync(context),
                ExecuteAsync = async (context, instance) => await instance.ExecuteAsync(context),
                ResumeAsync = async (context, instance) => await instance.ResumeAsync(context)
            };
        }

        private IEnumerable<Type> GetActivityTypes() => _elsaOptions.ActivityTypes;
        private Task<IActivity> ActivateActivity(ActivityExecutionContext context, Type type) => _activityActivator.ActivateActivityAsync(context, type);
    }
}
