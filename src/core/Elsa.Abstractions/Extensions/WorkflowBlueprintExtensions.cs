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
        public static ValueTask<T?> EvaluateActivityPropertyValue<TActivity, T>(
            this IWorkflowBlueprint workflowBlueprint,
            string activityId,
            Expression<Func<TActivity, T>> propertyExpression,
            ActivityExecutionContext activityExecutionContext,
            CancellationToken cancellationToken) where TActivity : IActivity
        {
            var expression = (MemberExpression) propertyExpression.Body;
            string propertyName = expression.Member.Name;
            return EvaluateActivityPropertyValue<T>(workflowBlueprint, activityId, propertyName, activityExecutionContext, cancellationToken);
        }

        public static async ValueTask<T?> EvaluateActivityPropertyValue<T>(
            this IWorkflowBlueprint workflowBlueprint,
            string activityId,
            string propertyName,
            ActivityExecutionContext activityExecutionContext,
            CancellationToken cancellationToken)
        {
            var value = await workflowBlueprint.EvaluateActivityPropertyValue(activityId, propertyName, activityExecutionContext, cancellationToken);
            return value == null ? default : (T) value;
        }

        public static async ValueTask<object?> EvaluateActivityPropertyValue(
            this IWorkflowBlueprint workflowBlueprint,
            string activityId,
            string propertyName,
            ActivityExecutionContext activityExecutionContext,
            CancellationToken cancellationToken)
        {
            var provider = workflowBlueprint.ActivityPropertyProviders.GetProvider(activityId, propertyName);
            var value = provider != null ? await provider.GetValueAsync(activityExecutionContext, cancellationToken) : null;

            if (value == null)
            {
                // Get default value.
                var activityTypeService = activityExecutionContext.GetService<IActivityTypeService>();
                var activityBlueprint = workflowBlueprint.GetActivity(activityId)!;
                var activityType = await activityTypeService.GetActivityTypeAsync(activityBlueprint.Type, cancellationToken);
                var activityDescriptor = await activityTypeService.DescribeActivityType(activityType, cancellationToken);
                var propertyDescriptor = activityDescriptor.InputProperties.FirstOrDefault(x => x.Name == propertyName);

                value = propertyDescriptor?.DefaultValue;
            }

            return value;
        }
        
        public static object? GetActivityPropertyRawValue(this IWorkflowBlueprint workflowBlueprint, string activityId, string propertyName)
        {
            var provider = workflowBlueprint.ActivityPropertyProviders.GetProvider(activityId, propertyName);
            var value = provider?.RawValue;
            
            return value;
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