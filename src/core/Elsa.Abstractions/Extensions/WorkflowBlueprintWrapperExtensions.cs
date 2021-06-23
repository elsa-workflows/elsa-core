using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa
{
    public static class WorkflowBlueprintWrapperExtensions
    {
        public static IEnumerable<IActivityBlueprintWrapper<TActivity>> Filter<TActivity>(this IWorkflowBlueprintWrapper workflowBlueprintWrapper, Func<IActivityBlueprintWrapper<TActivity>, bool>? predicate = default)
            where TActivity : IActivity
        {
            var query = workflowBlueprintWrapper.Activities.Where(x => x.ActivityBlueprint.Type == typeof(TActivity).Name).Select(x => x.As<TActivity>());

            if (predicate != null)
                query = query.Where(predicate);

            return query;
        }

        public static IActivityBlueprintWrapper<TActivity>? FindActivity<TActivity>(this IWorkflowBlueprintWrapper workflowBlueprintWrapper, Func<IActivityBlueprintWrapper<TActivity>, bool>? predicate = default) where TActivity : IActivity => workflowBlueprintWrapper.Filter(predicate).FirstOrDefault();
        public static IActivityBlueprintWrapper<TActivity> GetActivity<TActivity>(this IWorkflowBlueprintWrapper workflowBlueprintWrapper, string id) where TActivity : IActivity => workflowBlueprintWrapper.FindActivity<TActivity>(x => x.ActivityBlueprint.Id == id)!;
        public static IActivityBlueprintWrapper? GetActivity(this IWorkflowBlueprintWrapper workflowBlueprintWrapper, string id) => workflowBlueprintWrapper.Activities.FirstOrDefault(x => x.ActivityBlueprint.Id == id);
        
        public static IActivityBlueprintWrapper<TActivity>? FindUnfilteredActivity<TActivity>(this IWorkflowBlueprintWrapper workflowBlueprintWrapper, Func<IActivityBlueprintWrapper<TActivity>, bool> predicate) where TActivity : IActivity => workflowBlueprintWrapper.Activities.Select(x => x.As<TActivity>()).Where(predicate).FirstOrDefault();

        public static IActivityBlueprintWrapper<TActivity> GetUnfilteredActivity<TActivity>(this IWorkflowBlueprintWrapper workflowBlueprintWrapper, string id) where TActivity : IActivity => workflowBlueprintWrapper.FindUnfilteredActivity<TActivity>(x => x.ActivityBlueprint.Id == id)!;
    }
}