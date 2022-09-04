using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using LinqKit;

namespace Elsa.Common.Extensions;

public static class VersionedEntityExtensions
{
    public static bool WithVersion(this VersionedEntity workflow, VersionOptions versionOptions)
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

    public static IEnumerable<T> WithVersion<T>(
        this IEnumerable<T> query,
        VersionOptions versionOptions) where T : VersionedEntity =>
        query.Where(x => x.WithVersion(versionOptions)).OrderByDescending(x => x.Version);

    public static IQueryable<T> WithVersion<T>(this IQueryable<T> query, VersionOptions versionOptions) where T : VersionedEntity
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

    public static Expression<Func<T, bool>> WithVersion<T>(this Expression<Func<T, bool>> predicate, VersionOptions versionOptions) where T:VersionedEntity
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
}