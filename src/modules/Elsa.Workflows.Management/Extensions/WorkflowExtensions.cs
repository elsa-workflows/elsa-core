using Elsa.Common.Models;
using Elsa.Workflows.Core.Activities;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides a set of extension methods for <see cref="Workflow"/>.
/// </summary>
[PublicAPI]
public static class WorkflowExtensions
{
    /// <summary>
    /// Returns true if the specified workflow matches the specified version options.
    /// </summary>
    /// <param name="workflow">The workflow to check.</param>
    /// <param name="version">The version options.</param>
    /// <returns>True if the workflow matches the version options; otherwise, false.</returns>
    public static bool WithVersion(this Workflow workflow, VersionOptions version)
    {
        var identity = workflow.Identity;
        var (isLatest, isPublished) = workflow.Publication;

        if (version.IsDraft)
            return !isPublished;
        if (version.IsLatest)
            return isLatest;
        if (version.IsPublished)
            return isPublished;
        if (version.IsLatestOrPublished)
            return isPublished || isLatest;
        if (version.AllVersions)
            return true;
        if (version.Version > 0)
            return identity.Version == version.Version;
        return true;
    }

    /// <summary>
    /// Applies the specified version options to the query.
    /// </summary>
    /// <param name="query">The query to apply the version options to.</param>
    /// <param name="version">The version options.</param>
    /// <returns>The query.</returns>
    public static IEnumerable<Workflow> WithVersion(
        this IEnumerable<Workflow> query,
        VersionOptions version) =>
        query.Where(x => x.WithVersion(version)).OrderByDescending(x => x.Identity.Version);

    /// <summary>
    /// Applies the specified version options to the query.
    /// </summary>
    /// <param name="query">The query to apply the version options to.</param>
    /// <param name="version">The version options.</param>
    /// <returns>The query.</returns>
    public static IQueryable<Workflow> WithVersion(
        this IQueryable<Workflow> query,
        VersionOptions version) =>
        query.Where(x => x.WithVersion(version)).OrderByDescending(x => x.Identity.Version);
}