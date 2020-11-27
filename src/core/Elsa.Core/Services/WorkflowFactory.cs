using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Services
{
    public class WorkflowFactory : IWorkflowFactory
    {
        private readonly IActivityFactory _activityFactory;
        private readonly IClock _clock;
        private readonly IIdGenerator _idGenerator;

        public WorkflowFactory(IActivityFactory activityFactory, IClock clock, IIdGenerator idGenerator)
        {
            _activityFactory = activityFactory;
            _clock = clock;
            _idGenerator = idGenerator;
        }

        public Task<WorkflowInstance> InstantiateAsync(
            IWorkflowBlueprint workflowBlueprint,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
        {
            var workflowInstance = new WorkflowInstance
            {
                WorkflowInstanceId = _idGenerator.Generate(),
                WorkflowDefinitionId = workflowBlueprint.Id,
                TenantId = workflowBlueprint.TenantId,
                Version = workflowBlueprint.Version,
                Status = WorkflowStatus.Idle,
                CorrelationId = correlationId,
                ContextId = contextId,
                CreatedAt = _clock.GetCurrentInstant(),
                Activities = workflowBlueprint.Activities.Select(CreateInstance).ToList(),
                Variables = new Variables(workflowBlueprint.Variables)
            };

            return Task.FromResult(workflowInstance);
        }

        private ActivityInstance CreateInstance(IActivityBlueprint activityBlueprint) => _activityFactory.Instantiate(activityBlueprint);
    }
}