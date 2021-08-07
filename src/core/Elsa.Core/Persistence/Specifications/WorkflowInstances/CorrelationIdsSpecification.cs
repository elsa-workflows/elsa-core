using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowInstances
{
    public class CorrelationIdsSpecification : Specification<WorkflowInstance>
    {
        public CorrelationIdsSpecification(string workflowDefinitionId, IEnumerable<string> correlationIds)
        {
            WorkflowDefinitionId = workflowDefinitionId;
            CorrelationIds = correlationIds;
        }

        public string WorkflowDefinitionId { get; }
        public IEnumerable<string> CorrelationIds { get; }
        public override Expression<Func<WorkflowInstance, bool>> ToExpression() => workflowInstance => workflowInstance.DefinitionId == WorkflowDefinitionId && CorrelationIds.Contains(workflowInstance.CorrelationId);
    }
}