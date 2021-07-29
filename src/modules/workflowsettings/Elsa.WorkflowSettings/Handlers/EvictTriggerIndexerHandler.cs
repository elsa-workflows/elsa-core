using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.WorkflowSettings.Abstractions.Events;
using MediatR;

namespace Elsa.WorkflowSettings.Handlers
{
    public class EvictTriggerIndexerHandler : INotificationHandler<WorkflowSettingsSaved>, INotificationHandler<WorkflowSettingsDeleted>
    {
        private readonly ITriggerIndexer _triggerIndexer;

        public EvictTriggerIndexerHandler(ITriggerIndexer triggerIndexer)
        {
            _triggerIndexer = triggerIndexer;
        }

        public async Task Handle(WorkflowSettingsSaved notification, CancellationToken cancellationToken)
        {
            await _triggerIndexer.IndexTriggersAsync(cancellationToken);
        }

        public async Task Handle(WorkflowSettingsDeleted notification, CancellationToken cancellationToken)
        {
            await _triggerIndexer.IndexTriggersAsync(cancellationToken);
        }
    }
}