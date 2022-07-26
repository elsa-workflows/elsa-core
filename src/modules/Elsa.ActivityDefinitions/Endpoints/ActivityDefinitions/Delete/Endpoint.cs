using Elsa.ActivityDefinitions.Services;
using Elsa.Api.Common;

namespace Elsa.ActivityDefinitions.Endpoints.ActivityDefinitions.Delete;

public class Delete : ProtectedEndpoint<Request>
{
    private readonly IActivityDefinitionStore _activityDefinitionStore;

    public Delete(IActivityDefinitionStore activityDefinitionStore)
    {
        _activityDefinitionStore = activityDefinitionStore;
    }

    public override void Configure()
    {
        Delete("/activity-definitions/{definitionId}");
        ConfigureSecurity(SecurityConstants.Permissions, SecurityConstants.Policies, SecurityConstants.Roles);
    }

    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        await _activityDefinitionStore.DeleteByDefinitionIdAsync(request.DefinitionId, cancellationToken);
        await SendNoContentAsync(cancellationToken);
    }
}