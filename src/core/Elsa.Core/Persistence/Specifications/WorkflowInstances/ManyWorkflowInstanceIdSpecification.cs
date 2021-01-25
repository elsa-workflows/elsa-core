using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowInstances
{
    public class ManyWorkflowInstanceIdsSpecification : Specification<WorkflowInstance>
    {
        public IEnumerable<string> Ids { get; set; }
        public ManyWorkflowInstanceIdsSpecification(IEnumerable<string> ids) => Ids = ids;
        public override Expression<Func<WorkflowInstance, bool>> ToExpression() => x => Ids.Contains(x.Id);
    }
}