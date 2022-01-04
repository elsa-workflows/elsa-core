using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Persistence.Models;

namespace Elsa.Persistence.Extensions;

public static class WorkflowExtensions
{
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

    public static IEnumerable<Workflow> WithVersion(
        this IEnumerable<Workflow> query,
        VersionOptions version) =>
        query.Where(x => x.WithVersion(version)).OrderByDescending(x => x.Identity.Version);

    public static IQueryable<Workflow> WithVersion(
        this IQueryable<Workflow> query,
        VersionOptions version) =>
        query.Where(x => x.WithVersion(version)).OrderByDescending(x => x.Identity.Version);
}