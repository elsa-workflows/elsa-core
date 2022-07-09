using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.DataMasking.Core.Contracts;
using Elsa.DataMasking.Core.Models;
using Elsa.Server.Core.Events;
using MediatR;

namespace Elsa.DataMasking.Core.Handlers;

public class ApplyActivityStateFilters : INotificationHandler<RequestingWorkflowInstance>
{
    private readonly IEnumerable<IActivityStateFilter> _filters;

    public ApplyActivityStateFilters(IEnumerable<IActivityStateFilter> filters)
    {
        _filters = filters;
    }
    
    public async Task Handle(RequestingWorkflowInstance notification, CancellationToken cancellationToken)
    {
        var workflowBlueprint = notification.WorkflowBlueprint;
        var activityData = notification.WorkflowInstance.ActivityData;

        foreach (var entry in activityData.Where(x => x.Key != workflowBlueprint.Id))
        {
            var activityId = entry.Key;
            var activityBlueprint = workflowBlueprint.GetActivity(activityId)!;
            var context = new ActivityStateFilterContext(entry.Value, activityBlueprint);

            foreach (var filter in _filters) await filter.ApplyAsync(context);
        }
    }
}