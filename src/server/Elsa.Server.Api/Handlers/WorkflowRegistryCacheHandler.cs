using System.Threading;
using System.Threading.Tasks;
using Elsa.Caching;
using Elsa.Events;
using Elsa.Services.Workflows;
using MediatR;

namespace Elsa.Server.Api.Handlers
{
    public class WorkflowRegistryCacheHandler : INotificationHandler<WorkflowDefinitionUpdated>
    {
        private readonly ICacheSignal _cacheSignal;

        public WorkflowRegistryCacheHandler(ICacheSignal cacheSignal)
        {
            _cacheSignal = cacheSignal;
        }

        public async Task Handle(WorkflowDefinitionUpdated notification, CancellationToken cancellationToken)
        {
            await _cacheSignal.TriggerTokenAsync(ActivityTypeService.CacheKey);
        }
    }
}