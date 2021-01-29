using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Services
{
    public class WorkflowFactory : IWorkflowFactory
    {
        private readonly IClock _clock;
        private readonly IIdGenerator _idGenerator;

        public WorkflowFactory(IClock clock, IIdGenerator idGenerator)
        {
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
                Id = _idGenerator.Generate(),
                DefinitionId = workflowBlueprint.Id,
                TenantId = workflowBlueprint.TenantId,
                Version = workflowBlueprint.Version,
                WorkflowStatus = WorkflowStatus.Idle,
                CorrelationId = correlationId,
                ContextId = contextId,
                CreatedAt = _clock.GetCurrentInstant(),
                Variables = new Variables(workflowBlueprint.Variables),
                ContextType = workflowBlueprint.ContextOptions?.ContextType ?.GetContextTypeName()
            };

            return Task.FromResult(workflowInstance);
        }
    }
}