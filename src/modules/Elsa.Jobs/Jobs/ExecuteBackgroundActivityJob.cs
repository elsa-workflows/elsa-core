using System.Threading.Tasks;
using Elsa.Jobs.Abstractions;
using Elsa.Jobs.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Jobs.Jobs;

public class ExecuteBackgroundActivityJob : Job
{
    public string WorkflowInstanceId { get; set; } = default!;
    public string ActivityId { get; set; } = default!;
    
    protected override async ValueTask ExecuteAsync(JobExecutionContext context)
    {
        var activityInvoker = context.GetRequiredService<IActivityInvoker>();
    }
}