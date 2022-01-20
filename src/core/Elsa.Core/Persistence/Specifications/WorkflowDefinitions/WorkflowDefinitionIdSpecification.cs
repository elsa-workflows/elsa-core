using System;
using System.Linq.Expressions;
using Elsa.Models;

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
            Expression<Func<WorkflowDefinition, bool>> predicate =  x => x.DefinitionId == Id;

            if (VersionOptions != null)
                predicate = predicate.WithVersion(VersionOptions.Value);
            
            return predicate;
        }
    }
}