using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions;
using Elsa.Models;
using Elsa.Workflows.Persistence.Services;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Journal.Get;

public class Get : ElsaEndpoint<Request, Response>
{
    private readonly IWorkflowExecutionLogStore _store;

    public Get(IWorkflowExecutionLogStore store)
    {
        _store = store;
    }

    public override void Configure()
    {
        Get("/workflow-instances/{id}/journal");
        ConfigurePermissions("read:workflow-instances");
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var pageArgs = new PageArgs(request.Page, request.PageSize);
        var pageOfRecords = await _store.FindManyByWorkflowInstanceIdAsync(request.WorkflowInstanceId, pageArgs, cancellationToken);

        var models = pageOfRecords.Items.Select(x =>
                new ExecutionLogRecord(
                    x.Id,
                    x.ActivityInstanceId,
                    x.ParentActivityInstanceId,
                    x.ActivityId,
                    x.ActivityType,
                    x.Timestamp,
                    x.EventName,
                    x.Message,
                    x.Source,
                    x.Payload))
            .ToList();

        return new(models, pageOfRecords.TotalCount);
    }
}