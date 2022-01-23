using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowDefinitions
{
    public class ManyWorkflowDefinitionVersionIdsSpecification : Specification<WorkflowDefinition>
    {
        public IEnumerable<string> DefinitionVersionIds { get; set; }
        public ManyWorkflowDefinitionVersionIdsSpecification(IEnumerable<string> definitionVersionIds) => DefinitionVersionIds = definitionVersionIds;
        public override Expression<Func<WorkflowDefinition, bool>> ToExpression() => x => DefinitionVersionIds.Contains(x.Id);
    }
}