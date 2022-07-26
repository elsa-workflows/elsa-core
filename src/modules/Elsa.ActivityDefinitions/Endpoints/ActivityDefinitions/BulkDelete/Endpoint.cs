using Elsa.ActivityDefinitions.Services;
using Elsa.Api.Common;

namespace Elsa.ActivityDefinitions.Endpoints.ActivityDefinitions.BulkDelete;

public class BulkDelete : ProtectedEndpoint<Request, Response>
{
    private readonly IActivityDefinitionStore _activityDefinitionStore;

    public BulkDelete(IActivityDefinitionStore activityDefinitionStore)
    {
        _activityDefinitionStore = activityDefinitionStore;
    }

    public override void Configure()
    {
        Post("/bulk-actions/delete/activity-definitions/by-definition-id");
        ConfigureSecurity(SecurityConstants.Permissions, SecurityConstants.Policies, SecurityConstants.Roles);
    }

    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var deleted = await _activityDefinitionStore.DeleteByDefinitionIdsAsync(request.DefinitionIds, cancellationToken);

        return new Response
        {
            Deleted = deleted
        };
    }
}