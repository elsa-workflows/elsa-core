using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications.WorkflowDefinitions
{
    public class ManyWorkflowDefinitionNamesSpecification : Specification<WorkflowDefinition>
    {
        public IEnumerable<string> Names { get; set; }
        public ManyWorkflowDefinitionNamesSpecification(IEnumerable<string> names) => Names = names;
        public override Expression<Func<WorkflowDefinition, bool>> ToExpression() => x => Names.Contains(x.Name);
    }
}