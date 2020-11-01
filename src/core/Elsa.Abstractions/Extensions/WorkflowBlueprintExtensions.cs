using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Newtonsoft.Json;

namespace Elsa
{
    public static class WorkflowBlueprintExtensions
    {
        public static ValueTask<T> GetActivityPropertyValue<TActivity, T>(
            this IWorkflowBlueprint workflowBlueprint, 
            string activityId, 
            Expression<Func<TActivity, T>> propertyExpression, 
            ActivityExecutionContext activityExecutionContext, 
            CancellationToken cancellationToken) where TActivity : IActivity
        {
            var expression = (MemberExpression)propertyExpression.Body;
            string propertyName = expression.Member.Name;
            return GetActivityPropertyValue<T>(workflowBlueprint, activityId, propertyName, activityExecutionContext, cancellationToken);
        }
        
        public static async ValueTask<T> GetActivityPropertyValue<T>(
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
            return (T)value!;
        }
        
        public static T GetActivityState<TActivity, T>(
            this IWorkflowBlueprint workflowBlueprint, 
            string activityId, 
            Expression<Func<TActivity, T>> propertyExpression, 
            ActivityExecutionContext activityExecutionContext) where TActivity : IActivity
        {
            var expression = (MemberExpression)propertyExpression.Body;
            string propertyName = expression.Member.Name;
            return GetActivityState<T>(workflowBlueprint, activityId, propertyName, activityExecutionContext);
        }
        
        public static T GetActivityState<T>(
            this IWorkflowBlueprint workflowBlueprint, 
            string activityId, 
            string propertyName, 
            ActivityExecutionContext activityExecutionContext)
        {
            var activity = activityExecutionContext.ActivityInstance;
            var serializer = activityExecutionContext.GetService<JsonSerializer>();
            return activity.Data[propertyName]!.ToObject<T>(serializer)!;
        }
        
        public static IEnumerable<IWorkflowBlueprint> WithVersion(
            this IEnumerable<IWorkflowBlueprint> query,
            VersionOptions version) =>
            query.AsQueryable().WithVersion(version);

        public static IQueryable<IWorkflowBlueprint> WithVersion(
            this IQueryable<IWorkflowBlueprint> query,
            VersionOptions version)
        {
            if (version.IsDraft)
                query = query.Where(x => !x.IsPublished);
            else if (version.IsLatest)
                query = query.Where(x => x.IsLatest);
            else if (version.IsPublished)
                query = query.Where(x => x.IsPublished);
            else if (version.IsLatestOrPublished)
                query = query.Where(x => x.IsPublished || x.IsLatest);
            else if (version.AllVersions)
            {
                // Nothing to filter.
            }
            else if (version.Version > 0)
                query = query.Where(x => x.Version == version.Version);

            return query.OrderByDescending(x => x.Version);
        }
    }
}