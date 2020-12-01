using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private readonly Lazy<IDictionary<string, Type>> _lazyActivityTypeLookup;

        public TypeBasedActivityProvider(IServiceProvider serviceProvider, IActivityDescriber activityDescriber, IActivityActivator activityActivator)
        {
            _activityDescriber = activityDescriber;
            _activityActivator = activityActivator;

            _lazyActivityTypeLookup = new Lazy<IDictionary<string, Type>>(
                () =>
                {
                    using var scope = serviceProvider.CreateScope();
                    var activities = scope.ServiceProvider.GetServices<IActivity>();
                    return activities.Select(x => x.GetType()).Distinct().ToDictionary(x => x.Name);
                });
        }

        private IDictionary<string, Type> ActivityTypeLookup => _lazyActivityTypeLookup.Value;
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
                    return await instance.CanExecuteAsync(context, context.CancellationToken);
                },
                ExecuteAsync = async context =>
                {
                    var instance = await ActivateActivity(context, activityType);
                    return await instance.ExecuteAsync(context, context.CancellationToken);
                },
                ResumeAsync = async context =>
                {
                    var instance = await ActivateActivity(context, activityType);
                    return await instance.ResumeAsync(context, context.CancellationToken);
                }
            };
        }

        private IEnumerable<Type> GetActivityTypes() => ActivityTypeLookup.Values.ToList();

        private Task<IActivity> ActivateActivity(ActivityExecutionContext context, Type type) => _activityActivator.ActivateActivityAsync(context, type);
    }
}