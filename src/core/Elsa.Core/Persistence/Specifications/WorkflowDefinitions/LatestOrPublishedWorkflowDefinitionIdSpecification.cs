using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowDefinitions
{
    public class LatestOrPublishedWorkflowDefinitionIdSpecification : Specification<WorkflowDefinition>
    {
        public string WorkflowDefinitionId { get; set; }
        public LatestOrPublishedWorkflowDefinitionIdSpecification(string workflowDefinitionId) => WorkflowDefinitionId = workflowDefinitionId;
        public override Expression<Func<WorkflowDefinition, bool>> ToExpression() => x => x.DefinitionId == WorkflowDefinitionId && (x.IsLatest || x.IsPublished);
    }
}