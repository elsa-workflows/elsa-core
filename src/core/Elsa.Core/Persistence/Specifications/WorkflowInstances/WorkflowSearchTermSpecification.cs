using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowInstances
{
    public class WorkflowSearchTermSpecification : Specification<WorkflowInstance>
    {
        public string SearchTerm { get; set; }
        public WorkflowSearchTermSpecification(string searchTerm) => SearchTerm = searchTerm;

        public override Expression<Func<WorkflowInstance, bool>> ToExpression() => x =>
            x.Name!.Contains(SearchTerm)
            || x.Id.Contains(SearchTerm)
            || x.ContextId!.Contains(SearchTerm)
            || x.CorrelationId.Contains(SearchTerm);
    }
}