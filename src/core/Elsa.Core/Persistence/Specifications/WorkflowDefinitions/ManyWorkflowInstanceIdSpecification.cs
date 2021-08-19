using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Elsa.Models;
using LinqKit;

namespace Elsa.Persistence.Specifications.WorkflowDefinitions
{
    public class ManyWorkflowDefinitionIdsSpecification : Specification<WorkflowDefinition>
    {
        public ManyWorkflowDefinitionIdsSpecification(IEnumerable<string> ids, VersionOptions? versionOptions = default)
        {
            Ids = ids;
            VersionOptions = versionOptions;
        }
        
        public IEnumerable<string> Ids { get; set; }
        public VersionOptions? VersionOptions { get; }

        public override Expression<Func<WorkflowDefinition, bool>> ToExpression()
        {
            Expression<Func<WorkflowDefinition, bool>> predicate = x => Ids.Contains(x.DefinitionId);

            if (VersionOptions != null)
                predicate = predicate.And(x => x.WithVersion(VersionOptions));
            
            return predicate;
        }
    }
}