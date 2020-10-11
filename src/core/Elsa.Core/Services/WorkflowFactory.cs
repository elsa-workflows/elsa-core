using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
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
            WorkflowDefinition workflowDefinition,
            string? correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var workflowInstance = new WorkflowInstance
            {
                WorkflowInstanceId = _idGenerator.Generate(),
                WorkflowDefinitionId = workflowDefinition.WorkflowDefinitionId,
                Version = workflowDefinition.Version,
                Status = WorkflowStatus.Idle,
                CorrelationId = correlationId,
                CreatedAt = _clock.GetCurrentInstant(),
                Activities = workflowDefinition.Activities.Select(CreateInstance).ToList(),
                Variables = workflowDefinition.Variables != null ? new Variables(workflowDefinition.Variables) : new Variables(),
            };

            return Task.FromResult(workflowInstance);
        }

        private ActivityInstance CreateInstance(ActivityDefinition activityDefinition) => new ActivityInstance(
            activityDefinition.Id,
            activityDefinition.Type,
            null,
            new JObject(activityDefinition.Data));
    }
}