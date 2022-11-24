using Elsa.Models;

using LinqKit;

using System;
using System.Linq.Expressions;

namespace Elsa.Persistence.Specifications.WorkflowDefinitions;

public class WorkflowDefinitionNameSpecification : Specification<WorkflowDefinition>
{
    public WorkflowDefinitionNameSpecification(string name, VersionOptions? versionOptions = default, string? tenantId = default)
    {
        Name = name;
        VersionOptions = versionOptions;
        TenantId = tenantId;
    }

    public string Name { get; set; }
    public VersionOptions? VersionOptions { get; }
    public string? TenantId { get; set; }

    public override Expression<Func<WorkflowDefinition, bool>> ToExpression()
    {
        Expression<Func<WorkflowDefinition, bool>> predicate = x => x.Name == Name;

        if (!string.IsNullOrWhiteSpace(TenantId))
            predicate = predicate.And(x => x.TenantId == TenantId);

        if (VersionOptions != null)
            predicate = predicate.WithVersion(VersionOptions.Value);

        return predicate;
    }
}