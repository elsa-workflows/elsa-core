using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Conductor.Models;
using Elsa.Activities.Conductor.Services;
using Elsa.Metadata;
using Elsa.Providers.Activities;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Conductor.Providers.ActivityTypes
{
    public class EventActivityTypeProvider : IActivityTypeProvider
    {
        private readonly IDescribesActivityType _describesActivityType;
        private readonly IActivityActivator _activityActivator;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public EventActivityTypeProvider(IDescribesActivityType describesActivityType, IActivityActivator activityActivator, IServiceScopeFactory serviceScopeFactory)
        {
            _describesActivityType = describesActivityType;
            _activityActivator = activityActivator;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async ValueTask<IEnumerable<ActivityType>> GetActivityTypesAsync(CancellationToken cancellationToken = default)
        {
            var events = await GetEventsAsync(cancellationToken);
            return await GetActivityTypesAsync(events, cancellationToken).ToListAsync(cancellationToken);
        }

        private async IAsyncEnumerable<ActivityType> GetActivityTypesAsync(IEnumerable<EventDefinition> events, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var eventDefinition in events)
                yield return await CreateActivityTypeAsync(eventDefinition, cancellationToken);
        }

        private async Task<ActivityType> CreateActivityTypeAsync(EventDefinition eventDefinition, CancellationToken cancellationToken)
        {
            async ValueTask<ActivityDescriptor> CreateDescriptorAsync()
            {
                var des = await _describesActivityType.DescribeAsync<EventReceived>(cancellationToken);

                des.Type = eventDefinition.Name;
                des.DisplayName = eventDefinition.DisplayName ?? eventDefinition.Name;
                des.Description = eventDefinition.Description;
                des.InputProperties = Array.Empty<ActivityInputDescriptor>();
                des.Outcomes = eventDefinition.Outcomes?.ToArray() ?? new[] { OutcomeNames.Done };

                return des;
            }

            var descriptor = await CreateDescriptorAsync();

            return new ActivityType
            {
                Type = typeof(EventReceived),
                TypeName = descriptor.Type,
                DisplayName = descriptor.DisplayName,
                DescribeAsync = CreateDescriptorAsync,
                Description = descriptor.Description,
                ActivateAsync = async context =>
                {
                    var activity = await _activityActivator.ActivateActivityAsync<EventReceived>(context, cancellationToken);
                    activity.EventName = eventDefinition.Name;
                    return activity;
                },
                CanExecuteAsync = async (context, instance) => await instance.CanExecuteAsync(context),
                ExecuteAsync = async (context, instance) => await instance.ExecuteAsync(context),
                ResumeAsync = async (context, instance) => await instance.ResumeAsync(context)
            };
        }

        private async Task<IEnumerable<EventDefinition>> GetEventsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var providers = scope.ServiceProvider.GetRequiredService<IEnumerable<IEventsProvider>>();
            return await GetEventsAsync(providers, cancellationToken).ToListAsync(cancellationToken);
        }

        private static async IAsyncEnumerable<EventDefinition> GetEventsAsync(IEnumerable<IEventsProvider> eventProviders, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var commandProvider in eventProviders)
            {
                var commands = await commandProvider.GetEventsAsync(cancellationToken);

                foreach (var command in commands)
                    yield return command;
            }
        }
    }
}