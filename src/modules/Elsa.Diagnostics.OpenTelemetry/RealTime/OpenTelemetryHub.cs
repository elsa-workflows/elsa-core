using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Elsa.Diagnostics.OpenTelemetry.RealTime;

[Authorize]
public class OpenTelemetryHub(IOpenTelemetryLiveFeed liveFeed) : Hub<IOpenTelemetryClient>
{
    public async Task SubscribeAsync(OpenTelemetryTraceFilter? filter)
    {
        await foreach (var item in liveFeed.SubscribeAsync(filter ?? new(), Context.ConnectionAborted))
            await Clients.Caller.ReceiveAsync(item);
    }
}
