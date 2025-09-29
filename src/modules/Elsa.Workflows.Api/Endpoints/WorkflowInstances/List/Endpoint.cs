using Elsa.Abstractions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.List;

[UsedImplicitly]
internal class List(IWorkflowInstanceStore store) : ElsaEndpoint<Request, Response>
{
    public override void Configure()
    {
        Verbs(FastEndpoints.Http.GET, FastEndpoints.Http.POST);
        Routes("/workflow-instances");
        ConfigurePermissions("read:workflow-instances");
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var pageArgs = PageArgs.FromPage(request.Page, request.PageSize);
        var workflowStatuses = request.Statuses?.Any() == true ? ParseEnumStrings<WorkflowStatus>(request.Statuses).ToList() : null;
        var workflowSubStatuses = request.SubStatuses?.Any() == true ? ParseEnumStrings<WorkflowSubStatus>(request.SubStatuses).ToList() : null;

        ValidateInput(request);

        if (ValidationFailed)
        {
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
            return;
        }

        var filter = new WorkflowInstanceFilter
        {
            IsSystem = request.IsSystem,
            SearchTerm = request.SearchTerm,
            Name = request.Name,
            DefinitionId = request.DefinitionId,
            DefinitionIds = request.DefinitionIds?.Any() == true ? request.DefinitionIds : null,
            Version = request.Version,
            CorrelationId = request.CorrelationId,
            WorkflowStatus = request.Status,
            WorkflowSubStatus = request.SubStatus,
            WorkflowStatuses = workflowStatuses,
            WorkflowSubStatuses = workflowSubStatuses,
            HasIncidents = request.HasIncidents,
            TimestampFilters = request.TimestampFilters?.Any() == true ? request.TimestampFilters : null,
        };

        var summaries = await FindAsync(request, filter, pageArgs, cancellationToken);
        var response = new Response(summaries.Items, summaries.TotalCount);
        await Send.OkAsync(response, cancellationToken);
    }

    private IEnumerable<TEnum> ParseEnumStrings<TEnum>(IEnumerable<string> strings) where TEnum : struct
    {
        foreach (string s in strings)
        {
            if (Enum.TryParse<TEnum>(s, true, out var result))
                yield return result;
            else
            {
                AddError($"Invalid enum value '{s}'.");
                yield break;
            }
        }
    }

    private bool ValidateInput(Request request)
    {
        if (request.Page is < 0)
        {
            AddError("Page must be greater than or equal to 1.");
            return false;
        }

        if (request.PageSize is < 1)
        {
            AddError("Page size must be greater than or equal to 1.");
            return false;
        }

        var columnWhitelist = new[]
        {
            "CreatedAt", "UpdatedAt", "FinishedAt"
        };

        if (request.TimestampFilters?.Any() == true)
        {
            foreach (var timestampFilter in request.TimestampFilters)
            {
                if (string.IsNullOrWhiteSpace(timestampFilter.Column))
                {
                    AddError("Column must be specified.");
                    return false;
                }

                if (!columnWhitelist.Contains(timestampFilter.Column))
                {
                    AddError($"Invalid column '{timestampFilter.Column}'.");
                    return false;
                }
            }
        }

        return true;
    }

    private async Task<Page<WorkflowInstanceSummary>> FindAsync(Request request, WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken)
    {
        request.OrderBy ??= OrderByWorkflowInstance.Created;
        var direction = request.OrderBy == OrderByWorkflowInstance.Name ? request.OrderDirection ?? OrderDirection.Ascending : request.OrderDirection ?? OrderDirection.Descending;

        switch (request.OrderBy)
        {
            default:
                {
                    var o = new WorkflowInstanceOrder<DateTimeOffset>
                    {
                        KeySelector = p => p.CreatedAt,
                        Direction = direction
                    };

                    return await store.SummarizeManyAsync(filter, pageArgs, o, cancellationToken);
                }
            case OrderByWorkflowInstance.UpdatedAt:
                {
                    var o = new WorkflowInstanceOrder<DateTimeOffset?>
                    {
                        KeySelector = p => p.UpdatedAt,
                        Direction = direction
                    };

                    return await store.SummarizeManyAsync(filter, pageArgs, o, cancellationToken);
                }
            case OrderByWorkflowInstance.Finished:
                {
                    var o = new WorkflowInstanceOrder<DateTimeOffset?>
                    {
                        KeySelector = p => p.FinishedAt,
                        Direction = direction
                    };

                    return await store.SummarizeManyAsync(filter, pageArgs, o, cancellationToken);
                }
            case OrderByWorkflowInstance.Name:
                {
                    var o = new WorkflowInstanceOrder<string>
                    {
                        KeySelector = p => p.Name,
                        Direction = direction
                    };

                    return await store.SummarizeManyAsync(filter, pageArgs, o, cancellationToken);
                }
        }
    }
}