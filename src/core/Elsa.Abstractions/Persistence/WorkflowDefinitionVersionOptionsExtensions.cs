using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Elsa.Models;
using LinqKit;

namespace Elsa.Persistence
{
    public static class WorkflowDefinitionVersionOptionsExtensions
    {
        
        public static bool WithVersion(this WorkflowDefinition workflowDefinition, VersionOptions? version = default)
        {
            var versionOption = version ?? VersionOptions.Latest;

            if (versionOption.IsDraft)
                return !workflowDefinition.IsPublished;
            if (versionOption.IsLatest)
                return workflowDefinition.IsLatest;
            if (versionOption.IsPublished)
                return workflowDefinition.IsPublished;
            if (versionOption.IsLatestOrPublished)
                return workflowDefinition.IsPublished || workflowDefinition.IsLatest;
            if (versionOption.Version > 0)
                return workflowDefinition.Version == versionOption.Version;

            return true;
        }
        
        public static IEnumerable<WorkflowDefinition> WithVersion(this IEnumerable<WorkflowDefinition> query, VersionOptions? version = default)
        {
            var versionOption = version ?? VersionOptions.Latest;

            if (versionOption.IsDraft)
                return query.Where(x => !x.IsPublished);
            if (versionOption.IsLatest)
                return query.Where(x => x.IsLatest);
            if (versionOption.IsPublished)
                return query.Where(x => x.IsPublished);
            if (versionOption.IsLatestOrPublished)
                return query.Where(x => x.IsPublished || x.IsLatest);
            if (versionOption.Version > 0)
                return query.Where(x => x.Version == versionOption.Version);

            return query;
        }

        public static IQueryable<WorkflowDefinition> WithVersion(this IQueryable<WorkflowDefinition> query, VersionOptions? version = default)
        {
            var versionOption = version ?? VersionOptions.Latest;

            if (versionOption.IsDraft)
                return query.Where(x => !x.IsPublished);
            if (versionOption.IsLatest)
                return query.Where(x => x.IsLatest);
            if (versionOption.IsPublished)
                return query.Where(x => x.IsPublished);
            if (versionOption.IsLatestOrPublished)
                return query.Where(x => x.IsPublished || x.IsLatest);
            if (versionOption.Version > 0)
                return query.Where(x => x.Version == versionOption.Version);

            return query;
        }
        
        public static Expression<Func<WorkflowDefinition, bool>> WithVersion(this Expression<Func<WorkflowDefinition, bool>> predicate, VersionOptions? version = default)
        {
            var versionOption = version ?? VersionOptions.Latest;

            if (versionOption.IsDraft)
                return predicate.And(x => !x.IsPublished);
            if (versionOption.IsLatest)
                return predicate.And(x => x.IsLatest);
            if (versionOption.IsPublished)
                return predicate.And(x => x.IsPublished);
            if (versionOption.IsLatestOrPublished)
                return predicate.And(x => x.IsPublished || x.IsLatest);
            if (versionOption.Version > 0)
                return predicate.And(x => x.Version == versionOption.Version);

            return predicate;
        }
    }
}