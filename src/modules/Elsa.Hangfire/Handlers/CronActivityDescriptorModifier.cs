using Elsa.Scheduling.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;

namespace Elsa.Hangfire.Handlers;

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
        
        descriptor.Description = "Schedules the execution of the activity using a cron expression using Hangfire.";
    }
}