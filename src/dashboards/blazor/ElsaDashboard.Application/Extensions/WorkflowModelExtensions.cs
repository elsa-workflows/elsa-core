using System.Collections.Generic;
using System.Linq;
using ElsaDashboard.Application.Models;

namespace ElsaDashboard.Application.Extensions
{
    public static class WorkflowModelExtensions
    {
        public static IEnumerable<ActivityModel> GetChildActivities(this WorkflowModel workflowModel, string? parentId)
        {
            if (parentId == null)
            {
                var targetIds = workflowModel.Connections.Select(x => x.TargetId).Distinct().ToLookup(x => x);
                var children = workflowModel.Activities.Where(x => !targetIds.Contains(x.ActivityId)).ToList();

                return children;
            }
            else
            {
                var targetIds = workflowModel.Connections.Where(x => x.SourceId == parentId).Select(x => x.TargetId).Distinct().ToLookup(x => x);
                var children = workflowModel.Activities.Where(x => targetIds.Contains(x.ActivityId)).ToList();
                
                return children;
            }
        }
    }
}