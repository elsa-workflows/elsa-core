using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowInstances
{
    public class WorkflowInstanceCorrelationIdSpecification : CorrelationIdSpecification<WorkflowInstance>
    {
        public WorkflowInstanceCorrelationIdSpecification(string? correlationId) : base(correlationId)
        {
        }
    }
}