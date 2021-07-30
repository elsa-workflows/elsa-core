using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Caching;
using Elsa.Events;
using Elsa.Exceptions;
using Elsa.Metadata;
using Elsa.Providers.Activities;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Services.Workflows
{
    public class ActivityTypeService : IActivityTypeService
    {
        public const string CacheKey = "ActivityTypes";
        private readonly IEnumerable<IActivityTypeProvider> _providers;
        private readonly IMemoryCache _memoryCache;
        private readonly IMediator _mediator;
        private readonly ICacheSignal _cacheSignal;

        public ActivityTypeService(IEnumerable<IActivityTypeProvider> providers, IMemoryCache memoryCache, IMediator mediator, ICacheSignal cacheSignal)
        {
            _providers = providers;
            _memoryCache = memoryCache;
            _mediator = mediator;
            _cacheSignal = cacheSignal;
        }

        public async ValueTask<IEnumerable<ActivityType>> GetActivityTypesAsync(CancellationToken cancellationToken) => (await GetDictionaryAsync(cancellationToken)).Values;

        public async ValueTask<ActivityType> GetActivityTypeAsync(string type, CancellationToken cancellationToken)
        {
            var dictionary = await GetDictionaryAsync(cancellationToken);

            if (!dictionary.ContainsKey(type))
                throw new WorkflowException($"The activity type '{type}' has not been registered. Did you forget to register it with ElsaOptions?");

            return dictionary[type];
        }

        public async ValueTask<RuntimeActivityInstance> ActivateActivityAsync(IActivityBlueprint activityBlueprint, CancellationToken cancellationToken = default)
        {
            var type = await GetActivityTypeAsync(activityBlueprint.Type, cancellationToken);

            return new RuntimeActivityInstance
            {
                ActivityType = type,
                Id = activityBlueprint.Id,
                Name = activityBlueprint.Name,
                PersistWorkflow = activityBlueprint.PersistWorkflow,
                LoadWorkflowContext = activityBlueprint.LoadWorkflowContext,
                SaveWorkflowContext = activityBlueprint.SaveWorkflowContext
            };
        }

        public async ValueTask<ActivityDescriptor> DescribeActivityType(ActivityType activityType, CancellationToken cancellationToken)
        {
            var descriptor = await activityType.DescribeAsync();
            await _mediator.Publish(new DescribingActivityType(activityType, descriptor), cancellationToken);
            return descriptor;
        }

        private async ValueTask<IDictionary<string, ActivityType>> GetDictionaryAsync(CancellationToken cancellationToken)
        {
            return await _memoryCache.GetOrCreate(CacheKey, async entry =>
            {
                entry.Monitor(_cacheSignal.GetToken(CacheKey));
                return await GetActivityTypesInternalAsync(cancellationToken).ToDictionaryAsync(x => x.TypeName, cancellationToken);
            });
        }

        private async IAsyncEnumerable<ActivityType> GetActivityTypesInternalAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var provider in _providers)
            {
                var types = await provider.GetActivityTypesAsync(cancellationToken);

                foreach (var type in types)
                    yield return type;
            }
        }
    }
}