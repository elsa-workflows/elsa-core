using Elsa.Abstractions;
using Elsa.Models;
using Elsa.Resilience.Serialization;
using Microsoft.AspNetCore.Http;

namespace Elsa.Resilience.Endpoints.ResilienceStrategies.List;

public class Endpoint(IResilienceStrategyCatalog catalog, ResilienceStrategySerializer serializer) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/resilience/strategies");

        // Static metadata about what is installed, which the designer needs to open any definition, so every signed-in user may read it.
        RequireAuthenticatedOnly();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var strategies = (await catalog.ListAsync(ct)).ToList();
        var response = new ListResponse<IResilienceStrategy>(strategies);

        await HttpContext.Response.WriteAsJsonAsync(response, serializer.SerializerOptions, ct);
    }
}