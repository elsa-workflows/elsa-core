using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowDefinitions
{
    public class WorkflowDefinitionIdSpecification : Specification<WorkflowDefinition>
    {
        public string Id { get; set; }

        public WorkflowDefinitionIdSpecification(string id)
        {
            Id = id;
        }

        public override Expression<Func<WorkflowDefinition, bool>> ToExpression() => x => x.DefinitionId == Id;
    }
}