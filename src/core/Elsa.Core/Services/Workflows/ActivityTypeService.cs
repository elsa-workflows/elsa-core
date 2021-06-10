using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Exceptions;
using Elsa.Metadata;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Services.Workflows
{
    public class ActivityTypeService : IActivityTypeService
    {
        private readonly IEnumerable<IActivityTypeProvider> _providers;
        private readonly IMemoryCache _memoryCache;
        private readonly IMediator _mediator;

        public ActivityTypeService(IEnumerable<IActivityTypeProvider> providers, IMemoryCache memoryCache, IMediator mediator)
        {
            _providers = providers;
            _memoryCache = memoryCache;
            _mediator = mediator;
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
            var descriptor = activityType.Describe();
            await _mediator.Publish(new DescribingActivityType(activityType, descriptor), cancellationToken);
            return descriptor;
        }

        private async ValueTask<IDictionary<string, ActivityType>> GetDictionaryAsync(CancellationToken cancellationToken)
        {
            const string key = "ActivityTypes";
            return await _memoryCache.GetOrCreate(key, async _ => await GetActivityTypesInternalAsync(cancellationToken).ToDictionaryAsync(x => x.TypeName, cancellationToken));
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