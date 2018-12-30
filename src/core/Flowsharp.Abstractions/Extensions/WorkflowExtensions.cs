using System.Collections.Generic;
using System.Linq;
using Flowsharp.Models;

namespace Flowsharp.Extensions
{
    public static class WorkflowExtensions
    {
        public static bool IsDefinition(this Workflow workflow) => workflow.Metadata.ParentId == null;
        public static bool IsInstance(this Workflow workflow) => !workflow.IsDefinition();

        public static IEnumerable<IActivity> GetStartActivities(this Workflow workflow)
        {
            var query =
                from activity in workflow.Activities 
                where !workflow.Connections.Select(x => x.Target.Activity).Contains(activity)
                select activity;

            return query;
        }
    }
}