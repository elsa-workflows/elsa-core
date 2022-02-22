using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions.Multitenancy;
using Elsa.Caching;
using Elsa.Decorators;
using Elsa.Services;
using Elsa.WorkflowSettings.Events;
using MediatR;

namespace Elsa.WorkflowSettings.Handlers
{
    public class EvictWorkflowRegistryCacheHandler :
        INotificationHandler<WorkflowSettingsSaved>,
        INotificationHandler<ManyWorkflowSettingsSaved>,
        INotificationHandler<WorkflowSettingsDeleted>,
        INotificationHandler<ManyWorkflowSettingsDeleted>
    {
        private readonly ITriggerIndexer _triggerIndexer;
        private readonly ICacheSignal _cacheSignal;
        private readonly ITenantProvider _tenantProvider;

        public EvictWorkflowRegistryCacheHandler(ITriggerIndexer triggerIndexer, ICacheSignal cacheSignal, ITenantProvider tenantProvider)
        {
            _triggerIndexer = triggerIndexer;
            _cacheSignal = cacheSignal;
            _tenantProvider = tenantProvider;
        }

        public async Task Handle(WorkflowSettingsSaved notification, CancellationToken cancellationToken)
        {
            await HandleWorkflowCacheAndTriggerIndexer(cancellationToken);
        }

        public async Task Handle(ManyWorkflowSettingsSaved notification, CancellationToken cancellationToken)
        {
            await HandleWorkflowCacheAndTriggerIndexer(cancellationToken);
        }

        public async Task Handle(WorkflowSettingsDeleted notification, CancellationToken cancellationToken)
        {
            await HandleWorkflowCacheAndTriggerIndexer(cancellationToken);
        }

        public async Task Handle(ManyWorkflowSettingsDeleted notification, CancellationToken cancellationToken)
        {
            await HandleWorkflowCacheAndTriggerIndexer(cancellationToken);
        }

        private async Task HandleWorkflowCacheAndTriggerIndexer(CancellationToken cancellationToken)
        {
            //TODO: is still needed here?
            await _cacheSignal.TriggerTokenAsync(CachingWorkflowRegistry.RootKeyBase);
            await _triggerIndexer.IndexTriggersAsync(cancellationToken);
        }

        private string GenerateCacheKey(string keyBase)
        {
            var tenantPrefix = _tenantProvider.TryGetCurrentTenant()?.Configuration.GetPrefix();
            return string.IsNullOrEmpty(tenantPrefix) ? keyBase : $"{keyBase}_{tenantPrefix}";
        }
    }
}