using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Models;
using Elsa.Jobs.Abstractions;
using Elsa.Jobs.Contracts;

namespace Elsa.Modules.Scheduling.Jobs;

public record ResumeWorkflowJob(string WorkflowInstanceId, Bookmark Bookmark) : IJob
{
    public string JobId => $"Bookmark:{Bookmark.Id}";
}

public class ResumeWorkflowJobHandler : JobHandler<ResumeWorkflowJob>
{
    private readonly IWorkflowInvoker _workflowInvoker;
    public ResumeWorkflowJobHandler(IWorkflowInvoker workflowInvoker) => _workflowInvoker = workflowInvoker;

    protected override async Task HandleAsync(ResumeWorkflowJob job, CancellationToken cancellationToken)
    {
        var request = new DispatchWorkflowInstanceRequest(job.WorkflowInstanceId, job.Bookmark);
        await _workflowInvoker.DispatchAsync(request, cancellationToken);
    }
}