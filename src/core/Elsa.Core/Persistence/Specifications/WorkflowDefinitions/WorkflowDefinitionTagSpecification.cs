using System;
using System.Linq.Expressions;
using Elsa.Models;
using LinqKit;

namespace Elsa.Persistence.Specifications.WorkflowDefinitions;

public class WorkflowDefinitionTagSpecification : Specification<WorkflowDefinition>
{
    public WorkflowDefinitionTagSpecification(string tag, VersionOptions? versionOptions = default, string? tenantId = default)
    {
        Tag = tag;
        VersionOptions = versionOptions;
        TenantId = tenantId;
    }

    public string Tag { get; set; }
    public VersionOptions? VersionOptions { get; }
    public string? TenantId { get; set; }

    public override Expression<Func<WorkflowDefinition, bool>> ToExpression()
    {
        Expression<Func<WorkflowDefinition, bool>> predicate = x => x.Tag == Tag;

        if (!string.IsNullOrWhiteSpace(TenantId))
            predicate = predicate.And(x => x.TenantId == TenantId);

        if (VersionOptions != null)
            predicate = predicate.WithVersion(VersionOptions.Value);

        return predicate;
    }
}