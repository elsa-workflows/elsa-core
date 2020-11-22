using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa
{
    public static class WorkflowBlueprintWrapperExtensions
    {
        public static IEnumerable<IActivityBlueprintWrapper<TActivity>> Filter<TActivity>(this IWorkflowBlueprintWrapper workflowBlueprintWrapper, Func<TActivity, bool>? predicate = default) where TActivity : IActivity =>
            workflowBlueprintWrapper.Activities.Where(x => x.ActivityBlueprint.Type == typeof(TActivity).Name).Select(x => x.As<TActivity>());
    }
}