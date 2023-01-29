using System;
using System.Linq.Expressions;
using Elsa.Models;
using LinqKit;

namespace Elsa.Persistence.Specifications.WorkflowDefinitions
{
    public class WorkflowDefinitionIdSpecification : Specification<WorkflowDefinition>
    {
        public WorkflowDefinitionIdSpecification(string id, VersionOptions? versionOptions = default, string? tenantId = default)
        {
            Id = id;
            VersionOptions = versionOptions;
            TenantId = tenantId;
        }

        public string Id { get; set; }
        public VersionOptions? VersionOptions { get; }
        public string? TenantId { get; set; }

        public override Expression<Func<WorkflowDefinition, bool>> ToExpression()
        {
            Expression<Func<WorkflowDefinition, bool>> predicate = x => x.DefinitionId == Id;

            if (!string.IsNullOrWhiteSpace(TenantId))
                predicate = predicate.And(x => x.TenantId == TenantId);

            if (VersionOptions != null)
                predicate = predicate.WithVersion(VersionOptions.Value);

            return predicate;
        }
    }
}