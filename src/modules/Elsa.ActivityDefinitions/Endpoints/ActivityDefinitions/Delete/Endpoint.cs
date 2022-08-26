using Elsa.ActivityDefinitions.Services;
using FastEndpoints;

namespace Elsa.ActivityDefinitions.Endpoints.ActivityDefinitions.Delete;

/// <summary>
/// An endpoint that deletes a specific activity definition by ID.
/// </summary>
public class Delete : Endpoint<Request>
{
    private readonly IActivityDefinitionStore _activityDefinitionStore;

    /// <inheritdoc />
    public Delete(IActivityDefinitionStore activityDefinitionStore)
    {
        _activityDefinitionStore = activityDefinitionStore;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/activity-definitions/{definitionId}");
        Policies(Constants.PolicyName);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        await _activityDefinitionStore.DeleteByDefinitionIdAsync(request.DefinitionId, cancellationToken);
        await SendNoContentAsync(cancellationToken);
    }
}