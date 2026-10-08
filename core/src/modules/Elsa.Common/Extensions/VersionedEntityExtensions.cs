using System.Linq.Expressions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using LinqKit;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for <see cref="VersionedEntity"/> objects.
/// </summary>
public static class VersionedEntityExtensions
{
    /// <summary>
    /// Returns true if the specified entity matches the version options.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="versionOptions">The version options.</param>
    /// <returns>True if the entity matches the version options.</returns>
    public static bool WithVersion(this VersionedEntity entity, VersionOptions versionOptions)
    {
        var isPublished = entity.IsPublished;
        var isLatest = entity.IsLatest;
        var version = entity.Version;

        if (versionOptions.IsDraft)
            return !isPublished;
        if (versionOptions.IsLatest)
            return isLatest;
        if (versionOptions.IsPublished)
            return isPublished;
        if (versionOptions.IsLatestOrPublished)
            return isPublished || isLatest;
        if (versionOptions.IsLatestAndPublished)
            return isPublished && isLatest;
        if (versionOptions.AllVersions)
            return true;
        if (versionOptions.Version > 0)
            return version == versionOptions.Version;
        return true;
    }

    /// <summary>
    /// Filters the specified enumerable by the version options.
    /// </summary>
    /// <param name="enumerable">The enumerable to filter.</param>
    /// <param name="versionOptions">The version options.</param>
    /// <typeparam name="T">The type of the enumerable.</typeparam>
    /// <returns>The filtered enumerable.</returns>
    public static IEnumerable<T> WithVersion<T>(
        this IEnumerable<T> enumerable,
        VersionOptions versionOptions) where T : VersionedEntity =>
        enumerable.Where(x => x.WithVersion(versionOptions)).OrderByDescending(x => x.Version);

    /// <summary>
    /// Filters the specified queryable by the version options.
    /// </summary>
    /// <param name="query">The queryable to filter.</param>
    /// <param name="versionOptions">The version options.</param>
    /// <typeparam name="T">The type of the queryable.</typeparam>
    /// <returns>The filtered queryable.</returns>
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
        if (versionOptions.IsLatestAndPublished)
            return query.Where(x => x.IsPublished && x.IsLatest);
        if (versionOptions.Version > 0)
            return query.Where(x => x.Version == versionOptions.Version);

        return query;
    }

    /// <summary>
    /// Returns an expression that filters the specified expression by the version options.
    /// </summary>
    /// <param name="expression">The expression to filter.</param>
    /// <param name="versionOptions">The version options.</param>
    /// <typeparam name="T">The type of the expression.</typeparam>
    /// <returns>The filtered expression.</returns>
    public static Expression<Func<T, bool>> WithVersion<T>(this Expression<Func<T, bool>> expression, VersionOptions versionOptions) where T : VersionedEntity
    {
        if (versionOptions.IsDraft)
            return expression.And(x => !x.IsPublished);
        if (versionOptions.IsLatest)
            return expression.And(x => x.IsLatest);
        if (versionOptions.IsPublished)
            return expression.And(x => x.IsPublished);
        if (versionOptions.IsLatestOrPublished)
            return expression.And(x => x.IsPublished || x.IsLatest);
        if (versionOptions.IsLatestAndPublished)
            return expression.And(x => x.IsPublished && x.IsLatest);
        if (versionOptions.Version > 0)
            return expression.And(x => x.Version == versionOptions.Version);

        return expression;
    }
}