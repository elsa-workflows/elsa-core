using Elsa.ActivityDefinitions.Models;
using Elsa.ActivityDefinitions.Services;
using Elsa.Api.Common;
using Elsa.Api.Common.Models;
using Elsa.Persistence.Common.Models;

namespace Elsa.ActivityDefinitions.Endpoints.ActivityDefinitions.List;

public class List : ProtectedEndpoint<Request, PagedListResponse<ActivityDefinitionSummary>>
{
    private readonly IActivityDefinitionStore _store;

    public List(IActivityDefinitionStore store)
    {
        _store = store;
    }

    public override void Configure()
    {
        Get("/activity-definitions");
        ConfigureSecurity(SecurityConstants.Permissions, SecurityConstants.Policies, SecurityConstants.Roles);
    }

    public override async Task<PagedListResponse<ActivityDefinitionSummary>> ExecuteAsync(Request req, CancellationToken ct)
    {
        var parsedVersionOptions = req.VersionOptions != null ? VersionOptions.FromString(req.VersionOptions) : VersionOptions.Published;
        var pageArgs = new PageArgs(req.Page, req.PageSize);
        var pageOfSummaries = await _store.ListSummariesAsync(parsedVersionOptions, pageArgs, ct);
        return new PagedListResponse<ActivityDefinitionSummary>(pageOfSummaries);
    }
}