using System.Linq.Expressions;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Models;
using LinqKit;

namespace Elsa.Workflows.Persistence.Extensions;

public static class WorkflowDefinitionExtensions
{
    public static bool WithVersion(this WorkflowDefinition workflow, VersionOptions versionOptions)
    {
        var isPublished = workflow.IsPublished;
        var isLatest = workflow.IsLatest;
        var version = workflow.Version;

        if (versionOptions.IsDraft)
            return !isPublished;
        if (versionOptions.IsLatest)
            return isLatest;
        if (versionOptions.IsPublished)
            return isPublished;
        if (versionOptions.IsLatestOrPublished)
            return isPublished || isLatest;
        if (versionOptions.AllVersions)
            return true;
        if (versionOptions.Version > 0)
            return version == versionOptions.Version;
        return true;
    }

    public static IEnumerable<WorkflowDefinition> WithVersion(
        this IEnumerable<WorkflowDefinition> query,
        VersionOptions versionOptions) =>
        query.Where(x => x.WithVersion(versionOptions)).OrderByDescending(x => x.Version);

    public static IQueryable<WorkflowDefinition> WithVersion(this IQueryable<WorkflowDefinition> query, VersionOptions versionOptions)
    {
        if (versionOptions.IsDraft)
            return query.Where(x => !x.IsPublished);
        if (versionOptions.IsLatest)
            return query.Where(x => x.IsLatest);
        if (versionOptions.IsPublished)
            return query.Where(x => x.IsPublished);
        if (versionOptions.IsLatestOrPublished)
            return query.Where(x => x.IsPublished || x.IsLatest);
        if (versionOptions.Version > 0)
            return query.Where(x => x.Version == versionOptions.Version);

        return query;
    }

    public static Expression<Func<WorkflowDefinition, bool>> WithVersion(this Expression<Func<WorkflowDefinition, bool>> predicate, VersionOptions versionOptions)
    {
        if (versionOptions.IsDraft)
            return predicate.And(x => !x.IsPublished);
        if (versionOptions.IsLatest)
            return predicate.And(x => x.IsLatest);
        if (versionOptions.IsPublished)
            return predicate.And(x => x.IsPublished);
        if (versionOptions.IsLatestOrPublished)
            return predicate.And(x => x.IsPublished || x.IsLatest);
        if (versionOptions.Version > 0)
            return predicate.And(x => x.Version == versionOptions.Version);

        return predicate;
    }
    
    public static Workflow ToWorkflow(this WorkflowDefinition definition, IActivity root)
    {
        return new Workflow(
            new WorkflowIdentity(definition.DefinitionId, definition.Version, definition.Id),
            new WorkflowPublication(definition.IsLatest, definition.IsPublished),
            new WorkflowMetadata(definition.Name, definition.Description, definition.CreatedAt),
            root,
            definition.Variables,
            definition.Metadata,
            definition.ApplicationProperties);
    }
}