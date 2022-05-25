using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Elsa.Jobs.Abstractions;
using Elsa.Jobs.Models;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Scheduling.Jobs;

public class ResumeWorkflowJob : Job
{
    [JsonConstructor]
    public ResumeWorkflowJob()
    {
    }

    public ResumeWorkflowJob(string workflowInstanceId, Bookmark bookmark)
    {
        WorkflowInstanceId = workflowInstanceId;
        Bookmark = bookmark;
    }

    public string WorkflowInstanceId { get; set; } = default!;
    public Bookmark Bookmark { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(JobExecutionContext context)
    {
        var request = new DispatchWorkflowInstanceRequest(WorkflowInstanceId, Bookmark);
        var workflowDispatcher = context.GetRequiredService<IWorkflowDispatcher>();
        await workflowDispatcher.DispatchAsync(request, context.CancellationToken);
    }
}