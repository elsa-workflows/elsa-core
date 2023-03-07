using System.Text.Json.Serialization;
using Elsa.Jobs.Abstractions;
using Elsa.Jobs.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Scheduling.Jobs;

public class ResumeWorkflowJob : Job
{
    [JsonConstructor]
    public ResumeWorkflowJob()
    {
    }

    public ResumeWorkflowJob(string workflowInstanceId, string bookmarkId)
    {
        WorkflowInstanceId = workflowInstanceId;
        BookmarkId = bookmarkId;
    }

    public string WorkflowInstanceId { get; set; } = default!;
    public string BookmarkId { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(JobExecutionContext context)
    {
        var request = new DispatchWorkflowInstanceRequest(WorkflowInstanceId, BookmarkId);
        var workflowDispatcher = context.GetRequiredService<IWorkflowDispatcher>();
        await workflowDispatcher.DispatchAsync(request, context.CancellationToken);
    }
}