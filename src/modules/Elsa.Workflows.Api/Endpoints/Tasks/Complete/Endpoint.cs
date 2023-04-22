using Elsa.Abstractions;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.Api.Endpoints.Tasks.Complete;

/// <summary>
/// Resumes the <see cref="RunTask"/> activity matching the received Task ID.
/// </summary>
public class Complete : ElsaEndpoint<Request, Response>
{
    private readonly ITaskReporter _taskReporter;

    /// <inheritdoc />
    public Complete(ITaskReporter taskReporter)
    {
        _taskReporter = taskReporter;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/tasks/{taskId}/complete");
        ConfigurePermissions("tasks:complete");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        await _taskReporter.ReportCompletionAsync(request.TaskId, request.Result, cancellationToken);
        if (!HttpContext.Response.HasStarted) await SendOkAsync(cancellationToken);
    }
}
