using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowDefinitions
{
    public class WorkflowDefinitionVersionIdSpecification : Specification<WorkflowDefinition>
    {
        public string VersionId { get; set; }
        public WorkflowDefinitionVersionIdSpecification(string versionId) => VersionId = versionId;
        public override Expression<Func<WorkflowDefinition, bool>> ToExpression() => x => x.Id == VersionId;
    }
}