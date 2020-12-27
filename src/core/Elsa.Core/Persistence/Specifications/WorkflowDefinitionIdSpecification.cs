using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications
{
    public class WorkflowDefinitionIdSpecification : Specification<WorkflowDefinition>
    {
        public string Id { get; set; }
        public VersionOptions? VersionOptions { get; set; }
        public WorkflowDefinitionIdSpecification(string id, VersionOptions? versionOptions = default) => (Id, VersionOptions) = (id, versionOptions);
        public override Expression<Func<WorkflowDefinition, bool>> ToExpression() => VersionOptions == null ? x =>  x.Id == Id : x => x.Id == Id && x.WithVersion(VersionOptions);
    }
}