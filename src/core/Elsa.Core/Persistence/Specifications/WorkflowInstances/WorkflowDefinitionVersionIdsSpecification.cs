using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowInstances
{
    public class WorkflowDefinitionVersionIdsSpecification : Specification<WorkflowInstance>
    {
        public ICollection<string> WorkflowDefinitionVersionIds { get; set; }
        public WorkflowDefinitionVersionIdsSpecification(IEnumerable<string> workflowDefinitionVersionIds) => WorkflowDefinitionVersionIds = workflowDefinitionVersionIds.ToList();
        public override Expression<Func<WorkflowInstance, bool>> ToExpression() => x => WorkflowDefinitionVersionIds.Contains(x.DefinitionVersionId);
    }
}