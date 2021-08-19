using System;
using System.Linq.Expressions;
using Elsa.Models;
using LinqKit;

namespace Elsa.Persistence.Specifications.WorkflowDefinitions
{
    public class WorkflowDefinitionIdSpecification : Specification<WorkflowDefinition>
    {
        public WorkflowDefinitionIdSpecification(string id, VersionOptions? versionOptions = default)
        {
            Id = id;
            VersionOptions = versionOptions;
        }
        
        public string Id { get; set; }
        public VersionOptions? VersionOptions { get; }

        public override Expression<Func<WorkflowDefinition, bool>> ToExpression()
        {
            Expression<Func<WorkflowDefinition, bool>> queryable =  x => x.DefinitionId == Id;

            if (VersionOptions != null)
                queryable = queryable.And(x => x.WithVersion(VersionOptions));
            
            return queryable;
        }
    }
}