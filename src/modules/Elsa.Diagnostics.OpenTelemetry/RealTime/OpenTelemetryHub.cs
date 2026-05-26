using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Models;
using Elsa.Diagnostics.OpenTelemetry.Permissions;
using FastEndpoints.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Elsa.Diagnostics.OpenTelemetry.RealTime;

[Authorize]
public class OpenTelemetryHub(IOpenTelemetryLiveFeed liveFeed) : Hub<IOpenTelemetryClient>
{
    private const string ReadAllPermission = "read:*";
    private static readonly string[] ReadPermissions = [PermissionNames.All, ReadAllPermission, OpenTelemetryPermissions.Read];

    public async Task SubscribeAsync(OpenTelemetryTraceFilter? filter)
    {
        EnsureCanReadOpenTelemetry();

        await foreach (var item in liveFeed.SubscribeAsync(ValidateFilter(filter), Context.ConnectionAborted))
            await Clients.Caller.ReceiveAsync(item);
    }

    private static OpenTelemetryTraceFilter ValidateFilter(OpenTelemetryTraceFilter? filter)
    {
        filter ??= new();

        if (filter.From is { } from && filter.To is { } to && from > to)
            throw new HubException("The OpenTelemetry filter 'from' timestamp must be earlier than or equal to 'to'.");

        return filter;
    }

    private void EnsureCanReadOpenTelemetry()
    {
        var user = Context.User;

        if (user?.Identity?.IsAuthenticated != true || !ReadPermissions.Any(user.HasPermission))
            throw new HubException("Access denied.");
    }
}
