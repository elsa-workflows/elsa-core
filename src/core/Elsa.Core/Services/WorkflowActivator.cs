using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Services
{
    public class WorkflowActivator : IWorkflowActivator
    {
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IClock _clock;
        private readonly IIdGenerator _idGenerator;

        public WorkflowActivator(IWorkflowRegistry workflowRegistry, IClock clock, IIdGenerator idGenerator)
        {
            this._workflowRegistry = workflowRegistry;
            this._clock = clock;
            this._idGenerator = idGenerator;
        }

        public async Task<WorkflowInstance> ActivateAsync(
            string definitionId,
            string? correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var workflow = await _workflowRegistry.GetWorkflowAsync(
                definitionId,
                VersionOptions.Published,
                cancellationToken);
            return await ActivateAsync(workflow, correlationId, cancellationToken);
        }

        public Task<WorkflowInstance> ActivateAsync(
            Workflow workflow,
            string? correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var workflowInstance = new WorkflowInstance
            {
                Id = _idGenerator.Generate(),
                Status = WorkflowStatus.Idle,
                Version = workflow.Version,
                CorrelationId = correlationId,
                CreatedAt = _clock.GetCurrentInstant(),
                DefinitionId = workflow.DefinitionId
            };

            return Task.FromResult(workflowInstance);
        }
    }
}