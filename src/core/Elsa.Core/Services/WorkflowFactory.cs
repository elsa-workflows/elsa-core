using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;
using Newtonsoft.Json.Linq;
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
            CancellationToken cancellationToken = default)
        {
            var workflowInstance = new WorkflowInstance
            {
                WorkflowInstanceId = _idGenerator.Generate(),
                WorkflowDefinitionId = workflowBlueprint.Id,
                Version = workflowBlueprint.Version,
                Status = WorkflowStatus.Idle,
                CorrelationId = correlationId,
                CreatedAt = _clock.GetCurrentInstant(),
                Activities = workflowBlueprint.Activities.Select(CreateInstance).ToList(),
                Variables = new Variables(workflowBlueprint.Variables)
            };

            return Task.FromResult(workflowInstance);
        }

        private ActivityInstance CreateInstance(IActivityBlueprint activityBlueprint) => new ActivityInstance(
            activityBlueprint.Id,
            activityBlueprint.Type,
            null,
            new JObject(activityBlueprint.Data));
    }
}