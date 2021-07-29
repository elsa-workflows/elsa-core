using System.Threading;
using System.Threading.Tasks;
using Elsa.Caching;
using Elsa.Decorators;
using Elsa.WorkflowSettings.Abstractions.Events;
using MediatR;

namespace Elsa.WorkflowSettings.Handlers
{
    public class EvictWorkflowRegistryCacheHandler : INotificationHandler<WorkflowSettingsSaved>, INotificationHandler<WorkflowSettingsDeleted>
    {
        private readonly ICacheSignal _cacheSignal;

        public EvictWorkflowRegistryCacheHandler(ICacheSignal cacheSignal)
        {
            _cacheSignal = cacheSignal;
        }

        public async Task Handle(WorkflowSettingsSaved notification, CancellationToken cancellationToken)
        {
            await _cacheSignal.TriggerTokenAsync(CachingWorkflowRegistry.CacheKey);
        }

        public async Task Handle(WorkflowSettingsDeleted notification, CancellationToken cancellationToken)
        {
            await _cacheSignal.TriggerTokenAsync(CachingWorkflowRegistry.CacheKey);
        }
    }
}