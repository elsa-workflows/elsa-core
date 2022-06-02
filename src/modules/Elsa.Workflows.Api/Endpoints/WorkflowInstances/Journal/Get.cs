using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.AspNetCore.Attributes;
using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Persistence.Services;
using Microsoft.AspNetCore.Mvc;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Journal;

[Area(AreaNames.Elsa)]
[ApiEndpoint(ControllerNames.WorkflowJournal, "Get")]
public class Get : Controller
{
    private readonly IWorkflowExecutionLogStore _store;
    private readonly WorkflowSerializerOptionsProvider _serializerOptionsProvider;

    public Get(IWorkflowExecutionLogStore store, WorkflowSerializerOptionsProvider serializerOptionsProvider)
    {
        _store = store;
        _serializerOptionsProvider = serializerOptionsProvider;
    }

    [HttpGet]
    public async Task<IActionResult> HandleAsync(
        [FromRoute(Name = "id")] string workflowInstanceId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
        var pageArgs = new PageArgs(page, pageSize);
        var pageOfRecords = await _store.FindManyByWorkflowInstanceIdAsync(workflowInstanceId, pageArgs, cancellationToken);

        var models = pageOfRecords.Items.Select(x =>
                new WorkflowExecutionLogRecordModel(
                    x.Id,
                    x.ActivityId,
                    x.ActivityType,
                    x.Timestamp,
                    x.EventName,
                    x.Message,
                    x.Source,
                    x.Payload))
            .ToList();

        var pageOfModels = Page.Of(models, pageOfRecords.TotalCount);

        return Json(pageOfModels, serializerOptions);
    }

    public record WorkflowExecutionLogRecordModel(
        string Id,
        string ActivityId,
        string ActivityType,
        DateTimeOffset Timestamp,
        string? EventName,
        string? Message,
        string? Source,
        object? Payload);
}