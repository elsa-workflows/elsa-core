using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowInstances
{
    public class WorkflowInstanceNameMatchSpecification : Specification<WorkflowInstance>
    {
        public string Name { get; set; }
        public WorkflowInstanceNameMatchSpecification(string name) => Name = name;
        public override Expression<Func<WorkflowInstance, bool>> ToExpression() => x => x.Name!.Contains(Name);
        public override bool IsSatisfiedBy(WorkflowInstance entity) => entity.Name != null && entity.Name.IndexOf(Name, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}