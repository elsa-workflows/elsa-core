using Elsa.Authorization;
using Elsa.Abstractions;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Models;
using Elsa.Workflows.Api.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.List;

[PublicAPI]
internal class List(IWorkflowDefinitionStore store, IWorkflowDefinitionLinker linker, IEnumerable<IWorkflowDefinitionFilterProvider>? filterProviders = null) : ElsaEndpoint<Request, PagedListResponse<LinkedWorkflowDefinitionSummary>>
{
    public override void Configure()
    {
        Get("/workflow-definitions");
        RequirePermission(Elsa.Workflows.Api.Permissions.WorkflowPermissions.Definitions, CoreVerbs.View);
    }

    public override async Task<PagedListResponse<LinkedWorkflowDefinitionSummary>> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var pageArgs = PageArgs.FromPage(request.Page, request.PageSize);
        var filter = CreateFilter(request);
        try
        {
            foreach (var filterProvider in filterProviders ?? [])
            {
                if (!filterProvider.CanApply(filter))
                {
                    continue;
                }

                if (EndpointSecurityOptions.SecurityIsEnabled && !HasRequiredPermissions(filterProvider, filter))
                {
                    AddError("You do not have permission to filter workflow definitions by labels.");
                    await Send.ErrorsAsync(StatusCodes.Status403Forbidden, cancellationToken);
                    return default!;
                }

                await filterProvider.ApplyAsync(filter, cancellationToken);
            }
        }
        catch (WorkflowDefinitionFilterNotSupportedException exception)
        {
            AddError(exception.Message);
            await Send.ErrorsAsync(StatusCodes.Status501NotImplemented, cancellationToken);
            return default!;
        }

        if (filter.LabelIds is { Count: > 0 })
        {
            AddError("Filtering workflow definitions by labels is not supported by the configured modules.");
            await Send.ErrorsAsync(StatusCodes.Status501NotImplemented, cancellationToken);
            return default!;
        }

        var summaries = await FindAsync(request, filter, pageArgs, cancellationToken);
        var pagedList = new PagedListResponse<WorkflowDefinitionSummary>(summaries);
        var response = linker.MapAsync(pagedList);
        return response;
    }

    private bool HasRequiredPermissions(IWorkflowDefinitionFilterProvider filterProvider, WorkflowDefinitionFilter filter)
    {
        var evaluator = HttpContext.RequestServices.GetService<IPermissionEvaluator>() ?? PermissionEvaluator.Shared;
        return filterProvider.GetRequiredPermissions(filter).All(permission => evaluator.HasPermission(HttpContext.User, permission));
    }

    private WorkflowDefinitionFilter CreateFilter(Request request)
    {
        var versionOptions = string.IsNullOrWhiteSpace(request.VersionOptions) ? default(VersionOptions?) : VersionOptions.FromString(request.VersionOptions);

        return new()
        {
            IsSystem = request.IsSystem,
            VersionOptions = versionOptions,
            SearchTerm = request.SearchTerm?.Trim(),
            MaterializerName = request.MaterializerName,
            DefinitionIds = request.DefinitionIds,
            Ids = request.Ids,
            LabelIds = request.Labels
        };
    }

    private async Task<Page<WorkflowDefinitionSummary>> FindAsync(Request request, WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken)
    {
        request.OrderBy ??= OrderByWorkflowDefinition.Name;

        var direction = request.OrderBy == OrderByWorkflowDefinition.Name ? request.OrderDirection ?? OrderDirection.Ascending : request.OrderDirection ?? OrderDirection.Descending;

        switch (request.OrderBy)
        {
            default:
                {
                    var order = new WorkflowDefinitionOrder<DateTimeOffset>
                    {
                        KeySelector = p => p.CreatedAt,
                        Direction = direction
                    };

                    return await store.FindSummariesAsync(filter, order, pageArgs, cancellationToken);
                }
            case OrderByWorkflowDefinition.Name:
                {
                    var order = new WorkflowDefinitionOrder<string>
                    {
                        KeySelector = p => p.Name!,
                        Direction = direction
                    };

                    return await store.FindSummariesAsync(filter, order, pageArgs, cancellationToken);
                }
            case OrderByWorkflowDefinition.Version:
                {
                    var order = new WorkflowDefinitionOrder<int>
                    {
                        KeySelector = p => p.Version,
                        Direction = direction
                    };

                    return await store.FindSummariesAsync(filter, order, pageArgs, cancellationToken);
                }
        }
    }
}