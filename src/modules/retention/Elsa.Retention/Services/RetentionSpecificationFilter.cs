using Elsa.Models;
using Elsa.Persistence.Specifications;
using Elsa.Retention.Contracts;

namespace Elsa.Retention.Services
{
    public class RetentionSpecificationFilter : IRetentionSpecificationFilter
    {
        ISpecification<WorkflowInstance> _specification = default!;

        public RetentionSpecificationFilter()
        {
            _specification = Specification<WorkflowInstance>.Identity;
        }
        public void AddAndSpecification(ISpecification<WorkflowInstance> specification) => _specification = _specification.And(specification);


        public void AddOrSpecification(ISpecification<WorkflowInstance> specification) => _specification = _specification.Or(specification);

        public ISpecification<WorkflowInstance> GetSpecification() => _specification;

    }
}
