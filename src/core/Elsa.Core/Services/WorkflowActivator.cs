using System.Threading;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Models;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Services
{
    public class WorkflowActivator : IWorkflowActivator
    {
        private readonly IClock _clock;
        private readonly IIdGenerator _idGenerator;

        public WorkflowActivator(IClock clock, IIdGenerator idGenerator)
        {
            _clock = clock;
            _idGenerator = idGenerator;
        }

        public Task<WorkflowInstance> ActivateAsync(
            Workflow workflow,
            string? correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var workflowInstance = new WorkflowInstance
            {
                WorkflowInstanceId = _idGenerator.Generate(),
                WorkflowDefinitionId = workflow.WorkflowDefinitionId,
                Version = workflow.Version,
                Status = WorkflowStatus.Idle,
                CorrelationId = correlationId,
                CreatedAt = _clock.GetCurrentInstant(),
            };

            return Task.FromResult(workflowInstance);
        }
    }
}