using Elsa.ActivityDefinitions.Services;
using FastEndpoints;

namespace Elsa.ActivityDefinitions.Endpoints.ActivityDefinitions.BulkDelete;

/// <summary>
/// An endpoint that bulk-deletes activity definitions.
/// </summary>
public class BulkDelete : Endpoint<Request, Response>
{
    private readonly IActivityDefinitionStore _activityDefinitionStore;

    /// <inheritdoc />
    public BulkDelete(IActivityDefinitionStore activityDefinitionStore)
    {
        _activityDefinitionStore = activityDefinitionStore;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/bulk-actions/delete/activity-definitions/by-definition-id");
        Policies(Constants.PolicyName);
    }

    /// <inheritdoc />
    public override async Task<Response> ExecuteAsync(Request request, CancellationToken cancellationToken)
    {
        var deleted = await _activityDefinitionStore.DeleteByDefinitionIdsAsync(request.DefinitionIds, cancellationToken);

        return new Response
        {
            Deleted = deleted
        };
    }
}