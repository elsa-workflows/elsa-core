using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa
{
    public static class WorkflowBlueprintExtensions
    {
        public static ValueTask<T?> GetActivityPropertyValue<TActivity, T>(
            this IWorkflowBlueprint workflowBlueprint,
            string activityId,
            Expression<Func<TActivity, T>> propertyExpression,
            ActivityExecutionContext activityExecutionContext,
            CancellationToken cancellationToken) where TActivity : IActivity
        {
            var expression = (MemberExpression) propertyExpression.Body;
            string propertyName = expression.Member.Name;
            return GetActivityPropertyValue<T>(workflowBlueprint, activityId, propertyName, activityExecutionContext, cancellationToken);
        }

        public static async ValueTask<T?> GetActivityPropertyValue<T>(
            this IWorkflowBlueprint workflowBlueprint,
            string activityId,
            string propertyName,
            ActivityExecutionContext activityExecutionContext,
            CancellationToken cancellationToken)
        {
            var value = await workflowBlueprint.GetActivityPropertyValue(activityId, propertyName, activityExecutionContext, cancellationToken);
            return value == null ? default : (T) value;
        }

        public static async ValueTask<object?> GetActivityPropertyValue(
            this IWorkflowBlueprint workflowBlueprint,
            string activityId,
            string propertyName,
            ActivityExecutionContext activityExecutionContext,
            CancellationToken cancellationToken)
        {
            var provider = workflowBlueprint.ActivityPropertyProviders.GetProvider(activityId, propertyName);

            if (provider == null)
                return default!;

            var value = await provider.GetValueAsync(activityExecutionContext, cancellationToken);
            return value!;
        }

        public static IEnumerable<IWorkflowBlueprint> WithVersion(
            this IEnumerable<IWorkflowBlueprint> query,
            VersionOptions version) =>
            query.AsQueryable().WithVersion(version);

        public static IQueryable<IWorkflowBlueprint> WithVersion(
            this IQueryable<IWorkflowBlueprint> query,
            VersionOptions version) =>
            query.Where(x => x.WithVersion(version)).OrderByDescending(x => x.Version);

        public static bool WithVersion(this IWorkflowBlueprint workflowBlueprint, VersionOptions version)
        {
            if (version.IsDraft)
                return !workflowBlueprint.IsPublished;
            if (version.IsLatest)
                return workflowBlueprint.IsLatest;
            if (version.IsPublished)
                return workflowBlueprint.IsPublished;
            if (version.IsLatestOrPublished)
                return workflowBlueprint.IsPublished || workflowBlueprint.IsLatest;
            if (version.AllVersions)
                return true;
            if (version.Version > 0)
                return workflowBlueprint.Version == version.Version;
            return true;
        }
    }
}