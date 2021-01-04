using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityProviders;
using Elsa.Exceptions;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public class ActivityTypeService : IActivityTypeService
    {
        private readonly IEnumerable<IActivityTypeProvider> _providers;
        private IDictionary<string, ActivityType>? _activityTypeDictionary;

        public ActivityTypeService(IEnumerable<IActivityTypeProvider> providers)
        {
            _providers = providers;
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

        private async ValueTask<IDictionary<string, ActivityType>> GetDictionaryAsync(CancellationToken cancellationToken)
        {
            if (_activityTypeDictionary != null)
                return _activityTypeDictionary;
            
            _activityTypeDictionary = await GetActivityTypesInternalAsync(cancellationToken).ToDictionaryAsync(x => x.Type, cancellationToken);
            return _activityTypeDictionary;
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