using Elsa.Scheduling.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;

namespace Elsa.Quartz.Handlers;

/// <summary>
/// Modifies the <see cref="ActivityDescriptor"/> of the <see cref="Cron"/> activity.
/// </summary>
public class CronActivityDescriptorModifier : IActivityDescriptorModifier
{
    /// <inheritdoc />
    public void Modify(ActivityDescriptor descriptor)
    {
        if (descriptor.TypeName != ActivityTypeNameHelper.GenerateTypeName<Cron>())
            return;
        
        descriptor.Description = "Schedules the execution of the activity using a cron expression using Quartz.NET.";
                
        var cronExpressionInput = descriptor.Inputs.First(x => x.Name == nameof(Cron.CronExpression));
        cronExpressionInput.Description = "The Quartz.NET cron expression to use for scheduling the activity.";
    }
}