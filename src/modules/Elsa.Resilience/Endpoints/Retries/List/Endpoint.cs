using Elsa.Abstractions;
using Elsa.Common.Models;
using Elsa.Models;
using Elsa.Resilience.Entities;

namespace Elsa.Resilience.Endpoints.Retries.List;

public class Endpoint(IRetryAttemptReader reader) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/resilience/retries/{activityInstanceId}");
        ConfigurePermissions("read:*", "read:resilience", "read:resilience:retries");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var skip = Query<int?>("skip", false);
        var take = Query<int?>("take", false);
        var pageArgs = skip == null && take == null ? null : PageArgs.FromRange(skip, take);
        var activityInstanceId = Route<string>("activityInstanceId");

        if (string.IsNullOrWhiteSpace(activityInstanceId))
        {
            AddError("ActivityInstanceId is required.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var page = await reader.ReadAttemptsAsync(activityInstanceId, pageArgs, ct);
        var response = new PagedListResponse<RetryAttemptRecord>(page);
        await Send.OkAsync(response, ct);
    }
}