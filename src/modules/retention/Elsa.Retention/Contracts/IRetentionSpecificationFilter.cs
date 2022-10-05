using Elsa.Models;
using Elsa.Persistence.Specifications;

namespace Elsa.Retention.Contracts;

public interface IRetentionSpecificationFilter
{
    ISpecification<WorkflowInstance> GetSpecification();
    void AddAndSpecification(ISpecification<WorkflowInstance> specification);
    void AddOrSpecification(ISpecification<WorkflowInstance> specification);
}