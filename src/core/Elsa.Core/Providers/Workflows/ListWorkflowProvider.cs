using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Providers.Workflows;

/// <summary>
/// A simple provider that returns workflow blueprints stored in a collection.
/// </summary>
public class ListWorkflowProvider : WorkflowProvider
{
    private readonly ICollection<IWorkflowBlueprint> _workflowBlueprints = new List<IWorkflowBlueprint>();

    public void Add(IWorkflowBlueprint workflowBlueprint) => _workflowBlueprints.Add(workflowBlueprint);

    public override IAsyncEnumerable<IWorkflowBlueprint> ListAsync(VersionOptions versionOptions, int? skip = default, int? take = default, string? tenantId = default, CancellationToken cancellationToken = default)
    {
        var queryable = _workflowBlueprints.AsQueryable().WithVersion(versionOptions);

        if (skip != null)
            queryable = queryable.Skip(skip.Value);

        if (take != null)
            queryable = queryable.Take(take.Value);

        if (tenantId != null)
            queryable = queryable.Where(x => x.TenantId == tenantId);

        return queryable.ToAsyncEnumerable();
    }

    public override ValueTask<int> CountAsync(VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default)
    {
        var queryable = _workflowBlueprints.AsQueryable().WithVersion(versionOptions);

        if (tenantId != null)
            queryable = queryable.Where(x => x.TenantId == tenantId);

        var count = queryable.Count();
        return new ValueTask<int>(count);
    }

    public override ValueTask<IWorkflowBlueprint?> FindAsync(string definitionId, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default)
    {
        var queryable = _workflowBlueprints.AsQueryable().Where(x => x.Id == definitionId).WithVersion(versionOptions);

        if (tenantId != null)
            queryable = queryable.Where(x => x.TenantId == tenantId);

        var workflowBlueprint = queryable.FirstOrDefault();
        return new ValueTask<IWorkflowBlueprint?>(workflowBlueprint);
    }

    public override ValueTask<IWorkflowBlueprint?> FindByDefinitionVersionIdAsync(string definitionVersionId, string? tenantId = default, CancellationToken cancellationToken = default)
    {
        var queryable = _workflowBlueprints.AsQueryable().Where(x => x.VersionId == definitionVersionId);

        if (tenantId != null)
            queryable = queryable.Where(x => x.TenantId == tenantId);

        var workflowBlueprint = queryable.FirstOrDefault();
        return new ValueTask<IWorkflowBlueprint?>(workflowBlueprint);
    }

    public override ValueTask<IWorkflowBlueprint?> FindByNameAsync(string name, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default)
    {
        var queryable = _workflowBlueprints.AsQueryable().Where(x => x.Name == name).WithVersion(versionOptions);

        if (tenantId != null)
            queryable = queryable.Where(x => x.TenantId == tenantId);

        var workflowBlueprint = queryable.FirstOrDefault();
        return new ValueTask<IWorkflowBlueprint?>(workflowBlueprint);
    }

    public override ValueTask<IWorkflowBlueprint?> FindByTagAsync(string tag, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default)
    {
        var queryable = _workflowBlueprints.AsQueryable().Where(x => x.Tag == tag).WithVersion(versionOptions);

        if (tenantId != null)
            queryable = queryable.Where(x => x.TenantId == tenantId);

        var workflowBlueprint = queryable.FirstOrDefault();
        return new ValueTask<IWorkflowBlueprint?>(workflowBlueprint);
    }

    public override ValueTask<IEnumerable<IWorkflowBlueprint>> FindManyByDefinitionIds(IEnumerable<string> definitionIds, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var ids = definitionIds.ToList();
        var workflowBlueprints = _workflowBlueprints.Where(x => ids.Contains(x.Id) && x.WithVersion(versionOptions));
        return new ValueTask<IEnumerable<IWorkflowBlueprint>>(workflowBlueprints);
    }

    public override ValueTask<IEnumerable<IWorkflowBlueprint>> FindManyByDefinitionVersionIds(IEnumerable<string> definitionVersionIds, CancellationToken cancellationToken = default)
    {
        var ids = definitionVersionIds.ToList();
        var workflowBlueprints = _workflowBlueprints.Where(x => ids.Contains(x.VersionId));
        return new ValueTask<IEnumerable<IWorkflowBlueprint>>(workflowBlueprints);
    }

    public override ValueTask<IEnumerable<IWorkflowBlueprint>> FindManyByNames(IEnumerable<string> names, CancellationToken cancellationToken = default)
    {
        var nameList = names.ToList();
        var workflowBlueprints = _workflowBlueprints.Where(x => x.Name != null && nameList.Contains(x.Name));
        return new ValueTask<IEnumerable<IWorkflowBlueprint>>(workflowBlueprints);
    }

    public override ValueTask<IEnumerable<IWorkflowBlueprint>> FindManyByTagAsync(string tag, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default)
    {
        var workflowBlueprints = _workflowBlueprints.Where(x => string.Equals(x.Tag, tag, StringComparison.OrdinalIgnoreCase));
        return new ValueTask<IEnumerable<IWorkflowBlueprint>>(workflowBlueprints);
    }
}