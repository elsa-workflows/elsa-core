using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Services
{
    public class WorkflowActivator : IWorkflowActivator
    {
        private readonly IWorkflowRegistry workflowRegistry;
        private readonly IClock clock;
        private readonly IIdGenerator idGenerator;

        public WorkflowActivator(IWorkflowRegistry workflowRegistry, IClock clock, IIdGenerator idGenerator)
        {
            this.workflowRegistry = workflowRegistry;
            this.clock = clock;
            this.idGenerator = idGenerator;
        }
        
        public async Task<WorkflowInstance> ActivateAsync(string definitionId, string? correlationId = default, CancellationToken cancellationToken = default)
        {
            var workflow = await workflowRegistry.GetWorkflowAsync(definitionId, VersionOptions.Published, cancellationToken);
            return await ActivateAsync(workflow, correlationId, cancellationToken);
        }

        public Task<WorkflowInstance> ActivateAsync(Workflow workflow, string? correlationId = default, CancellationToken cancellationToken = default)
        {
            var workflowInstance = new WorkflowInstance
            {
                Id = idGenerator.Generate(),
                Status = WorkflowStatus.Idle,
                Version = workflow.Version,
                CorrelationId = correlationId,
                CreatedAt = clock.GetCurrentInstant(),
                DefinitionId = workflow.DefinitionId
            };

            return Task.FromResult(workflowInstance);
        }
    }
}